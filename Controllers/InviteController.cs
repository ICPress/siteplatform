using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using siteplatform.Models;

namespace siteplatform.Controllers;

[Route("invite")]
public class InviteController : Controller
{
    private readonly ILogger<InviteController> _logger;

    public InviteController(ILogger<InviteController> logger)
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