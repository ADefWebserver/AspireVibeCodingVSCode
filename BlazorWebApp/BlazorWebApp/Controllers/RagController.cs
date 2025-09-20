using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using BlazorWebApp.Client.Models;
using System.Text;

namespace BlazorWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RagController : ControllerBase
    {
        private readonly ChatClient _chatClient;
        private readonly OpenAIConfiguration _config;
        private readonly ILogger<RagController> _logger;

        public RagController(IOptions<OpenAIConfiguration> config, ILogger<RagController> logger)
        {
            _config = config.Value;
            _logger = logger;
            _chatClient = new ChatClient(_config.Model, _config.ApiKey);
        }

        [HttpPost("generate-answer")]
        public async Task<ActionResult<QuestionAnswerResponse>> GenerateAnswer([FromBody] QuestionAnswerRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest("Question cannot be empty");
            }

            try
            {
                // Build context from relevant content
                var contextBuilder = new StringBuilder();
                if (request.RelevantContent?.Any() == true)
                {
                    contextBuilder.AppendLine("Based on the following relevant information from the knowledgebase:");
                    contextBuilder.AppendLine();

                    foreach (var content in request.RelevantContent.Take(5)) // Limit to top 5 results
                    {
                        contextBuilder.AppendLine($"**Source: {content.FileName} (Similarity: {content.SimilarityScore:P1})**");
                        contextBuilder.AppendLine(content.Text);
                        contextBuilder.AppendLine();
                    }
                }

                // Create the prompt
                var systemMessage = @"You are an expert assistant helping to answer questions from Request for Proposal (RFP) documents. 
Your task is to provide clear, accurate, and comprehensive answers based on the provided context from the knowledgebase.

Guidelines:
1. Answer directly and professionally
2. Use the provided context to support your answer
3. If the context doesn't contain enough information, clearly state what additional information would be needed
4. Structure your answer clearly with bullet points or numbered lists when appropriate
5. Be specific and actionable in your recommendations
6. If you're making assumptions, clearly state them
7. Provide confidence levels based on how well the context supports your answer";

                var userMessage = $@"Context from knowledgebase:
{contextBuilder}

Question: {request.Question}

Please provide a comprehensive answer to this question based on the context provided above.";

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemMessage),
                    new UserChatMessage(userMessage)
                };

                var response = await _chatClient.CompleteChatAsync(messages);
                var answer = response.Value.Content[0].Text;

                // Calculate confidence based on context quality
                var confidence = CalculateConfidence(request.RelevantContent);

                // Extract source documents
                var sourceDocuments = request.RelevantContent?
                    .Select(c => c.FileName)
                    .Distinct()
                    .ToList() ?? new List<string>();

                var result = new QuestionAnswerResponse
                {
                    Answer = answer,
                    Confidence = confidence,
                    SourceDocuments = sourceDocuments
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate answer for question: {Question}", request.Question);
                return StatusCode(500, "Failed to generate answer");
            }
        }

        private double CalculateConfidence(List<RagSearchResult>? relevantContent)
        {
            if (relevantContent == null || !relevantContent.Any())
                return 0.1; // Very low confidence without context

            // Base confidence on the quality and quantity of relevant content
            var topSimilarity = relevantContent.Max(c => c.SimilarityScore);
            var averageSimilarity = relevantContent.Average(c => c.SimilarityScore);
            var contentCount = Math.Min(relevantContent.Count, 5); // Cap at 5 for diminishing returns

            // Weighted calculation
            var confidence = (topSimilarity * 0.5) + (averageSimilarity * 0.3) + (contentCount * 0.04);
            
            // Ensure confidence is between 0.1 and 0.95
            return Math.Max(0.1, Math.Min(0.95, confidence));
        }

        [HttpPost("process-question")]
        public ActionResult<RfpQuestion> ProcessQuestion([FromBody] ProcessQuestionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.QuestionText))
            {
                return BadRequest("Question text cannot be empty");
            }

            try
            {
                // This endpoint could be used for processing individual questions
                // For now, it returns a basic RfpQuestion structure
                var question = new RfpQuestion
                {
                    Text = request.QuestionText,
                    Answer = "Processing...",
                    Confidence = 0.0
                };

                return Ok(question);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process question: {Question}", request.QuestionText);
                return StatusCode(500, "Failed to process question");
            }
        }

        [HttpGet("health")]
        public ActionResult GetHealth()
        {
            return Ok(new { Status = "Healthy", Model = _config.Model, Timestamp = DateTime.UtcNow });
        }
    }

    public class ProcessQuestionRequest
    {
        public string QuestionText { get; set; } = string.Empty;
        public List<RagSearchResult> Context { get; set; } = new();
    }
}