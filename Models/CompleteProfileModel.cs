using System.ComponentModel.DataAnnotations;
namespace siteplatform.Models;

public sealed class CompleteProfileModel
{
    [StringLength(2048)]
    public string? AvatarTicket { get; set; }

    [Required(ErrorMessage = "Choose a username.")]
    [RegularExpression("^[a-zA-Z0-9_]{3,20}$", ErrorMessage = "Use 3–20 letters, numbers or underscores.")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Enter your full name.")]
    [StringLength(200, ErrorMessage = "Your full name must be at most 200 characters.")]
    public string FullName { get; set; } = "";
}
