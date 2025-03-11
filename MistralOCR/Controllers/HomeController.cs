using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MistralOCR.Models;
using MistralOCR.Services;
using Microsoft.Extensions.Options;

namespace MistralOCR.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IMistralService _mistralService;
    private readonly IDocumentService _documentService;
    private readonly IQuestionLogService _questionLogService;
    private readonly AppSettings _appSettings;

    public HomeController(
        ILogger<HomeController> logger, 
        IMistralService mistralService,
        IDocumentService documentService,
        IQuestionLogService questionLogService,
        IOptions<AppSettings> appSettings)
    {
        _logger = logger;
        _mistralService = mistralService;
        _documentService = documentService;
        _questionLogService = questionLogService;
        _appSettings = appSettings.Value;
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
                    
                    // Add the document ID to the response
                    result.DocumentId = document.Id;
                    
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
    [RequestFormLimits(MultipartBodyLengthLimit = 209715200)] // Set from configuration in Program.cs
    public async Task<IActionResult> AskQuestion([FromBody] ChatCompletionRequest request)
    {
        if (string.IsNullOrEmpty(request.Question) || string.IsNullOrEmpty(request.DocumentUrl))
        {
            return BadRequest(new { error = "Question and document URL are required" });
        }

        try
        {
            // Use the model from the request or the default from configuration
            if (string.IsNullOrEmpty(request.Model))
            {
                request.Model = _appSettings.MistralAI.Models.ChatSmall;
            }
            
            _logger.LogInformation("Processing chat completion request with model: {Model}", request.Model);
            var result = await _mistralService.GetChatCompletionAsync(request.Question, request.DocumentUrl, request.Model);
            
            // If successful, save the question and answer to the database
            if (result.IsSuccess)
            {
                // Get the document by URL
                var document = await _documentService.GetDocumentByUrlAsync(request.DocumentUrl);
                if (document != null)
                {
                    // Save the question log
                    await _questionLogService.AddLogAsync(
                        document.Id,
                        request.Question,
                        result.Answer,
                        request.Model,
                        result.Usage?.PromptTokens,
                        result.Usage?.CompletionTokens,
                        result.Usage?.TotalTokens
                    );
                    
                    _logger.LogInformation("Saved question log for document {DocumentId}", document.Id);
                }
            }
            
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

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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
    
    public async Task<IActionResult> DocumentLogs(int id)
    {
        var document = await _documentService.GetDocumentByIdAsync(id);
        
        if (document == null)
        {
            return NotFound();
        }
        
        // Get all question logs for this document
        var logs = await _questionLogService.GetLogsByDocumentIdAsync(id);
        
        // Create a view model with both document and logs
        var viewModel = new DocumentLogsViewModel
        {
            Document = document,
            QuestionLogs = logs
        };
        
        return View(viewModel);
    }
}

