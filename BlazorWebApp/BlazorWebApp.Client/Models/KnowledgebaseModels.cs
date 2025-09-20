using System.Text.Json.Serialization;

namespace BlazorWebApp.Client.Models
{
    public class KnowledgebaseItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("originalText")]
        public string OriginalText { get; set; } = string.Empty;

        [JsonPropertyName("originalTextEmbedding")]
        public float[] OriginalTextEmbedding { get; set; } = Array.Empty<float>();

        [JsonPropertyName("chunks")]
        public List<TextChunk> Chunks { get; set; } = new();

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class TextChunk
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();

        [JsonPropertyName("startIndex")]
        public int StartIndex { get; set; }

        [JsonPropertyName("endIndex")]
        public int EndIndex { get; set; }
    }

    public class Knowledgebase
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("items")]
        public List<KnowledgebaseItem> Items { get; set; } = new();

        [JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}