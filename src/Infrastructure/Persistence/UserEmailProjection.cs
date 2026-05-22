namespace Infrastructure.Persistence;

public sealed class UserEmailProjection
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
}
