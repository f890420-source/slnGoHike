namespace prjGoHike.Models;

public sealed class PasswordResetSettings
{
    public string FrontendUrl { get; set; } = "http://localhost:4200";
    public int ExpiryMinutes { get; set; } = 30;
}

public sealed class EmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "GoHike";
}
