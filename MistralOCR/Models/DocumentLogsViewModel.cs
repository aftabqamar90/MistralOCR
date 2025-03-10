namespace MistralOCR.Models
{
    public class DocumentLogsViewModel
    {
        public DocumentRecord Document { get; set; } = null!;
        public List<DocumentQuestionLog> QuestionLogs { get; set; } = new List<DocumentQuestionLog>();
    }
} 