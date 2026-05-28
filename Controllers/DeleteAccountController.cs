using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using siteplatform.Models;

namespace siteplatform.Controllers;

[Route("delete-data")]
public class DeleteAccountController : Controller
{
    private readonly ILogger<DeleteAccountController> _logger;

    public DeleteAccountController(ILogger<DeleteAccountController> logger)
    {
        _logger = logger;
    }

    [ResponseCache(Duration = 520)]
    [HttpGet()]
    public IActionResult Index()
    {
        return View();
    }
}