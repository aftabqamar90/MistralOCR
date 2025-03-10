using System.ComponentModel.DataAnnotations;

namespace MistralOCR.Models
{
    public class DocumentRecord
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Url { get; set; } = string.Empty;
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastProcessedAt { get; set; }
        
        public int ProcessCount { get; set; } = 0;
        
        // Navigation property for question logs
        public ICollection<DocumentQuestionLog> QuestionLogs { get; set; } = new List<DocumentQuestionLog>();
    }
} 