using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MyRunshaw.Domain.Entities;

public class TimetableCache
{
    public int Id { get; set; }

    public string StudentId { get; set; } = string.Empty;
    public User Student { get; set; } = null!;

    [Column(TypeName = "jsonb")] // EF Core will store this as JSONB in postgres
    public TimetableDocument Data { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class TimetableDocument
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("prodid")]
    public string ProdId { get; set; } = string.Empty;

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public List<TimetableEvent> Data { get; set; } = new();
}

public class TimetableEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("uid")]
    public string Uid { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("dtstart")]
    public TimetableDate DtStart { get; set; } = new();

    [JsonPropertyName("dtend")]
    public TimetableDate DtEnd { get; set; } = new();

    [JsonPropertyName("dtstamp")]
    public TimetableDate DtStamp { get; set; } = new();
}

// represents the nested {"dt": "time"}
public class TimetableDate
{
    [JsonPropertyName("dt")]
    public string Dt { get; set; } = string.Empty;
}