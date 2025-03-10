using System.ComponentModel.DataAnnotations;

namespace MistralOCR.Models
{
    public class FileUploadViewModel
    {
        [Required(ErrorMessage = "Please select a file to upload")]
        public IFormFile? File { get; set; }
        
        public FileUploadResponse? UploadResult { get; set; }
        
        public ChatCompletionRequest? ChatRequest { get; set; }
        
        public ChatCompletionResponse? ChatResponse { get; set; }
    }
} 