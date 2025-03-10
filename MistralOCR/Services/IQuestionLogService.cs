using MistralOCR.Models;

namespace MistralOCR.Services
{
    public interface IQuestionLogService
    {
        Task<List<DocumentQuestionLog>> GetLogsByDocumentIdAsync(int documentId);
        Task<DocumentQuestionLog> AddLogAsync(int documentId, string question, string answer, string model, int? promptTokens = null, int? completionTokens = null, int? totalTokens = null);
        Task<DocumentQuestionLog?> GetLogByIdAsync(int id);
        Task<bool> DeleteLogAsync(int id);
    }
} 