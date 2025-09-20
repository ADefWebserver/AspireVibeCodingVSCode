using BlazorWebApp.Client.Models;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace BlazorWebApp.Client.Services
{
    public interface IEmbeddingService
    {
        Task<float[]> GenerateEmbeddingAsync(string text);
        Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts);
    }

    public class OpenAIEmbeddingService : IEmbeddingService
    {
        private readonly EmbeddingClient _embeddingClient;
        private readonly OpenAIConfiguration _config;

        public OpenAIEmbeddingService(IOptions<OpenAIConfiguration> config)
        {
            _config = config.Value;
            _embeddingClient = new EmbeddingClient(_config.EmbeddingModel, _config.ApiKey);
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<float>();

            try
            {
                var response = await _embeddingClient.GenerateEmbeddingAsync(text);
                return response.Value.ToFloats().ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate embedding: {ex.Message}", ex);
            }
        }

        public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts)
        {
            if (texts == null || !texts.Any())
                return new List<float[]>();

            try
            {
                var tasks = texts.Select(GenerateEmbeddingAsync);
                var embeddings = await Task.WhenAll(tasks);
                return embeddings.ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate embeddings: {ex.Message}", ex);
            }
        }
    }
}