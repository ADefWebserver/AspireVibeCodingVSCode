using BlazorWebApp.Client.Models;
using Blazored.LocalStorage;
using System.Text.Json;

namespace BlazorWebApp.Client.Services
{
    public interface IKnowledgebaseStorageService
    {
        Task<Knowledgebase> LoadKnowledgebaseAsync();
        Task SaveKnowledgebaseAsync(Knowledgebase knowledgebase);
        Task AddKnowledgebaseItemAsync(KnowledgebaseItem item);
        Task<bool> RemoveKnowledgebaseItemAsync(string itemId);
        Task ClearKnowledgebaseAsync();
    }

    public class KnowledgebaseStorageService : IKnowledgebaseStorageService
    {
        private const string KNOWLEDGEBASE_KEY = "knowledgebase";
        private readonly ILocalStorageService _localStorage;
        private readonly JsonSerializerOptions _jsonOptions;

        public KnowledgebaseStorageService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        public async Task<Knowledgebase> LoadKnowledgebaseAsync()
        {
            try
            {
                var exists = await _localStorage.ContainKeyAsync(KNOWLEDGEBASE_KEY);
                if (!exists)
                {
                    return new Knowledgebase();
                }

                var json = await _localStorage.GetItemAsStringAsync(KNOWLEDGEBASE_KEY);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new Knowledgebase();
                }

                var knowledgebase = JsonSerializer.Deserialize<Knowledgebase>(json, _jsonOptions);
                return knowledgebase ?? new Knowledgebase();
            }
            catch (Exception ex)
            {
                // Log error and return empty knowledgebase
                Console.WriteLine($"Error loading knowledgebase: {ex.Message}");
                return new Knowledgebase();
            }
        }

        public async Task SaveKnowledgebaseAsync(Knowledgebase knowledgebase)
        {
            try
            {
                knowledgebase.LastUpdated = DateTime.UtcNow;
                var json = JsonSerializer.Serialize(knowledgebase, _jsonOptions);
                await _localStorage.SetItemAsStringAsync(KNOWLEDGEBASE_KEY, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save knowledgebase: {ex.Message}", ex);
            }
        }

        public async Task AddKnowledgebaseItemAsync(KnowledgebaseItem item)
        {
            var knowledgebase = await LoadKnowledgebaseAsync();
            
            // Remove any existing item with the same file name
            knowledgebase.Items.RemoveAll(i => i.FileName.Equals(item.FileName, StringComparison.OrdinalIgnoreCase));
            
            // Add the new item
            knowledgebase.Items.Add(item);
            
            await SaveKnowledgebaseAsync(knowledgebase);
        }

        public async Task<bool> RemoveKnowledgebaseItemAsync(string itemId)
        {
            var knowledgebase = await LoadKnowledgebaseAsync();
            var removedCount = knowledgebase.Items.RemoveAll(i => i.Id == itemId);
            
            if (removedCount > 0)
            {
                await SaveKnowledgebaseAsync(knowledgebase);
                return true;
            }
            
            return false;
        }

        public async Task ClearKnowledgebaseAsync()
        {
            await _localStorage.RemoveItemAsync(KNOWLEDGEBASE_KEY);
        }
    }
}