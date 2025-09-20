using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;
using Blazored.LocalStorage;
using BlazorWebApp.Client.Models;
using BlazorWebApp.Client.Services;

namespace BlazorWebApp.Client;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        // Add Radzen components
        builder.Services.AddRadzenComponents();
        
        // Add Radzen NotificationService for RadzenNotification component
        builder.Services.AddScoped<NotificationService>();

        // Add Blazored LocalStorage
        builder.Services.AddBlazoredLocalStorage();

        // Add HttpClient for API calls
        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        // Register application services (using secure server-based implementation)
        builder.Services.AddScoped<IPdfProcessingService, PdfProcessingService>();
        builder.Services.AddScoped<IEmbeddingService, ServerEmbeddingService>();
        builder.Services.AddScoped<IKnowledgebaseStorageService, KnowledgebaseStorageService>();
        builder.Services.AddScoped<IQuestionDetectionService, QuestionDetectionService>();
        builder.Services.AddScoped<IRagService, RagService>();

        await builder.Build().RunAsync();
    }
}
