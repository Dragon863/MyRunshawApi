using System.Text.Json.Serialization;

namespace MyRunshaw.Contracts.Requests;

public class UpdateNameRequest
{
    [JsonPropertyName("new_name")]
    public string new_name { get; set; } = string.Empty;
}