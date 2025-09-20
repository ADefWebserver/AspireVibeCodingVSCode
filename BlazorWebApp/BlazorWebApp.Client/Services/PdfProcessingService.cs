using BlazorWebApp.Client.Models;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Text;
using System.Text.RegularExpressions;

namespace BlazorWebApp.Client.Services
{
    public interface IPdfProcessingService
    {
        Task<string> ExtractTextFromPdfAsync(byte[] pdfBytes);
        List<TextChunk> SplitIntoChunks(string text, int maxChunkSize = 250);
    }

    public class PdfProcessingService : IPdfProcessingService
    {
        public async Task<string> ExtractTextFromPdfAsync(byte[] pdfBytes)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var stream = new MemoryStream(pdfBytes);
                    using var pdfReader = new PdfReader(stream);
                    using var pdfDocument = new PdfDocument(pdfReader);
                    
                    var text = new StringBuilder();
                    
                    for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                    {
                        var page = pdfDocument.GetPage(i);
                        var pageText = PdfTextExtractor.GetTextFromPage(page);
                        text.AppendLine(pageText);
                    }
                    
                    return text.ToString();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to extract text from PDF: {ex.Message}", ex);
                }
            });
        }

        public List<TextChunk> SplitIntoChunks(string text, int maxChunkSize = 250)
        {
            var chunks = new List<TextChunk>();
            
            if (string.IsNullOrWhiteSpace(text))
                return chunks;

            // Clean up the text - normalize whitespace and remove excessive line breaks
            text = Regex.Replace(text, @"\s+", " ").Trim();
            
            // Split into sentences using regex that handles multiple sentence endings
            var sentences = Regex.Split(text, @"(?<=[.!?])\s+")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            var currentChunk = new StringBuilder();
            int startIndex = 0;
            
            foreach (var sentence in sentences)
            {
                // Check if adding this sentence would exceed the chunk size
                if (currentChunk.Length > 0 && 
                    currentChunk.Length + sentence.Length + 1 > maxChunkSize)
                {
                    // Create chunk from current content
                    var chunkText = currentChunk.ToString().Trim();
                    if (!string.IsNullOrEmpty(chunkText))
                    {
                        chunks.Add(new TextChunk
                        {
                            Text = chunkText,
                            StartIndex = startIndex,
                            EndIndex = startIndex + chunkText.Length - 1
                        });
                        
                        startIndex += chunkText.Length;
                    }
                    
                    // Start new chunk
                    currentChunk.Clear();
                }
                
                // Add sentence to current chunk
                if (currentChunk.Length > 0)
                    currentChunk.Append(" ");
                currentChunk.Append(sentence);
            }
            
            // Add the final chunk if it has content
            if (currentChunk.Length > 0)
            {
                var chunkText = currentChunk.ToString().Trim();
                chunks.Add(new TextChunk
                {
                    Text = chunkText,
                    StartIndex = startIndex,
                    EndIndex = startIndex + chunkText.Length - 1
                });
            }
            
            return chunks;
        }
    }
}