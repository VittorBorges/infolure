namespace Infolure.Api.Features.Auth;

// DTOs de autenticação (US-04). Espelham contracts/api.yaml.

public record SyncUserRequest(
    string Provider,
    string ProviderUid,
    string? Email,
    string? DisplayName,
    string? AvatarUrl);

public record SyncUserResponse(Guid UserId, string? Username, bool NeedsUsername);

public record SetUsernameRequest(string Username);

public record SetUsernameResponse(string Username);

public enum SetUsernameResult { Ok, Taken, Invalid, UserNotFound }
