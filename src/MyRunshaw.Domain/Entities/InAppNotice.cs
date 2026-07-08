using System.ComponentModel.DataAnnotations;

namespace MyRunshaw.Domain.Entities;

public class InAppNotice
{
    [Key]
    public string NoticeId { get; set; } = string.Empty;

    public bool Android { get; set; } = true; // show on Android? (default yes)
    public bool Ios { get; set; } = true; // show on iOS? (default yes)
    public string Title { get; set; } = string.Empty; // title of the notice
    public string Description { get; set; } = string.Empty; // description of the notice
    public DateTime Expires { get; set; } = DateTime.UtcNow.AddDays(7); // default to 7 days from now
    public string? ImageUrl { get; set; } // optional image URL for the notice
    public string? Link { get; set; } // optional link for the notice
    public string? LinkText { get; set; } // optional link text for the notice
    public string? MinVersion { get; set; } // optional minimum app version for the notice
    public string? MaxVersion { get; set; } // optional maximum app version for the notice
}