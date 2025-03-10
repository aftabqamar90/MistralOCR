namespace MistralOCR.Models
{
    public class AppSettings
    {
        public MistralAISettings MistralAI { get; set; } = new MistralAISettings();
        public FileUploadSettings FileUpload { get; set; } = new FileUploadSettings();
        public KestrelSettings Kestrel { get; set; } = new KestrelSettings();
    }

    public class MistralAISettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public MistralAIModels Models { get; set; } = new MistralAIModels();
        public MistralAITimeouts Timeouts { get; set; } = new MistralAITimeouts();
    }

    public class MistralAIModels
    {
        public string OCR { get; set; } = "mistral-ocr-latest";
        public string ChatSmall { get; set; } = "mistral-small-latest";
        public string ChatMedium { get; set; } = "mistral-medium-latest";
        public string ChatLarge { get; set; } = "mistral-large-latest";
    }

    public class MistralAITimeouts
    {
        public int RequestTimeoutSeconds { get; set; } = 180;
    }

    public class FileUploadSettings
    {
        public long MaxSizeBytes { get; set; } = 209715200;
        public int MaxSizeMB { get; set; } = 200;
    }

    public class KestrelSettings
    {
        public KestrelLimits Limits { get; set; } = new KestrelLimits();
    }

    public class KestrelLimits
    {
        public string KeepAliveTimeout { get; set; } = "00:03:00";
        public string RequestHeadersTimeout { get; set; } = "00:03:00";
        public long MaxRequestBodySize { get; set; } = 209715200;
    }
} 