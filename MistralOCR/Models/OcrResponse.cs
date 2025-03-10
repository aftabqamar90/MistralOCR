using System.Text.Json.Serialization;

namespace MistralOCR.Models
{

    public class OcrResponse
    {
        public int dpi { get; set; }
        public int height { get; set; }
        public int width { get; set; }
    }

    public class Image
    {
        public string id { get; set; } = string.Empty;
        public int top_left_x { get; set; }
        public int top_left_y { get; set; }
        public int bottom_right_x { get; set; }
        public int bottom_right_y { get; set; }
        public string image_base64 { get; set; } = string.Empty;
    }

    public class Page
    {
        public int index { get; set; }
        public string markdown { get; set; } = string.Empty;
        public List<Image> images { get; set; } = new List<Image>();
        public OcrResponse dimensions { get; set; } = new OcrResponse();
    }

    public class Root
    {
        public bool IsSuccess { get; set; } = false;
        public string Error { get; set; } = string.Empty;
        public List<Page> pages { get; set; } = new List<Page>();
        public string model { get; set; } = string.Empty;
        public UsageInfo usage_info { get; set; } = new UsageInfo();
    }

    public class UsageInfo
    {
        public int pages_processed { get; set; }
        public int doc_size_bytes { get; set; }
    }
}