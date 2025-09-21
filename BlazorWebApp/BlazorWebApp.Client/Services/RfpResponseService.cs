using BlazorWebApp.Client.Models;
using System.Net.Http.Json;

namespace BlazorWebApp.Client.Services
{
    public interface IRfpResponseService
    {
        Task<RfpResponseGenerationResult> GenerateRfpResponseAsync(RfpResponseGenerationRequest request);
        Task<SummaryGenerationResponse> GenerateSummaryAsync(SummaryGenerationRequest request);
        Task<bool> CheckServiceStatusAsync();
    }

    public class RfpResponseService : IRfpResponseService
    {
        private readonly HttpClient _httpClient;

        public RfpResponseService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<RfpResponseGenerationResult> GenerateRfpResponseAsync(RfpResponseGenerationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/rfpresponse/generate", request);
                
                if (response.IsSuccessStatusCode)
                {
                    // For file downloads, we need to handle the response differently
                    if (response.Content.Headers.ContentType?.MediaType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                    {
                        var fileBytes = await response.Content.ReadAsByteArrayAsync();
                        var fileName = GetFileNameFromResponse(response) ?? $"RFP_Response_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
                        
                        return new RfpResponseGenerationResult
                        {
                            Success = true,
                            FileName = fileName,
                            FileSize = fileBytes.Length,
                            FileData = fileBytes,
                            QuestionCount = request.Questions.Count
                        };
                    }
                }

                // Handle error responses
                var errorContent = await response.Content.ReadAsStringAsync();
                return new RfpResponseGenerationResult
                {
                    Success = false,
                    ErrorMessage = $"Server returned {response.StatusCode}: {errorContent}"
                };
            }
            catch (Exception ex)
            {
                return new RfpResponseGenerationResult
                {
                    Success = false,
                    ErrorMessage = $"Request failed: {ex.Message}"
                };
            }
        }

        public async Task<SummaryGenerationResponse> GenerateSummaryAsync(SummaryGenerationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/rfpresponse/generate-summary", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<SummaryGenerationResponse>();
                    return result ?? new SummaryGenerationResponse 
                    { 
                        Success = false, 
                        ErrorMessage = "Invalid response format" 
                    };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new SummaryGenerationResponse
                {
                    Success = false,
                    ErrorMessage = $"Server returned {response.StatusCode}: {errorContent}"
                };
            }
            catch (Exception ex)
            {
                return new SummaryGenerationResponse
                {
                    Success = false,
                    ErrorMessage = $"Request failed: {ex.Message}"
                };
            }
        }

        public async Task<bool> CheckServiceStatusAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/rfpresponse/status");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static string? GetFileNameFromResponse(HttpResponseMessage response)
        {
            if (response.Content.Headers.ContentDisposition?.FileName is string fileName)
            {
                return fileName.Trim('"');
            }
            return null;
        }
    }
}