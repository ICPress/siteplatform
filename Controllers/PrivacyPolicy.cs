using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using siteplatform.Models;

namespace siteplatform.Controllers;

[Route("privacy-policy")]
public class PrivacyPolicyController : Controller
{
    private readonly ILogger<PrivacyPolicyController> _logger;

    public PrivacyPolicyController(ILogger<PrivacyPolicyController> logger)
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