using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MistralOCR.Models;
using MistralOCR.Services;

namespace MistralOCR.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IMistralService _mistralService;

    public HomeController(ILogger<HomeController> logger, IMistralService mistralService)
    {
        _logger = logger;
        _mistralService = mistralService;
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
        }

        return View(model);
    }
    
    [HttpPost]
    public async Task<IActionResult> AskQuestion([FromBody] ChatCompletionRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Question) || string.IsNullOrWhiteSpace(request.DocumentUrl))
        {
            return BadRequest(new { success = false, message = "Question and document URL are required" });
        }
        
        try
        {
            var chatResponse = await _mistralService.GetChatCompletionAsync(
                request.Question,
                request.DocumentUrl
            );
            
            return Json(chatResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat completion request");
            return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
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
}

