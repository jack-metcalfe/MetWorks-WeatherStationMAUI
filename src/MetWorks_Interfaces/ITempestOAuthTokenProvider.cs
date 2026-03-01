namespace MetWorks.Interfaces;

public interface ITempestOAuthTokenProvider
{
    /// <summary>
    /// Returns a Tempest OAuth access token if one is available.
    /// If <paramref name="allowInteractive"/> is true and no cached token exists, the provider may prompt the user.
    /// </summary>
    Task<string?> GetAccessTokenAsync(bool allowInteractive, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears any cached token material.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
