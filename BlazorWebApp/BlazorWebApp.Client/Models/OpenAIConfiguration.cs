namespace BlazorWebApp.Client.Models
{
    public class OpenAIConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Endpoint { get; set; } = "https://api.openai.com/v1";
        public string Model { get; set; } = "gpt-4";
        public string EmbeddingModel { get; set; } = "text-embedding-3-small";
        public int MaxRetries { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 30;
    }
}