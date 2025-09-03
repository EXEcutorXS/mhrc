using System.Text.Json.Serialization;

public class AuthRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; }
    
    [JsonPropertyName("password")]
    public string Password { get; set; }
    
    [JsonPropertyName("clientid")]
    public string ClientId { get; set; }
    
    [JsonPropertyName("topic")]
    public string Topic { get; set; }
    
    [JsonPropertyName("acc")]
    public int Acc { get; set; }
}

public class AuthResponse
{
    [JsonPropertyName("Ok")]
    public bool Ok { get; set; }
    
    [JsonPropertyName("Error")]
    public string Error { get; set; }
}

public class AclRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; }
    
    [JsonPropertyName("topic")]
    public string Topic { get; set; }
    
    [JsonPropertyName("clientid")]
    public string ClientId { get; set; }
    
    [JsonPropertyName("acc")]
    public int Acc { get; set; } // 1: read, 2: write
}

public record RegisterDto(string Username, string Email, string Password);

public record LoginDto(string Username, string Email, string Password, bool RememberMe);