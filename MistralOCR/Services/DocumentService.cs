using Microsoft.EntityFrameworkCore;
using MistralOCR.Data;
using MistralOCR.Models;
using System.Text.Json;

namespace MistralOCR.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(ApplicationDbContext context, ILogger<DocumentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<DocumentRecord>> GetAllDocumentsAsync()
        {
            return await _context.Documents
                .OrderByDescending(d => d.LastProcessedAt ?? d.CreatedAt)
                .ToListAsync();
        }

        public async Task<DocumentRecord?> GetDocumentByIdAsync(int id)
        {
            return await _context.Documents.FindAsync(id);
        }

        public async Task<DocumentRecord?> GetDocumentByUrlAsync(string url)
        {
            return await _context.Documents.FirstOrDefaultAsync(d => d.Url == url);
        }

        public async Task<DocumentRecord> AddDocumentAsync(string url, string title, string? description = null)
        {
            // Check if document with this URL already exists
            var existingDocument = await GetDocumentByUrlAsync(url);
            if (existingDocument != null)
            {
                return existingDocument;
            }

            var document = new DocumentRecord
            {
                Url = url,
                Title = title,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Added new document record: {document.Id} - {document.Title}");
            
            return document;
        }

        public async Task<DocumentRecord> UpdateDocumentProcessedAsync(int id)
        {
            var document = await GetDocumentByIdAsync(id);
            if (document == null)
            {
                throw new ArgumentException($"Document with ID {id} not found");
            }

            document.LastProcessedAt = DateTime.UtcNow;
            document.ProcessCount++;

            _context.Documents.Update(document);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Updated document record: {document.Id} - Process count: {document.ProcessCount}");
            
            return document;
        }

        public async Task<bool> DeleteDocumentAsync(int id)
        {
            var document = await GetDocumentByIdAsync(id);
            if (document == null)
            {
                return false;
            }

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Deleted document record: {id}");
            
            return true;
        }
        
        // OCR result methods
        public async Task<DocumentOcrResult?> GetLatestOcrResultAsync(int documentId, string? model = null)
        {
            var query = _context.OcrResults
                .Where(o => o.DocumentId == documentId);
                
            // If model is specified, filter by model
            if (!string.IsNullOrEmpty(model))
            {
                query = query.Where(o => o.Model == model);
            }
            
            // Get the latest OCR result
            return await query
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
        }
        
        public async Task<DocumentOcrResult> SaveOcrResultAsync(int documentId, Root ocrResult, string model)
        {
            // Check if document exists
            var document = await GetDocumentByIdAsync(documentId);
            if (document == null)
            {
                throw new ArgumentException($"Document with ID {documentId} not found");
            }
            
            // Serialize the OCR result to JSON
            var ocrResultJson = JsonSerializer.Serialize(ocrResult);
            
            // Create a new OCR result record
            var ocrResultRecord = new DocumentOcrResult
            {
                DocumentId = documentId,
                Model = model,
                OcrResultJson = ocrResultJson,
                PagesProcessed = ocrResult.usage_info?.pages_processed ?? 0,
                DocSizeBytes = ocrResult.usage_info?.doc_size_bytes ?? 0,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.OcrResults.Add(ocrResultRecord);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Saved OCR result for document {documentId} using model {model}");
            
            return ocrResultRecord;
        }
    }
} 