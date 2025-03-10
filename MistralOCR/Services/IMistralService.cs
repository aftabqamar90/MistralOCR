using MistralOCR.Models;

namespace MistralOCR.Services
{
    public interface IMistralService
    {
        public Task<FileUploadResponse> UploadPdfAsync(Stream fileStream, string fileName);
        public Task<FileUploadResponse> GetFileUrlAsync(string fileId, int expiryHours = 24);
        public Task<ChatCompletionResponse> GetChatCompletionAsync(string question, string documentUrl, string model = "mistral-small-latest");
        public Task<Root> GetOcrAsync(string documentUrl, bool includeImageBase64 = false, string model = "mistral-ocr-latest");
    }
}
