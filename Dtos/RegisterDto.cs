namespace mhrc.Dtos;

public record RegisterDto(string Email, string Password);

public record LoginDto(string Email, string Password, bool RememberMe);
