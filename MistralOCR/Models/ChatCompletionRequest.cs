namespace MistralOCR.Models
{
    public class ChatCompletionRequest
    {
        public string Question { get; set; } = string.Empty;
        public string DocumentUrl { get; set; } = string.Empty;
        public string Model { get; set; } = "mistral-small-latest";
    }
} 