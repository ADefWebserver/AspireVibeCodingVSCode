using System.Text.Json.Serialization;

namespace BlazorWebApp.Client.Models
{
    public class RfpQuestion
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();

        [JsonPropertyName("answer")]
        public string Answer { get; set; } = string.Empty;

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("relevantKnowledgebaseItems")]
        public List<string> RelevantKnowledgebaseItems { get; set; } = new();

        [JsonPropertyName("isAnswerEdited")]
        public bool IsAnswerEdited { get; set; } = false;

        [JsonPropertyName("originalAnswer")]
        public string OriginalAnswer { get; set; } = string.Empty;
    }

    public class RfpProcessingResult
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("extractedText")]
        public string ExtractedText { get; set; } = string.Empty;

        [JsonPropertyName("questions")]
        public List<RfpQuestion> Questions { get; set; } = new();

        [JsonPropertyName("processingStatus")]
        public RfpProcessingStatus ProcessingStatus { get; set; } = RfpProcessingStatus.NotStarted;

        [JsonPropertyName("currentStep")]
        public string CurrentStep { get; set; } = string.Empty;

        [JsonPropertyName("progress")]
        public int Progress { get; set; } = 0;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public enum RfpProcessingStatus
    {
        NotStarted,
        Uploading,
        ExtractingText,
        DetectingQuestions,
        GeneratingEmbeddings,
        RetrievingRelevantContent,
        GeneratingAnswers,
        Completed,
        Error
    }

    public class ProcessingStepInfo
    {
        public string StepName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsCompleted { get; set; } = false;
        public bool IsCurrentStep { get; set; } = false;
        public bool HasError { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class RagSearchResult
    {
        public string KnowledgebaseItemId { get; set; } = string.Empty;
        public string TextChunkId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public double SimilarityScore { get; set; }
        public string FileName { get; set; } = string.Empty;
    }

    public class QuestionAnswerRequest
    {
        public string Question { get; set; } = string.Empty;
        public List<RagSearchResult> RelevantContent { get; set; } = new();
        public string Model { get; set; } = "gpt-4";
    }

    public class QuestionAnswerResponse
    {
        public string Answer { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public List<string> SourceDocuments { get; set; } = new();
    }
}