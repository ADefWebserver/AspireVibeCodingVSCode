using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
using BlazorWebApp.Client.Models;

namespace BlazorWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmbeddingController : ControllerBase
    {
        private readonly EmbeddingClient _embeddingClient;
        private readonly OpenAIConfiguration _config;
        private readonly ILogger<EmbeddingController> _logger;

        public EmbeddingController(IOptions<OpenAIConfiguration> config, ILogger<EmbeddingController> logger)
        {
            _config = config.Value;
            _logger = logger;
            _embeddingClient = new EmbeddingClient(_config.EmbeddingModel, _config.ApiKey);
        }

        [HttpPost("generate")]
        public async Task<ActionResult<float[]>> GenerateEmbedding([FromBody] GenerateEmbeddingRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest("Text cannot be empty");
            }

            try
            {
                var response = await _embeddingClient.GenerateEmbeddingAsync(request.Text);
                return Ok(response.Value.ToFloats().ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate embedding for text: {Text}", request.Text);
                return StatusCode(500, "Failed to generate embedding");
            }
        }

        [HttpPost("generate-batch")]
        public async Task<ActionResult<List<float[]>>> GenerateEmbeddings([FromBody] GenerateEmbeddingsRequest request)
        {
            if (request.Texts == null || !request.Texts.Any())
            {
                return BadRequest("Texts cannot be empty");
            }

            try
            {
                var results = new List<float[]>();
                
                foreach (var text in request.Texts)
                {
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        results.Add(Array.Empty<float>());
                        continue;
                    }

                    var response = await _embeddingClient.GenerateEmbeddingAsync(text);
                    results.Add(response.Value.ToFloats().ToArray());
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate embeddings for batch");
                return StatusCode(500, "Failed to generate embeddings");
            }
        }
    }

    public class GenerateEmbeddingRequest
    {
        public string Text { get; set; } = string.Empty;
    }

    public class GenerateEmbeddingsRequest
    {
        public List<string> Texts { get; set; } = new();
    }
}