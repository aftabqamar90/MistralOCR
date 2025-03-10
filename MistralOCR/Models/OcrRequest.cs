using System.Text.Json.Serialization;

namespace MistralOCR.Models
{
    public class OcrRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "mistral-ocr-latest";

        [JsonPropertyName("document")]
        public OcrDocument Document { get; set; } = new OcrDocument();

        [JsonPropertyName("include_image_base64")]
        public bool IncludeImageBase64 { get; set; } = false;
    }

    public class OcrDocument
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "document_url";

        [JsonPropertyName("document_url")]
        public string DocumentUrl { get; set; } = string.Empty;

        // For file upload support (not used in the current implementation)
        [JsonPropertyName("document_base64")]
        public string? DocumentBase64 { get; set; }
    }
} 