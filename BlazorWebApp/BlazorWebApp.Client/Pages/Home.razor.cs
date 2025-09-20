using BlazorWebApp.Client.Models;
using BlazorWebApp.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using System.Text.Json;
using System.Text;

namespace BlazorWebApp.Client.Pages
{
    public partial class Home
    {
        [Inject] private IPdfProcessingService PdfProcessingService { get; set; } = default!;
        [Inject] private IQuestionDetectionService QuestionDetectionService { get; set; } = default!;
        [Inject] private IEmbeddingService EmbeddingService { get; set; } = default!;
        [Inject] private IRagService RagService { get; set; } = default!;
        [Inject] private IKnowledgebaseStorageService KnowledgebaseStorageService { get; set; } = default!;
        [Inject] private NotificationService NotificationService { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        private IBrowserFile? selectedFile;
        private RfpProcessingResult? processingResult;
        private bool isProcessing = false;
        private string currentStatusMessage = "";
        private int currentProgress = 0;
        private RadzenDataGrid<RfpQuestion>? questionsGrid;

        private List<ProcessingStepInfo> processingSteps = new()
        {
            new ProcessingStepInfo { StepName = "Upload Document", Description = "Uploading and validating PDF file" },
            new ProcessingStepInfo { StepName = "Extract Text", Description = "Extracting text content from PDF" },
            new ProcessingStepInfo { StepName = "Detect Questions", Description = "Identifying questions in the document" },
            new ProcessingStepInfo { StepName = "Generate Embeddings", Description = "Creating embeddings for questions" },
            new ProcessingStepInfo { StepName = "Retrieve Content", Description = "Finding relevant content from knowledgebase" },
            new ProcessingStepInfo { StepName = "Generate Answers", Description = "Creating answers using AI" },
            new ProcessingStepInfo { StepName = "Complete", Description = "Processing finished successfully" }
        };

        private void OnFileSelected(InputFileChangeEventArgs e)
        {
            selectedFile = e.GetMultipleFiles().FirstOrDefault();
            StateHasChanged();
        }

        private async Task ProcessRfp()
        {
            if (selectedFile == null)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "No File Selected",
                    Detail = "Please select a PDF file to process."
                });
                return;
            }

            var file = selectedFile;
            if (!file.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Invalid File Type",
                    Detail = "Please select a PDF file."
                });
                return;
            }

            isProcessing = true;
            currentProgress = 0;
            currentStatusMessage = "Starting processing...";

            try
            {
                // Reset processing steps
                foreach (var step in processingSteps)
                {
                    step.IsCompleted = false;
                    step.IsCurrentStep = false;
                    step.HasError = false;
                    step.ErrorMessage = "";
                    step.StartTime = null;
                    step.EndTime = null;
                }

                // Read file content
                using var stream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024); // 50MB limit
                var buffer = new byte[stream.Length];
                var totalRead = 0;
                int bytesRead;
                while (totalRead < buffer.Length)
                {
                    bytesRead = await stream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead));
                    if (bytesRead == 0)
                        break;
                    totalRead += bytesRead;
                }

                // Load knowledgebase
                var knowledgebase = await KnowledgebaseStorageService.LoadKnowledgebaseAsync();
                if (knowledgebase?.Items == null || !knowledgebase.Items.Any())
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = "Empty Knowledgebase",
                        Detail = "Your knowledgebase is empty. Please add some documents first."
                    });
                    isProcessing = false;
                    return;
                }

                // Process the RFP
                processingResult = await RagService.ProcessRfpAsync(
                    buffer, 
                    file.Name, 
                    knowledgebase, 
                    OnProcessingProgress);

                if (processingResult.ProcessingStatus == RfpProcessingStatus.Completed)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Processing Complete",
                        Detail = $"Successfully processed {processingResult.Questions.Count} questions from {file.Name}."
                    });
                }
                else if (processingResult.ProcessingStatus == RfpProcessingStatus.Error)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Processing Error",
                        Detail = processingResult.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Processing Failed",
                    Detail = ex.Message
                });

                processingResult = new RfpProcessingResult
                {
                    FileName = file.Name,
                    ProcessingStatus = RfpProcessingStatus.Error,
                    ErrorMessage = ex.Message
                };
            }
            finally
            {
                isProcessing = false;
                StateHasChanged();
            }
        }

        private void OnProcessingProgress(string message, int progress)
        {
            currentStatusMessage = message;
            currentProgress = progress;

            // Update processing steps based on progress
            UpdateProcessingSteps(progress);

            InvokeAsync(StateHasChanged);
        }

        private void UpdateProcessingSteps(int progress)
        {
            for (int i = 0; i < processingSteps.Count; i++)
            {
                var step = processingSteps[i];
                
                if (progress >= (i + 1) * 100 / processingSteps.Count)
                {
                    step.IsCompleted = true;
                    step.IsCurrentStep = false;
                    step.EndTime = DateTime.Now;
                }
                else if (progress >= i * 100 / processingSteps.Count)
                {
                    step.IsCurrentStep = true;
                    step.IsCompleted = false;
                    if (step.StartTime == null)
                        step.StartTime = DateTime.Now;
                }
                else
                {
                    step.IsCurrentStep = false;
                    step.IsCompleted = false;
                }
            }
        }

        private void OnAnswerChanged(RfpQuestion question)
        {
            question.IsAnswerEdited = question.Answer != question.OriginalAnswer;
            StateHasChanged();
        }

        private void ResetAnswer(RfpQuestion question)
        {
            question.Answer = question.OriginalAnswer;
            question.IsAnswerEdited = false;
            StateHasChanged();
        }

        private BadgeStyle GetConfidenceBadgeStyle(double confidence)
        {
            return confidence switch
            {
                >= 0.8 => BadgeStyle.Success,
                >= 0.5 => BadgeStyle.Warning,
                _ => BadgeStyle.Danger
            };
        }

        private void ResetProcessing()
        {
            processingResult = null;
            selectedFile = null;
            isProcessing = false;
            currentProgress = 0;
            currentStatusMessage = "";
            StateHasChanged();
        }

        private async Task ExportAsJson()
        {
            if (processingResult?.Questions == null || !processingResult.Questions.Any())
                return;

            try
            {
                var json = JsonSerializer.Serialize(processingResult, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var bytes = Encoding.UTF8.GetBytes(json);
                var fileName = $"rfp_qa_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                
                // Use browser's download functionality
                await JSRuntime.InvokeVoidAsync("downloadFile", fileName, "application/json", bytes);

                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Export Complete",
                    Detail = $"Downloaded {fileName}"
                });
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Export Failed",
                    Detail = ex.Message
                });
            }
        }

        private async Task ExportAsCsv()
        {
            if (processingResult?.Questions == null || !processingResult.Questions.Any())
                return;

            try
            {
                var csv = new StringBuilder();
                csv.AppendLine("Question,Answer,Confidence,Is Modified,Original Answer");

                foreach (var question in processingResult.Questions)
                {
                    var questionText = EscapeCsvValue(question.Text);
                    var answerText = EscapeCsvValue(question.Answer);
                    var originalAnswerText = EscapeCsvValue(question.OriginalAnswer);
                    
                    csv.AppendLine($"{questionText},{answerText},{question.Confidence:P2},{question.IsAnswerEdited},{originalAnswerText}");
                }

                var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                var fileName = $"rfp_qa_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                
                await JSRuntime.InvokeVoidAsync("downloadFile", fileName, "text/csv", bytes);

                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Export Complete",
                    Detail = $"Downloaded {fileName}"
                });
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Export Failed",
                    Detail = ex.Message
                });
            }
        }

        private static string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // Escape quotes and wrap in quotes if necessary
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }
    }
}