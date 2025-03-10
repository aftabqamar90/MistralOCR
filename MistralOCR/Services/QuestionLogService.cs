using Microsoft.EntityFrameworkCore;
using MistralOCR.Data;
using MistralOCR.Models;

namespace MistralOCR.Services
{
    public class QuestionLogService : IQuestionLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuestionLogService> _logger;

        public QuestionLogService(ApplicationDbContext context, ILogger<QuestionLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<DocumentQuestionLog>> GetLogsByDocumentIdAsync(int documentId)
        {
            return await _context.QuestionLogs
                .Where(q => q.DocumentId == documentId)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();
        }

        public async Task<DocumentQuestionLog> AddLogAsync(int documentId, string question, string answer, string model, int? promptTokens = null, int? completionTokens = null, int? totalTokens = null)
        {
            // Check if document exists
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null)
            {
                throw new ArgumentException($"Document with ID {documentId} not found");
            }

            var log = new DocumentQuestionLog
            {
                DocumentId = documentId,
                Question = question,
                Answer = answer,
                Model = model,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                TotalTokens = totalTokens,
                CreatedAt = DateTime.UtcNow
            };

            _context.QuestionLogs.Add(log);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Added new question log: {log.Id} for document {documentId}");
            
            return log;
        }

        public async Task<DocumentQuestionLog?> GetLogByIdAsync(int id)
        {
            return await _context.QuestionLogs
                .Include(q => q.Document)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<bool> DeleteLogAsync(int id)
        {
            var log = await _context.QuestionLogs.FindAsync(id);
            if (log == null)
            {
                return false;
            }

            _context.QuestionLogs.Remove(log);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Deleted question log: {id}");
            
            return true;
        }
    }
} 