using System.Text.Json.Serialization;

namespace MyRunshaw.Contracts.Requests;

public class BlockFriendBody
{
    [JsonPropertyName("blocked_id")]
    public string blocked_id { get; set; } = string.Empty;
}