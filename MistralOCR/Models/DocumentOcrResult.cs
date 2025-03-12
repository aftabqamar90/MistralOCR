using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace MistralOCR.Models
{
    public class DocumentOcrResult
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int DocumentId { get; set; }
        
        [ForeignKey("DocumentId")]
        public DocumentRecord? Document { get; set; }
        
        [Required]
        public string Model { get; set; } = string.Empty;
        
        [Required]
        public string OcrResultJson { get; set; } = string.Empty;
        
        public int PagesProcessed { get; set; }
        
        public int DocSizeBytes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Helper method to convert JSON to Root object
        [NotMapped]
        public Root? OcrResult 
        { 
            get 
            {
                if (string.IsNullOrEmpty(OcrResultJson))
                    return null;
                
                try
                {
                    return JsonSerializer.Deserialize<Root>(OcrResultJson);
                }
                catch
                {
                    return null;
                }
            }
        }
    }
} 