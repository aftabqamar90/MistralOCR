namespace MistralOCR.Models
{
    public class FileUploadResponse
    {
        public string FileId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public DateTime? UrlExpiryTime { get; set; }
    }
} 