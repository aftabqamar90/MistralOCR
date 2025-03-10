using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MistralOCR.Models
{
    public class DocumentQuestionLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int DocumentId { get; set; }
        
        [ForeignKey("DocumentId")]
        public DocumentRecord? Document { get; set; }
        
        [Required]
        public string Question { get; set; } = string.Empty;
        
        [Required]
        public string Answer { get; set; } = string.Empty;
        
        [Required]
        public string Model { get; set; } = string.Empty;
        
        public int? PromptTokens { get; set; }
        
        public int? CompletionTokens { get; set; }
        
        public int? TotalTokens { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
} 