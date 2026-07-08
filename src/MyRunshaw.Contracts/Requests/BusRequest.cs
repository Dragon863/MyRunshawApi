using System.Text.Json.Serialization;

namespace MyRunshaw.Contracts.Requests;

public class ExtraBusRequest
{
    [JsonPropertyName("bus_number")]
    public string bus_number { get; set; } = string.Empty;
}