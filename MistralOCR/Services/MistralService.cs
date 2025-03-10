using MistralOCR.Models;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;

namespace MistralOCR.Services
{
    public class MistralService : IMistralService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MistralService> _logger;
        private readonly HttpClient _httpClient;

        public MistralService(IConfiguration configuration, ILogger<MistralService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;

            // Get API key from configuration or environment variable
            string? apiKey = _configuration["MistralAI:ApiKey"];

            // If not found in configuration, try environment variable
            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_MISTRAL_API_KEY_HERE")
            {
                apiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY");
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Mistral API key is not configured. Please set it in appsettings.json or as an environment variable MISTRAL_API_KEY.");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.BaseAddress = new Uri("https://api.mistral.ai/v1/");
        }

        public async Task<FileUploadResponse> UploadPdfAsync(Stream fileStream, string fileName)
        {
            try
            {
                _logger.LogInformation($"Uploading file {fileName} to Mistral API");

                // Create multipart form content
                var formContent = new MultipartFormDataContent();
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

                formContent.Add(fileContent, "file", fileName);
                formContent.Add(new StringContent("ocr"), "purpose");

                // Send the request
                var response = await _httpClient.PostAsync("files", formContent);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var apiResponse = JsonSerializer.Deserialize<MistralApiResponse>(jsonResponse, options);

                    var fileUploadResponse = new FileUploadResponse
                    {
                        FileId = apiResponse?.Id ?? string.Empty,
                        FileName = fileName,
                        Purpose = apiResponse?.Purpose ?? string.Empty,
                        IsSuccess = true
                    };

                    // If we have a file ID, get the file URL
                    if (!string.IsNullOrEmpty(fileUploadResponse.FileId))
                    {
                        var urlResponse = await GetFileUrlAsync(fileUploadResponse.FileId);
                        if (urlResponse.IsSuccess)
                        {
                            fileUploadResponse.FileUrl = urlResponse.FileUrl;
                            fileUploadResponse.UrlExpiryTime = urlResponse.UrlExpiryTime;
                        }
                    }

                    return fileUploadResponse;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error uploading file: {errorContent}");

                    return new FileUploadResponse
                    {
                        FileName = fileName,
                        IsSuccess = false,
                        ErrorMessage = $"API Error: {response.StatusCode} - {errorContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception while uploading file {fileName}");

                return new FileUploadResponse
                {
                    FileName = fileName,
                    IsSuccess = false,
                    ErrorMessage = $"Exception: {ex.Message}"
                };
            }
        }

        public async Task<FileUploadResponse> GetFileUrlAsync(string fileId, int expiryHours = 24)
        {
            try
            {
                _logger.LogInformation($"Getting URL for file {fileId} with expiry of {expiryHours} hours");

                // Send the request to get the file URL
                var response = await _httpClient.GetAsync($"files/{fileId}/url?expiry={expiryHours}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var urlResponse = JsonSerializer.Deserialize<FileUrlResponse>(jsonResponse, options);

                    // Calculate expiry time
                    DateTime? expiryTime = null;
                    if (urlResponse?.ExpiresAt > 0)
                    {
                        // Convert Unix timestamp to DateTime
                        expiryTime = DateTimeOffset.FromUnixTimeSeconds(urlResponse.ExpiresAt).DateTime;
                    }

                    return new FileUploadResponse
                    {
                        FileId = fileId,
                        FileUrl = urlResponse?.Url ?? string.Empty,
                        UrlExpiryTime = expiryTime,
                        IsSuccess = true
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error getting file URL: {errorContent}");

                    return new FileUploadResponse
                    {
                        FileId = fileId,
                        IsSuccess = false,
                        ErrorMessage = $"API Error: {response.StatusCode} - {errorContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception while getting URL for file {fileId}");

                return new FileUploadResponse
                {
                    FileId = fileId,
                    IsSuccess = false,
                    ErrorMessage = $"Exception: {ex.Message}"
                };
            }
        }

        public async Task<ChatCompletionResponse> GetChatCompletionAsync(string question, string documentUrl, string model = "mistral-small-latest")
        {
            try
            {
                _logger.LogInformation($"Getting chat completion for question: {question}");
                
                // Create the request payload
                var requestPayload = new
                {
                    model = model,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new
                                {
                                    type = "text",
                                    text = question
                                },
                                new
                                {
                                    type = "document_url",
                                    document_url = documentUrl
                                }
                            }
                        }
                    },
                    document_image_limit = 8,
                    document_page_limit = 64
                };
                
                var jsonContent = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                // Send the request
                var response = await _httpClient.PostAsync("chat/completions", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Chat completion response: {jsonResponse}");
                    
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var apiResponse = JsonSerializer.Deserialize<ChatApiResponse>(jsonResponse, options);
                    
                    return new ChatCompletionResponse
                    {
                        Id = apiResponse?.Id ?? string.Empty,
                        Model = apiResponse?.Model ?? string.Empty,
                        Answer = apiResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty,
                        Usage = apiResponse?.Usage != null ? new Usage
                        {
                            PromptTokens = apiResponse.Usage.PromptTokens,
                            CompletionTokens = apiResponse.Usage.CompletionTokens,
                            TotalTokens = apiResponse.Usage.TotalTokens
                        } : null,
                        IsSuccess = true
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error getting chat completion: {errorContent}");
                    
                    return new ChatCompletionResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"API Error: {response.StatusCode} - {errorContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception while getting chat completion");
                
                return new ChatCompletionResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"Exception: {ex.Message}"
                };
            }
        }

        public async Task<OcrResponse> GetOcrAsync(string documentUrl, bool includeImageBase64 = false, string model = "mistral-ocr-latest")
        {
            try
            {
                _logger.LogInformation($"Performing OCR on document: {documentUrl} using model: {model}");

                var request = new OcrRequest
                {
                    Model = model,
                    Document = new OcrDocument
                    {
                        Type = "document_url",
                        DocumentUrl = documentUrl
                    },
                    IncludeImageBase64 = includeImageBase64
                };

                var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("ocr", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"OCR API error: {response.StatusCode}, {responseBody}");
                    return new OcrResponse
                    {
                        IsSuccess = false,
                        Error = $"API Error: {response.StatusCode} - {responseBody}"
                    };
                }

                var ocrResponse = JsonSerializer.Deserialize<OcrResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (ocrResponse == null)
                {
                    return new OcrResponse
                    {
                        IsSuccess = false,
                        Error = "Failed to deserialize OCR response"
                    };
                }

                ocrResponse.IsSuccess = true;
                return ocrResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing OCR");
                return new OcrResponse
                {
                    IsSuccess = false,
                    Error = $"Exception: {ex.Message}"
                };
            }
        }

        // Internal class to deserialize the Mistral API response
        private class MistralApiResponse
        {
            public string Id { get; set; } = string.Empty;
            public string Purpose { get; set; } = string.Empty;
        }

        // Internal class to deserialize the file URL response
        private class FileUrlResponse
        {
            public string Url { get; set; } = string.Empty;
            public long ExpiresAt { get; set; }
        }

        // Internal classes to deserialize the chat completion response
        private class ChatApiResponse
        {
            public string Id { get; set; } = string.Empty;
            public string Model { get; set; } = string.Empty;
            public ChatChoice[]? Choices { get; set; }
            public ChatUsage? Usage { get; set; }
        }

        private class ChatChoice
        {
            public int Index { get; set; }
            public ChatMessage? Message { get; set; }
            public string? FinishReason { get; set; }
        }

        private class ChatMessage
        {
            public string Role { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
        }

        private class ChatUsage
        {
            [JsonPropertyName("prompt_tokens")]
            public int PromptTokens { get; set; }
            
            [JsonPropertyName("completion_tokens")]
            public int CompletionTokens { get; set; }
            
            [JsonPropertyName("total_tokens")]
            public int TotalTokens { get; set; }
        }
    }
}