using BlazorWebApp.Client.Models;
using System.Text.Json;
using System.Text;

namespace BlazorWebApp.Client.Services
{
    public interface IRagService
    {
        Task<QuestionAnswerResponse> GenerateAnswerAsync(QuestionAnswerRequest request);
        Task<RfpProcessingResult> ProcessRfpAsync(byte[] pdfBytes, string fileName, Knowledgebase knowledgebase, Action<string, int> progressCallback = null);
    }

    public class RagService : IRagService
    {
        private readonly HttpClient _httpClient;
        private readonly IPdfProcessingService _pdfProcessingService;
        private readonly IQuestionDetectionService _questionDetectionService;
        private readonly IEmbeddingService _embeddingService;
        private readonly IKnowledgebaseStorageService _knowledgebaseStorageService;
        private readonly JsonSerializerOptions _jsonOptions;

        public RagService(
            HttpClient httpClient,
            IPdfProcessingService pdfProcessingService,
            IQuestionDetectionService questionDetectionService,
            IEmbeddingService embeddingService,
            IKnowledgebaseStorageService knowledgebaseStorageService)
        {
            _httpClient = httpClient;
            _pdfProcessingService = pdfProcessingService;
            _questionDetectionService = questionDetectionService;
            _embeddingService = embeddingService;
            _knowledgebaseStorageService = knowledgebaseStorageService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<QuestionAnswerResponse> GenerateAnswerAsync(QuestionAnswerRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/rag/generate-answer", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<QuestionAnswerResponse>(responseJson, _jsonOptions);

                return result ?? new QuestionAnswerResponse();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate answer: {ex.Message}", ex);
            }
        }

        public async Task<RfpProcessingResult> ProcessRfpAsync(
            byte[] pdfBytes, 
            string fileName, 
            Knowledgebase knowledgebase, 
            Action<string, int> progressCallback = null)
        {
            var result = new RfpProcessingResult
            {
                FileName = fileName,
                ProcessingStatus = RfpProcessingStatus.Uploading
            };

            try
            {
                // Step 1: Extract text from PDF
                progressCallback?.Invoke("Extracting text from PDF...", 10);
                result.ProcessingStatus = RfpProcessingStatus.ExtractingText;
                result.CurrentStep = "Extracting text from PDF";
                
                result.ExtractedText = await _pdfProcessingService.ExtractTextFromPdfAsync(pdfBytes);
                
                if (string.IsNullOrWhiteSpace(result.ExtractedText))
                {
                    throw new InvalidOperationException("No text could be extracted from the PDF");
                }

                // Step 2: Detect questions
                progressCallback?.Invoke("Detecting questions in the document...", 25);
                result.ProcessingStatus = RfpProcessingStatus.DetectingQuestions;
                result.CurrentStep = "Detecting questions";
                
                var detectedQuestions = await _questionDetectionService.DetectQuestionsAsync(result.ExtractedText);
                
                if (!detectedQuestions.Any())
                {
                    throw new InvalidOperationException("No questions detected in the document");
                }

                // Convert to RfpQuestion objects
                foreach (var questionText in detectedQuestions)
                {
                    result.Questions.Add(new RfpQuestion
                    {
                        Text = questionText
                    });
                }

                // Step 3: Generate embeddings for questions
                progressCallback?.Invoke("Generating embeddings for questions...", 40);
                result.ProcessingStatus = RfpProcessingStatus.GeneratingEmbeddings;
                result.CurrentStep = "Generating embeddings";
                
                var questionTexts = result.Questions.Select(q => q.Text).ToList();
                var embeddings = await _embeddingService.GenerateEmbeddingsAsync(questionTexts);
                
                for (int i = 0; i < result.Questions.Count && i < embeddings.Count; i++)
                {
                    result.Questions[i].Embedding = embeddings[i];
                }

                // Step 4: Process each question with RAG
                progressCallback?.Invoke("Retrieving relevant content and generating answers...", 60);
                result.ProcessingStatus = RfpProcessingStatus.RetrievingRelevantContent;
                result.CurrentStep = "Performing RAG for each question";

                var totalQuestions = result.Questions.Count;
                for (int i = 0; i < totalQuestions; i++)
                {
                    var question = result.Questions[i];
                    var currentProgress = 60 + (i * 30 / totalQuestions);
                    
                    progressCallback?.Invoke($"Processing question {i + 1} of {totalQuestions}...", currentProgress);

                    // Find relevant content using RAG
                    var relevantContent = _embeddingService.FindSimilarContent(
                        question.Embedding, 
                        knowledgebase, 
                        topK: 5, 
                        minSimilarity: 0.3);

                    if (relevantContent.Any())
                    {
                        question.RelevantKnowledgebaseItems = relevantContent
                            .Select(r => r.KnowledgebaseItemId)
                            .Distinct()
                            .ToList();

                        // Generate answer using LLM
                        result.ProcessingStatus = RfpProcessingStatus.GeneratingAnswers;
                        
                        var answerRequest = new QuestionAnswerRequest
                        {
                            Question = question.Text,
                            RelevantContent = relevantContent,
                            Model = "gpt-4" // This should come from configuration
                        };

                        var answerResponse = await GenerateAnswerAsync(answerRequest);
                        question.Answer = answerResponse.Answer;
                        question.OriginalAnswer = answerResponse.Answer;
                        question.Confidence = answerResponse.Confidence;
                    }
                    else
                    {
                        // No relevant content found
                        question.Answer = "No relevant information found in the knowledgebase to answer this question.";
                        question.OriginalAnswer = question.Answer;
                        question.Confidence = 0.1;
                    }
                }

                // Step 5: Complete processing
                progressCallback?.Invoke("Processing completed!", 100);
                result.ProcessingStatus = RfpProcessingStatus.Completed;
                result.CurrentStep = "Completed";
                result.Progress = 100;
                result.CompletedAt = DateTime.UtcNow;

                return result;
            }
            catch (Exception ex)
            {
                result.ProcessingStatus = RfpProcessingStatus.Error;
                result.ErrorMessage = ex.Message;
                result.CurrentStep = "Error occurred";
                progressCallback?.Invoke($"Error: {ex.Message}", result.Progress);
                
                return result;
            }
        }
    }
}