namespace MetWorks.Interfaces;

/// <summary>
/// Published when a component requires the user to complete Tempest OAuth interactive authorization.
/// Intended to be handled by the UI layer (e.g., a ViewModel) which can invoke
/// <see cref="ITempestOAuthTokenProvider.GetAccessTokenAsync"/> with <c>allowInteractive: true</c>.
/// </summary>
public sealed record TempestOAuthInteractiveAuthRequest(
    DateTimeOffset RequestedUtc,
    string Reason
);
