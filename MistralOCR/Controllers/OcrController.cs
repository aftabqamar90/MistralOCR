using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MistralOCR.Models;
using MistralOCR.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace MistralOCR.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OcrController : ControllerBase
    {
        private readonly ILogger<OcrController> _logger;
        private readonly IMistralService _mistralService;
        private readonly IDocumentService _documentService;
        private readonly AppSettings _appSettings;

        public OcrController(
            ILogger<OcrController> logger, 
            IMistralService mistralService,
            IDocumentService documentService,
            IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _mistralService = mistralService;
            _documentService = documentService;
            _appSettings = appSettings.Value;
        }

        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 209715200)] // Set from configuration in Program.cs
        public async Task<IActionResult> PerformOcr([FromBody] OcrRequest request)
        {
            if (string.IsNullOrEmpty(request.Document.DocumentUrl))
            {
                return BadRequest("Document URL is required");
            }

            try
            {
                var result = await _mistralService.GetOcrAsync(
                    request.Document.DocumentUrl,
                    request.IncludeImageBase64,
                    request.Model);

                if (!result.IsSuccess)
                {
                    return StatusCode(500, new { error = result.Error });
                }

                // Store the document URL
                var documentTitle = ExtractTitleFromUrl(request.Document.DocumentUrl);
                await _documentService.AddDocumentAsync(request.Document.DocumentUrl, documentTitle);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing OCR");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("url")]
        [RequestFormLimits(MultipartBodyLengthLimit = 209715200)] // Set from configuration in Program.cs
        public async Task<IActionResult> PerformOcrFromUrl([FromQuery] string url, [FromQuery] bool includeImages = false, [FromQuery] string? model = null)
        {
            if (string.IsNullOrEmpty(url))
            {
                return BadRequest("URL is required");
            }

            try
            {
                // Use the model from the request or the default from configuration
                model ??= _appSettings.MistralAI.Models.OCR;
                
                // Log the request details
                _logger.LogInformation($"Processing OCR request for URL: {url}, includeImages: {includeImages}, model: {model}");
                
                var result = await _mistralService.GetOcrAsync(url, includeImages, model);

                if (!result.IsSuccess)
                {
                    _logger.LogError($"OCR processing failed: {result.Error}");
                    return StatusCode(500, new { error = result.Error });
                }

                // Store the document URL
                var documentTitle = ExtractTitleFromUrl(url);
                var document = await _documentService.AddDocumentAsync(url, documentTitle);
                
                // Update the document as processed
                await _documentService.UpdateDocumentProcessedAsync(document.Id);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing OCR from URL");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new FileUploadResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "No file was uploaded"
                });
            }

            // Check if the file is a PDF
            if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new FileUploadResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "Only PDF files are supported"
                });
            }

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    var result = await _mistralService.UploadPdfAsync(stream, file.FileName);
                    
                    if (result.IsSuccess && !string.IsNullOrEmpty(result.FileUrl))
                    {
                        // Store the document URL in the database
                        var documentTitle = Path.GetFileNameWithoutExtension(file.FileName);
                        var document = await _documentService.AddDocumentAsync(result.FileUrl, documentTitle);
                        
                        // Update the document as processed
                        await _documentService.UpdateDocumentProcessedAsync(document.Id);
                        
                        _logger.LogInformation($"File uploaded and stored in database: {documentTitle}, URL: {result.FileUrl}");
                    }
                    else
                    {
                        _logger.LogWarning($"File upload succeeded but no URL was returned or operation failed: {result.ErrorMessage}");
                    }
                    
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return StatusCode(500, new FileUploadResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"Error: {ex.Message}"
                });
            }
        }

        [HttpGet("documents")]
        public async Task<IActionResult> GetDocuments()
        {
            try
            {
                var documents = await _documentService.GetAllDocumentsAsync();
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("documents/{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            try
            {
                var result = await _documentService.DeleteDocumentAsync(id);
                if (!result)
                {
                    return NotFound();
                }
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting document {id}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private string ExtractTitleFromUrl(string url)
        {
            try
            {
                // Try to extract a meaningful title from the URL
                var uri = new Uri(url);
                var path = uri.AbsolutePath;
                var fileName = System.IO.Path.GetFileName(path);

                // If we have a filename, use it as the title
                if (!string.IsNullOrEmpty(fileName))
                {
                    // Remove extension and replace special characters
                    var title = System.IO.Path.GetFileNameWithoutExtension(fileName);
                    title = title.Replace('-', ' ').Replace('_', ' ');
                    
                    // Capitalize first letter of each word
                    var textInfo = new System.Globalization.CultureInfo("en-US", false).TextInfo;
                    title = textInfo.ToTitleCase(title);
                    
                    if (!string.IsNullOrEmpty(title))
                    {
                        return title;
                    }
                }

                // If we couldn't extract a title, use the host
                return $"Document from {uri.Host}";
            }
            catch
            {
                // If parsing fails, return a generic title
                return "Document";
            }
        }
    }
} 