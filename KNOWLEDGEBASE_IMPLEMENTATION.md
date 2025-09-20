# PDF Knowledgebase Upload Feature - Implementation Guide

## Overview
This implementation adds a comprehensive PDF upload and processing system to your Blazor WebAssembly application. The system converts PDF files to text, splits them into chunks, generates embeddings, and stores everything locally using Blazored.LocalStorage.

## Features Implemented

### 1. PDF Upload & Processing
- **Upload Component**: Radzen-based file upload with progress tracking
- **PDF Text Extraction**: Uses iText7 library to extract text from PDF files
- **Text Chunking**: Splits text into 250-character chunks at sentence boundaries
- **File Size Limit**: 10MB maximum file size
- **File Type Validation**: Accepts only PDF files

### 2. OpenAI Integration
- **Embeddings Generation**: Creates embeddings for both original text and chunks
- **Configuration**: Stores OpenAI settings in appsettings.json
- **Error Handling**: Comprehensive error handling for API calls
- **Retry Logic**: Configurable retry settings

### 3. Local Storage
- **Persistent Storage**: Uses Blazored.LocalStorage for client-side persistence
- **Data Structure**: JSON-based knowledgebase format
- **Automatic Persistence**: Data survives application restarts
- **CRUD Operations**: Add, remove, and clear knowledgebase items

### 4. User Interface
- **Modern UI**: Built with Radzen Blazor components
- **Progress Tracking**: Real-time progress during PDF processing
- **Data Grid**: View uploaded documents with previews
- **Navigation**: New "Knowledgebase" menu item
- **Responsive Design**: Works on various screen sizes

## Files Created/Modified

### New Files
1. **Models/**
   - `OpenAIConfiguration.cs` - Configuration model for OpenAI settings
   - `KnowledgebaseModels.cs` - Data models for knowledgebase items and chunks

2. **Services/**
   - `PdfProcessingService.cs` - PDF text extraction and chunking
   - `EmbeddingService.cs` - OpenAI embedding generation
   - `KnowledgebaseStorageService.cs` - Local storage operations

3. **Components/**
   - `KnowledgebaseUpload.razor` - Main upload component with UI

4. **Pages/**
   - `Knowledgebase.razor` - Dedicated page for knowledgebase management

### Modified Files
1. `BlazorWebApp.Client.csproj` - Added required NuGet packages
2. `BlazorWebApp.csproj` - Added OpenAI package reference
3. `_Imports.razor` - Added namespace imports
4. `Program.cs` (Client) - Configured dependency injection
5. `NavMenu.razor` - Added knowledgebase navigation link
6. `appsettings.json` (both server and client) - Added OpenAI configuration

## Configuration Required

### 1. OpenAI API Key
Update the `appsettings.json` files with your OpenAI API key:

```json
{
  "OpenAI": {
    "ApiKey": "your-actual-openai-api-key-here",
    "Endpoint": "https://api.openai.com/v1",
    "EmbeddingModel": "text-embedding-3-small",
    "MaxRetries": 3,
    "TimeoutSeconds": 30
  }
}
```

### 2. Package Versions
The implementation uses these key packages:
- `Blazored.LocalStorage` v4.5.0
- `Microsoft.Extensions.AI.OpenAI` v9.0.1-preview.1.24570.5
- `OpenAI` v2.1.0
- `iText7` v8.0.5
- `Radzen.Blazor` v5.6.6

## Usage Instructions

### 1. Navigate to Knowledgebase
- Use the navigation menu to go to the "Knowledgebase" page
- The page shows the upload interface and existing documents

### 2. Upload a PDF
- Click "Choose File" and select a PDF (max 10MB)
- Click "Upload Knowledgebase Content"
- Watch the progress bar during processing
- View the uploaded document in the data grid

### 3. Manage Documents
- View document details: filename, upload date, text preview, chunk count
- Remove individual documents using the delete button
- Clear entire knowledgebase using "Clear Knowledgebase" button

## Technical Details

### Data Flow
1. User selects PDF file
2. File bytes are read into memory
3. Text is extracted using iText7
4. Text is split into 250-character chunks at sentence boundaries
5. OpenAI embeddings are generated for original text and all chunks
6. Data is serialized to JSON and stored in browser local storage
7. UI is updated to show the new document

### Storage Format
```json
{
  "version": "1.0",
  "lastUpdated": "2025-09-19T10:30:00Z",
  "items": [
    {
      "id": "guid",
      "fileName": "document.pdf",
      "originalText": "Full extracted text...",
      "originalTextEmbedding": [0.1, 0.2, ...],
      "chunks": [
        {
          "id": "guid",
          "text": "Chunk text...",
          "embedding": [0.1, 0.2, ...],
          "startIndex": 0,
          "endIndex": 249
        }
      ],
      "createdAt": "2025-09-19T10:30:00Z"
    }
  ]
}
```

### Error Handling
- PDF parsing errors are caught and displayed to users
- OpenAI API errors are handled with appropriate error messages
- File size and type validation prevents invalid uploads
- Network timeouts and retries are configured

## Next Steps

### Potential Enhancements
1. **Search Functionality**: Add semantic search using the stored embeddings
2. **Batch Upload**: Support multiple file uploads
3. **Export/Import**: Allow knowledgebase backup and restore
4. **Document Management**: Add editing, tagging, and categorization
5. **Analytics**: Show storage usage and document statistics
6. **Advanced Chunking**: Support different chunking strategies
7. **File Type Support**: Add support for other document types (Word, Text, etc.)

### Performance Optimizations
1. **Streaming**: Process large files in chunks
2. **Background Processing**: Move embedding generation to web workers
3. **Caching**: Cache embeddings to avoid regeneration
4. **Compression**: Compress stored data to save space

## Troubleshooting

### Common Issues
1. **OpenAI API Errors**: Check API key and network connectivity
2. **PDF Processing Errors**: Ensure PDF is not password-protected or corrupted
3. **Storage Full**: Browser storage has limits; implement cleanup
4. **Performance**: Large files may cause browser slowdowns

### Development Tips
1. Test with small PDF files first
2. Monitor browser console for errors
3. Check network tab for API call issues
4. Use browser dev tools to inspect local storage

This implementation provides a solid foundation for a PDF-based knowledgebase system with modern UI and robust error handling.