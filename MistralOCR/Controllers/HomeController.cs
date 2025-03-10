using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MistralOCR.Models;
using MistralOCR.Services;

namespace MistralOCR.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IMistralService _mistralService;
    private readonly IDocumentService _documentService;

    public HomeController(
        ILogger<HomeController> logger, 
        IMistralService mistralService,
        IDocumentService documentService)
    {
        _logger = logger;
        _mistralService = mistralService;
        _documentService = documentService;
    }

    public IActionResult Index()
    {
        return View(new FileUploadViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Index(FileUploadViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.File == null || model.File.Length == 0)
        {
            ModelState.AddModelError("File", "Please select a file to upload");
            return View(model);
        }

        // Check if the file is a PDF
        if (!model.File.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("File", "Only PDF files are supported");
            return View(model);
        }

        using (var stream = model.File.OpenReadStream())
        {
            var result = await _mistralService.UploadPdfAsync(stream, model.File.FileName);
            model.UploadResult = result;
            
            // Store the file URL in the database if upload was successful
            if (result.IsSuccess && !string.IsNullOrEmpty(result.FileUrl))
            {
                var documentTitle = Path.GetFileNameWithoutExtension(model.File.FileName);
                await _documentService.AddDocumentAsync(result.FileUrl, documentTitle);
            }
        }

        return View(model);
    }

    [HttpPost]
    [Route("home/upload")]
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
                    
                    _logger.LogInformation($"File uploaded from home page and stored in database: {documentTitle}, URL: {result.FileUrl}");
                }
                else
                {
                    _logger.LogWarning($"File upload from home page succeeded but no URL was returned or operation failed: {result.ErrorMessage}");
                }
                
                return Ok(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file from home page");
            return StatusCode(500, new FileUploadResponse
            {
                IsSuccess = false,
                ErrorMessage = $"Error: {ex.Message}"
            });
        }
    }

    [HttpPost]
    [RequestFormLimits(MultipartBodyLengthLimit = 209715200)] // 200 MB
    public async Task<IActionResult> AskQuestion([FromBody] ChatCompletionRequest request)
    {
        if (string.IsNullOrEmpty(request.Question) || string.IsNullOrEmpty(request.DocumentUrl))
        {
            return BadRequest(new { error = "Question and document URL are required" });
        }

        try
        {
            _logger.LogInformation("Processing chat completion request with model: {Model}", request.Model);
            var result = await _mistralService.GetChatCompletionAsync(request.Question, request.DocumentUrl, request.Model);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat completion");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public IActionResult Ocr()
    {
        return View();
    }
    
    public async Task<IActionResult> ProcessDocument(int id)
    {
        var document = await _documentService.GetDocumentByIdAsync(id);
        
        if (document == null)
        {
            return NotFound();
        }
        
        return View(document);
    }
}

