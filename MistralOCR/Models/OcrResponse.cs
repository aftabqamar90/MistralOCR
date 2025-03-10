using System.Text.Json.Serialization;

namespace MistralOCR.Models
{
    public class OcrResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public OcrPage[] Pages { get; set; } = Array.Empty<OcrPage>();
        public string? Error { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class OcrPage
    {
        public int PageNumber { get; set; }
        public string Text { get; set; } = string.Empty;
        
        [JsonPropertyName("image_base64")]
        public string? ImageBase64 { get; set; }
        
        public OcrBlock[] Blocks { get; set; } = Array.Empty<OcrBlock>();
    }

    public class OcrBlock
    {
        public string Text { get; set; } = string.Empty;
        public OcrBoundingBox BoundingBox { get; set; } = new OcrBoundingBox();
        public OcrLine[] Lines { get; set; } = Array.Empty<OcrLine>();
    }

    public class OcrLine
    {
        public string Text { get; set; } = string.Empty;
        public OcrBoundingBox BoundingBox { get; set; } = new OcrBoundingBox();
        public OcrWord[] Words { get; set; } = Array.Empty<OcrWord>();
    }

    public class OcrWord
    {
        public string Text { get; set; } = string.Empty;
        public OcrBoundingBox BoundingBox { get; set; } = new OcrBoundingBox();
    }

    public class OcrBoundingBox
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }
} 