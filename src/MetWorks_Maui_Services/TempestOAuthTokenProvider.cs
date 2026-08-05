namespace MetWorks.Maui.Services;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using MetWorks.Constants;
using MetWorks.Interfaces;

using Microsoft.Maui.Authentication;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

public sealed class TempestOAuthTokenProvider : ITempestOAuthTokenProvider
{
    // Must be kept in sync with Android intent filter registration.
    public const string RedirectUriScheme = "metworks-weatherstation";

    const string TempestAuthorizeUrl = "https://smartweather.weatherflow.com/authorize.html";
    const string TempestTokenUrl = "https://swd.weatherflow.com/id/oauth2/token";

    const string SecureKey_AccessToken = "tempest.oauth.access_token";
    const string SecureKey_RefreshToken = "tempest.oauth.refresh_token";
    const string SecureKey_ExpiresUtc = "tempest.oauth.expires_utc";

    readonly SemaphoreSlim _authLock = new(1, 1);

    MetWorks.Interfaces.ILogger? _logger;
    ISettingRepository? _settingRepository;

    public TempestOAuthTokenProvider() { }

    public Task<bool> InitializeAsync(
        MetWorks.Interfaces.ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        CancellationToken externalCancellation = default
    )
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);

        _logger = iLogger.ForContext(GetType());
        _settingRepository = iSettingRepository;

        return Task.FromResult(true);
    }

    public async Task<string?> GetAccessTokenAsync(bool allowInteractive, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var token = await TryGetCachedAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(token))
            return token;

        if (!allowInteractive)
            return null;

        await _authLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            token = await TryGetCachedAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(token))
                return token;

            return await AuthenticateAndCacheAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _authLock.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var logger = _logger;
        if (logger is null)
            throw new InvalidOperationException($"{nameof(TempestOAuthTokenProvider)} is not initialized.");

        try { SecureStorage.Remove(SecureKey_AccessToken); }
        catch (InvalidOperationException ex) { logger.Warning($"Tempest OAuth: failed to clear access token. {ex.Message}"); throw; }
        catch (NotSupportedException ex) { logger.Warning($"Tempest OAuth: secure storage is not supported. {ex.Message}"); throw; }

        try { SecureStorage.Remove(SecureKey_RefreshToken); }
        catch (InvalidOperationException ex) { logger.Warning($"Tempest OAuth: failed to clear refresh token. {ex.Message}"); throw; }
        catch (NotSupportedException ex) { logger.Warning($"Tempest OAuth: secure storage is not supported. {ex.Message}"); throw; }

        try { SecureStorage.Remove(SecureKey_ExpiresUtc); }
        catch (InvalidOperationException ex) { logger.Warning($"Tempest OAuth: failed to clear expires timestamp. {ex.Message}"); throw; }
        catch (NotSupportedException ex) { logger.Warning($"Tempest OAuth: secure storage is not supported. {ex.Message}"); throw; }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    async Task<string?> AuthenticateAndCacheAsync(CancellationToken cancellationToken)
    {
        var logger = _logger;
        var settingRepository = _settingRepository;
        if (logger is null || settingRepository is null)
            throw new InvalidOperationException($"{nameof(TempestOAuthTokenProvider)} is not initialized.");

        if (OperatingSystem.IsWindows())
            throw new NotSupportedException("Tempest OAuth interactive authorization is not supported on Windows. Run on Android/iOS to complete OAuth.");

        var authorizeUrlPath = LookupDictionaries.TempestGroupSettingsDefinition.BuildPath(SettingConstants.Tempest_oauth_authorizeUrl);
        var clientIdPath = LookupDictionaries.TempestGroupSettingsDefinition.BuildPath(SettingConstants.Tempest_oauth_clientId);
        var redirectUriPath = LookupDictionaries.TempestGroupSettingsDefinition.BuildPath(SettingConstants.Tempest_oauth_redirectUri);
        var tokenUrlPath = LookupDictionaries.TempestGroupSettingsDefinition.BuildPath(SettingConstants.Tempest_oauth_tokenUrl);

        var authorizeUrlText = settingRepository.GetValueOrDefault<string>(authorizeUrlPath);
        var clientId = settingRepository.GetValueOrDefault<string>(clientIdPath);
        var redirectUriText = settingRepository.GetValueOrDefault<string>(redirectUriPath);
        var tokenUrlText = settingRepository.GetValueOrDefault<string>(tokenUrlPath);

        var authorizeUrlBase = string.IsNullOrWhiteSpace(authorizeUrlText) ? TempestAuthorizeUrl : authorizeUrlText;
        var tokenUrlBase = string.IsNullOrWhiteSpace(tokenUrlText) ? TempestTokenUrl : tokenUrlText;

        if (!Uri.TryCreate(authorizeUrlBase, UriKind.Absolute, out var authorizeUri))
            throw new InvalidOperationException($"Tempest OAuth authorize URL is invalid (setting: '{authorizeUrlPath}', value: '{authorizeUrlBase}').");

        if (!Uri.TryCreate(tokenUrlBase, UriKind.Absolute, out var tokenUri))
            throw new InvalidOperationException($"Tempest OAuth token URL is invalid (setting: '{tokenUrlPath}', value: '{tokenUrlBase}').");

        logger.Information($"Tempest OAuth settings loaded. authorizeUrlPath='{authorizeUrlPath}', tokenUrlPath='{tokenUrlPath}', clientIdPath='{clientIdPath}', redirectUriPath='{redirectUriPath}'.");
        logger.Information($"Tempest OAuth endpoints resolved. authorizeUrl='{authorizeUri}', tokenUrl='{tokenUri}', redirectUriText='{redirectUriText ?? ""}'.");

        if (string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException("Tempest OAuth client id is not configured.");

        if (string.IsNullOrWhiteSpace(redirectUriText))
            throw new InvalidOperationException("Tempest OAuth redirect URI is not configured.");

        if (!Uri.TryCreate(redirectUriText, UriKind.Absolute, out var redirectUri))
            throw new InvalidOperationException($"Tempest OAuth redirect URI is invalid (value: '{redirectUriText}').");

        var redirectUriValue = redirectUriText.Trim();

        if (!string.Equals(redirectUri.Scheme, RedirectUriScheme, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Tempest OAuth redirect URI scheme must be '{RedirectUriScheme}' (current: '{redirectUri.Scheme}').");

        cancellationToken.ThrowIfCancellationRequested();

        var pkce = Pkce.Create();
        var state = Guid.NewGuid().ToString("N");

        var authorizeUrl = authorizeUri.ToString().TrimEnd('/') + "?" + string.Join("&", new[]
        {
            "response_type=code",
            "client_id=" + Uri.EscapeDataString(clientId),
            "redirect_uri=" + Uri.EscapeDataString(redirectUriValue),
            "code_challenge=" + Uri.EscapeDataString(pkce.CodeChallenge),
            "code_challenge_method=S256",
            "state=" + Uri.EscapeDataString(state)
        });

        WebAuthenticatorResult authResult;
        try
        {
            logger.Information("Tempest OAuth: starting interactive authorization.");
            authResult = await MainThread.InvokeOnMainThreadAsync(() =>
                WebAuthenticator.AuthenticateAsync(new Uri(authorizeUrl, UriKind.Absolute), redirectUri)
            ).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (PlatformNotSupportedException ex)
        {
            logger.Warning($"Tempest OAuth interactive auth is not supported on this platform. {ex.Message}");
            throw new NotSupportedException("Tempest OAuth interactive authorization is not supported on this platform.", ex);
        }
        catch (InvalidOperationException ex)
        {
            logger.Warning($"Tempest OAuth interactive auth failed. {ex.Message}");
            throw;
        }

        if (authResult?.Properties is null
            || !authResult.Properties.TryGetValue("code", out var code)
            || string.IsNullOrWhiteSpace(code))
        {
            logger.Warning("Tempest OAuth callback did not include an authorization code.");
            return null;
        }

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        using var tokenReq = new HttpRequestMessage(HttpMethod.Post, tokenUri)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = clientId,
                ["redirect_uri"] = redirectUriValue,
                ["code"] = code,
                ["code_verifier"] = pkce.CodeVerifier
            })
        };

        tokenReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var tokenRes = await httpClient.SendAsync(tokenReq, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            tokenRes.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            logger.Warning($"Tempest OAuth token exchange failed. {ex.Message}");
            throw;
        }

        await using var stream = await tokenRes.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            return null;

        var root = doc.RootElement;

        if (!root.TryGetProperty("access_token", out var at) || at.ValueKind != JsonValueKind.String)
            return null;

        var accessToken = at.GetString();
        if (string.IsNullOrWhiteSpace(accessToken))
            return null;

        string? refreshToken = null;
        if (root.TryGetProperty("refresh_token", out var rt) && rt.ValueKind == JsonValueKind.String)
            refreshToken = rt.GetString();

        DateTimeOffset? expiresUtc = null;
        if (root.TryGetProperty("expires_in", out var exp) && exp.ValueKind == JsonValueKind.Number && exp.TryGetInt32(out var expSec) && expSec > 0)
            expiresUtc = DateTimeOffset.UtcNow.AddSeconds(expSec);

        try
        {
            await SecureStorage.SetAsync(SecureKey_AccessToken, accessToken).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(refreshToken))
                await SecureStorage.SetAsync(SecureKey_RefreshToken, refreshToken).ConfigureAwait(false);

            if (expiresUtc is not null)
                await SecureStorage.SetAsync(SecureKey_ExpiresUtc, expiresUtc.Value.ToString("O", CultureInfo.InvariantCulture)).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            logger.Warning($"Failed to persist Tempest OAuth token to secure storage. {ex.Message}");
            throw;
        }
        catch (NotSupportedException ex)
        {
            logger.Warning($"Failed to persist Tempest OAuth token to secure storage. {ex.Message}");
            throw;
        }

        return accessToken;
    }

    static async Task<string?> TryGetCachedAccessTokenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var token = await SecureStorage.GetAsync(SecureKey_AccessToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    sealed record Pkce(string CodeVerifier, string CodeChallenge)
    {
        public static Pkce Create()
        {
            // RFC 7636: verifier length 43-128 characters.
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);

            var codeVerifier = Base64UrlEncode(bytes);
            var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
            var codeChallenge = Base64UrlEncode(hash);

            return new Pkce(codeVerifier, codeChallenge);
        }

        static string Base64UrlEncode(byte[] data)
        {
            var s = Convert.ToBase64String(data);
            return s.TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
