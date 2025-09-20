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

        // Configure OpenAI settings
        builder.Services.Configure<OpenAIConfiguration>(options =>
        {
            builder.Configuration.GetSection("OpenAI").Bind(options);
        });

        // Register application services
        builder.Services.AddScoped<IPdfProcessingService, PdfProcessingService>();
        builder.Services.AddScoped<IEmbeddingService, OpenAIEmbeddingService>();
        builder.Services.AddScoped<IKnowledgebaseStorageService, KnowledgebaseStorageService>();

        await builder.Build().RunAsync();
    }
}
