using BlazorWebApp.Client.Models;
using Xceed.Document.NET;
using Xceed.Words.NET;
using System.Text.Json;

namespace BlazorWebApp.Services
{
    public interface IRfpDocumentService
    {
        Task<RfpResponseGenerationResult> GenerateRfpResponseDocumentAsync(RfpResponseGenerationRequest request);
        Task<SummaryGenerationResponse> GenerateSummaryAsync(SummaryGenerationRequest request);
    }

    public class RfpDocumentService : IRfpDocumentService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RfpDocumentService> _logger;

        public RfpDocumentService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<RfpDocumentService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<RfpResponseGenerationResult> GenerateRfpResponseDocumentAsync(RfpResponseGenerationRequest request)
        {
            try
            {
                _logger.LogInformation("Starting RFP response document generation for {QuestionCount} questions", request.Questions.Count);

                if (!request.Questions.Any())
                {
                    return new RfpResponseGenerationResult
                    {
                        Success = false,
                        ErrorMessage = "No questions provided for document generation."
                    };
                }

                string summary = string.Empty;
                if (request.GenerateSummary)
                {
                    var summaryRequest = new SummaryGenerationRequest
                    {
                        Questions = request.Questions.Select(q => new QuestionSummaryInfo
                        {
                            Question = q.Text,
                            Answer = q.Answer,
                            Confidence = q.Confidence
                        }).ToList(),
                        DocumentTitle = request.DocumentTitle,
                        CompanyName = request.CompanyName
                    };

                    var summaryResponse = await GenerateSummaryAsync(summaryRequest);
                    if (summaryResponse.Success)
                    {
                        summary = summaryResponse.Summary;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to generate AI summary, using fallback: {Error}", summaryResponse.ErrorMessage);
                        summary = GenerateFallbackSummary(request);
                    }
                }

                var fileName = $"RFP_Response_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
                var documentBytes = await CreateWordDocumentAsync(request, summary);

                return new RfpResponseGenerationResult
                {
                    Success = true,
                    FileName = fileName,
                    FileSize = documentBytes.Length,
                    GeneratedSummary = summary,
                    QuestionCount = request.Questions.Count,
                    FileData = documentBytes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating RFP response document");
                return new RfpResponseGenerationResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to generate document: {ex.Message}"
                };
            }
        }

        public async Task<SummaryGenerationResponse> GenerateSummaryAsync(SummaryGenerationRequest request)
        {
            try
            {
                var openAiConfig = _configuration.GetSection("OpenAI");
                var apiKey = openAiConfig["ApiKey"];
                var model = openAiConfig["Model"] ?? "gpt-4";
                var endpoint = openAiConfig["Endpoint"] ?? "https://api.openai.com/v1";

                if (string.IsNullOrEmpty(apiKey) || apiKey == "sk-your-actual-openai-api-key-here")
                {
                    return new SummaryGenerationResponse
                    {
                        Success = false,
                        ErrorMessage = "OpenAI API key not configured"
                    };
                }

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var prompt = BuildSummaryPrompt(request);

                var requestBody = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "system", content = "You are a professional proposal writer creating concise, compelling summaries for RFP responses." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 500,
                    temperature = 0.7
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{endpoint}/chat/completions", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAI API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    return new SummaryGenerationResponse
                    {
                        Success = false,
                        ErrorMessage = $"OpenAI API error: {response.StatusCode}"
                    };
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var openAiResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                var summary = openAiResponse
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;

                return new SummaryGenerationResponse
                {
                    Success = true,
                    Summary = summary.Trim()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI summary");
                return new SummaryGenerationResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private string BuildSummaryPrompt(SummaryGenerationRequest request)
        {
            var questionsText = string.Join("\n", request.Questions.Select((q, i) => 
                $"Q{i + 1}: {q.Question}\nA{i + 1}: {q.Answer}\n"));

            return $@"Create a professional, concise summary paragraph for an RFP response document. 

Document Title: {request.DocumentTitle}
Company: {request.CompanyName}

The following questions and answers will be included in the full response:

{questionsText}

Write a compelling opening summary paragraph (2-3 sentences) that:
1. Acknowledges the RFP and expresses interest
2. Briefly highlights our key capabilities relevant to the requirements
3. Sets a confident, professional tone for the detailed responses that follow

Requirements:
- Keep it professional, concise, and engaging
- Do not repeat the specific questions or answers verbatim
- Do not include any assumptions sections or confidence level discussions
- Format as normal narrative paragraphs with left alignment
- Focus only on summarizing our capabilities and approach";
        }

        private string GenerateFallbackSummary(RfpResponseGenerationRequest request)
        {
            var companyName = !string.IsNullOrEmpty(request.CompanyName) ? request.CompanyName : "Our Company";
            var documentTitle = !string.IsNullOrEmpty(request.DocumentTitle) ? request.DocumentTitle : "this RFP";

            return $"Thank you for the opportunity to respond to {documentTitle}. {companyName} is pleased to present our comprehensive response addressing your requirements. Our detailed answers below demonstrate our expertise, capabilities, and commitment to delivering exceptional solutions that meet your specific needs.";
        }

        private Task<byte[]> CreateWordDocumentAsync(RfpResponseGenerationRequest request, string summary)
        {
            return Task.Run(() =>
            {
                using var memoryStream = new MemoryStream();
                
                // Create a new document
                using var document = DocX.Create(memoryStream);
                
                // Add title
                var title = !string.IsNullOrEmpty(request.DocumentTitle) 
                    ? request.DocumentTitle 
                    : "RFP Response";
                
                var titleParagraph = document.InsertParagraph(title);
                titleParagraph.FontSize(18);
                titleParagraph.Bold();
                titleParagraph.Alignment = Alignment.center;
                
                document.InsertParagraph(); // Empty line
                
                // Add company name if provided
                if (!string.IsNullOrEmpty(request.CompanyName))
                {
                    var companyParagraph = document.InsertParagraph($"Submitted by: {request.CompanyName}");
                    companyParagraph.FontSize(12);
                    companyParagraph.Italic();
                    companyParagraph.Alignment = Alignment.center;
                    
                    document.InsertParagraph(); // Empty line
                }
                
                // Add date
                var dateParagraph = document.InsertParagraph($"Date: {DateTime.Now:MMMM dd, yyyy}");
                dateParagraph.FontSize(12);
                dateParagraph.Alignment = Alignment.center;
                
                document.InsertParagraph(); // Empty line
                
                // Add summary if provided
                if (!string.IsNullOrEmpty(summary))
                {
                    var summaryHeading = document.InsertParagraph("Executive Summary");
                    summaryHeading.FontSize(14);
                    summaryHeading.Bold();
                    summaryHeading.UnderlineStyle(UnderlineStyle.singleLine);
                    
                    var summaryParagraph = document.InsertParagraph(summary);
                    summaryParagraph.FontSize(11);
                    summaryParagraph.Alignment = Alignment.left;
                    
                    document.InsertParagraph(); // Empty line
                }
                
                // Add questions and answers section
                var qaHeading = document.InsertParagraph("Detailed Responses");
                qaHeading.FontSize(14);
                qaHeading.Bold();
                qaHeading.UnderlineStyle(UnderlineStyle.singleLine);
                
                document.InsertParagraph(); // Empty line
                
                // Add each question and answer
                for (int i = 0; i < request.Questions.Count; i++)
                {
                    var question = request.Questions[i];
                    
                    // Question number and text
                    var questionParagraph = document.InsertParagraph($"{i + 1}. {question.Text}");
                    questionParagraph.FontSize(12);
                    questionParagraph.Bold();
                    
                    // Answer
                    var answerParagraph = document.InsertParagraph(question.Answer);
                    answerParagraph.FontSize(11);
                    answerParagraph.Alignment = Alignment.left;
                    
                    // Add space between questions
                    if (i < request.Questions.Count - 1)
                    {
                        document.InsertParagraph(); // Empty line
                    }
                }
                
                // Add footer with generation info
                document.InsertParagraph();
                var footerParagraph = document.InsertParagraph($"Document generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                footerParagraph.FontSize(9);
                footerParagraph.Italic();
                footerParagraph.Color(Xceed.Drawing.Color.Gray);
                footerParagraph.Alignment = Alignment.center;
                
                // Save the document to memory stream
                document.Save();
                
                return memoryStream.ToArray();
            });
        }
    }
}