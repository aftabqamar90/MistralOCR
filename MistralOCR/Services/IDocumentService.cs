using MistralOCR.Models;

namespace MistralOCR.Services
{
    public interface IDocumentService
    {
        Task<List<DocumentRecord>> GetAllDocumentsAsync();
        Task<DocumentRecord?> GetDocumentByIdAsync(int id);
        Task<DocumentRecord?> GetDocumentByUrlAsync(string url);
        Task<DocumentRecord> AddDocumentAsync(string url, string title, string? description = null);
        Task<DocumentRecord> UpdateDocumentProcessedAsync(int id);
        Task<bool> DeleteDocumentAsync(int id);
        
        // OCR result methods
        Task<DocumentOcrResult?> GetLatestOcrResultAsync(int documentId, string? model = null);
        Task<DocumentOcrResult> SaveOcrResultAsync(int documentId, Root ocrResult, string model);
    }
} 