using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using siteplatform.Models;

namespace siteplatform.Controllers;

[Route("[controller]")]
public class WalletController : Controller
{
    private readonly ILogger<WalletController> _logger;

    public WalletController(ILogger<WalletController> logger)
    {
        _logger = logger;
    }

    [ResponseCache(Duration = 520)]
    [HttpGet("verify")]
    public IActionResult Verify()
    {
        return View(("Verify your wallet &#128093;<br>Confirm using the button below on your phone &#128241;","Verify wallet"));
    }
}