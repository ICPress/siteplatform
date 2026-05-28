using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using siteplatform.Models;

namespace siteplatform.Controllers;

[Route("[controller]")]
public class AccountController : Controller
{
    private readonly ILogger<AccountController> _logger;

    public AccountController(ILogger<AccountController> logger)
    {
        _logger = logger;
    }

    [ResponseCache(Duration = 520)]
    [HttpGet("verify")]
    public IActionResult Verify()
    {
        return View(("Verify your email &#9993;<br>Confirm using the button below on your phone &#128241;","Verify email"));
    }
}