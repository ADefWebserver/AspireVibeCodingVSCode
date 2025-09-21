using BlazorWebApp.Client.Models;
using BlazorWebApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlazorWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RfpResponseController : ControllerBase
    {
        private readonly IRfpDocumentService _rfpDocumentService;
        private readonly ILogger<RfpResponseController> _logger;

        public RfpResponseController(
            IRfpDocumentService rfpDocumentService,
            ILogger<RfpResponseController> logger)
        {
            _rfpDocumentService = rfpDocumentService;
            _logger = logger;
        }

        /// <summary>
        /// Generates a Word document (.docx) containing RFP responses with AI-generated summary
        /// </summary>
        /// <param name="request">The RFP response generation request</param>
        /// <returns>The generated Word document as a file download</returns>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateRfpResponse([FromBody] RfpResponseGenerationRequest request)
        {
            try
            {
                _logger.LogInformation("Received RFP response generation request for {QuestionCount} questions", request.Questions?.Count ?? 0);

                if (request.Questions == null || !request.Questions.Any())
                {
                    return BadRequest(new { error = "No questions provided for document generation." });
                }

                // Validate that questions have answers
                var unansweredQuestions = request.Questions.Where(q => string.IsNullOrWhiteSpace(q.Answer)).ToList();
                if (unansweredQuestions.Any())
                {
                    return BadRequest(new { 
                        error = $"Found {unansweredQuestions.Count} questions without answers. Please provide answers for all questions before generating the document.",
                        unansweredQuestions = unansweredQuestions.Select(q => q.Text).ToList()
                    });
                }

                var result = await _rfpDocumentService.GenerateRfpResponseDocumentAsync(request);

                if (!result.Success)
                {
                    _logger.LogError("RFP response generation failed: {Error}", result.ErrorMessage);
                    return BadRequest(new { error = result.ErrorMessage });
                }

                _logger.LogInformation("Successfully generated RFP response document: {FileName}, Size: {FileSize} bytes", 
                    result.FileName, result.FileSize);

                // Return the file as a download
                return File(
                    result.FileData,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    result.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during RFP response generation");
                return StatusCode(500, new { error = "An unexpected error occurred while generating the document." });
            }
        }

        /// <summary>
        /// Generates an AI summary for the given questions and answers
        /// </summary>
        /// <param name="request">The summary generation request</param>
        /// <returns>The generated summary text</returns>
        [HttpPost("generate-summary")]
        public async Task<IActionResult> GenerateSummary([FromBody] SummaryGenerationRequest request)
        {
            try
            {
                _logger.LogInformation("Received summary generation request for {QuestionCount} questions", request.Questions?.Count ?? 0);

                if (request.Questions == null || !request.Questions.Any())
                {
                    return BadRequest(new { error = "No questions provided for summary generation." });
                }

                var result = await _rfpDocumentService.GenerateSummaryAsync(request);

                if (!result.Success)
                {
                    _logger.LogError("Summary generation failed: {Error}", result.ErrorMessage);
                    return BadRequest(new { error = result.ErrorMessage });
                }

                _logger.LogInformation("Successfully generated summary");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during summary generation");
                return StatusCode(500, new { error = "An unexpected error occurred while generating the summary." });
            }
        }

        /// <summary>
        /// Gets the status of document generation capabilities
        /// </summary>
        /// <returns>Service status information</returns>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            try
            {
                var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                var openAiConfig = config.GetSection("OpenAI");
                var hasApiKey = !string.IsNullOrEmpty(openAiConfig["ApiKey"]) && 
                               openAiConfig["ApiKey"] != "sk-your-actual-openai-api-key-here";

                return Ok(new
                {
                    serviceAvailable = true,
                    aiSummaryAvailable = hasApiKey,
                    supportedFormats = new[] { "docx" },
                    maxQuestions = 1000,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking service status");
                return StatusCode(500, new { error = "Unable to check service status." });
            }
        }
    }
}