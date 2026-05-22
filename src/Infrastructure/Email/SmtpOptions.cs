namespace Infrastructure.Email;

internal sealed class SmtpOptions
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 1025;
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public string FromAddress { get; init; } = "noreply@hankkarpinen.com";
    public string FromName { get; init; } = "hankkarpinen.com";
    public string BaseUrl { get; init; } = "http://localhost:3000";
}
