# Android + Windows stream shipping: HTTP/HTTPS troubleshooting and options

This document summarizes the HTTP/HTTPS issues encountered while testing *stream shipping* from the MAUI app and the practical ways to get unblocked during development.

## Context

- Stream shipping sends NDJSON batches from local SQLite tables to a web service.
- In the current setup:
  - HTTP endpoint: `http://192.168.8.9:5157/ingest/v1/stream`
  - HTTPS endpoint: `https://192.168.8.9:7286/ingest/v1/stream`
  - HTTPS uses a self-signed / "crude" certificate.

## Why it behaves differently on Windows vs Android

### HTTP (cleartext)
- **Windows** generally allows outgoing cleartext HTTP by default.
- **Android** commonly blocks cleartext HTTP by default for apps (platform security policy).
  - Symptom can look like HTTP failure/connection error depending on the stack and logging.

### HTTPS (TLS)
- Both Windows and Android require the server certificate to validate.
- A self-signed certificate (or a certificate with an incomplete chain) is *not* trusted by default.
  - Windows browsers can appear to “work” by allowing a click-through, but code using `HttpClient` will still fail validation.

## Recognizing the common failures

### 1) Connection refused
Example:

- `No connection could be made because the target machine actively refused it. (192.168.8.9:7286)`

Meaning:
- This is a **TCP connect** failure, not a certificate issue.
- Common causes:
  - Wrong port or service not listening on that port.
  - Server only bound to loopback (`localhost`) rather than `0.0.0.0`.
  - Firewall/network rules blocking access.
  - Emulator routing differences.

### 2) TLS handshake/certificate validation errors
Examples:

- `The SSL connection could not be established...`
- `PartialChain` / `remote certificate is invalid`

Meaning:
- HTTPS is reachable, but the cert chain is not trusted by the client.

## Development options

### Option A (preferred): Use a trusted certificate and HTTPS
Goal:
- Make `https://...` work everywhere without bypasses.

Practical notes:
- Publicly trusted CAs generally do not issue certificates for private IPs like `192.168.x.x`.
- To get a normal (trusted) cert, most teams use:
  - A DNS name (e.g., `ship.metworks.biz`) + a CA like Let’s Encrypt.
  - A reverse proxy (nginx/caddy/traefik) terminating TLS.
  - Split-horizon DNS for local networks (optional) so devices resolve the same hostname.

Rough cost ranges (USD):
- Let’s Encrypt: **$0**
- Budget DV cert: **$10–$30/year**
- Many commercial DV: **$50–$200/year**

### Option B: Use HTTP for local network development (Android needs explicit allowance)
Goal:
- Keep using `http://192.168.8.9:5157/...` for development.

Android requirement:
- Configure Android Network Security Config to allow cleartext for the target host.

Implementation in this repo:
- `src/MetWorks_Apps_MAUI_WeatherStationMaui/Platforms/Android/Resources/xml/network_security_config.xml`
  - Disallows cleartext by default.
  - Allows cleartext only for host `192.168.8.9`.
- `src/MetWorks_Apps_MAUI_WeatherStationMaui/Platforms/Android/AndroidManifest.xml`
  - `android:usesCleartextTraffic="true"`
  - `android:networkSecurityConfig="@xml/network_security_config"`

Operational note:
- Manifest/resource changes require a **redeploy/reinstall** to take effect; hot reload does not reliably apply these.

### Option C: Dev-only TLS bypass (Windows unblocker)
Goal:
- Keep using HTTPS but accept an invalid cert for development.

Important:
- This is **not production safe**.
- The bypass was implemented as **host-scoped** and **DEBUG-only**.

Implementation in this repo:
- `src/MetWorks_Common/Networking/StreamShippingHttpClientProvider.cs`
  - Builds `HttpClient` with a `HttpClientHandler`.
  - In `DEBUG`, allows invalid TLS if the cert subject matches the configured host.

Setting:
- `streamShippingHttp/allowInvalidTlsForEndpointHost`
  - Added to:
    - `src/MetWorks_Constants/SettingConstants.cs`
    - `src/MetWorks_Constants/LookupDictionaries.cs`
    - `src/MetWorks_Resource_Store/data/settings.yaml`

Notes / pitfalls:
- If the server cert does not present the expected name/IP in the certificate subject, the bypass won’t activate.
- For long-term, prefer Option A.

## Emulator vs physical Android device networking

- If using the Android emulator, `192.168.x.x` may or may not route to your server depending on network setup.
- A physical Android device on the same Wi-Fi is usually the simplest path.

## Recommended path forward

1. For quick progress on Android today:
   - Use **HTTP** on port `5157`.
   - Keep the Android cleartext allowlist in place.
2. For a stable cross-platform setup:
   - Move to a DNS hostname (e.g., `ship.metworks.biz` or similar), then:
     - put HTTPS behind a real certificate (Let’s Encrypt), and
     - use that hostname in `streamShipping.endpointUrl`.

## Appendix: Settings involved

- `/services/streamShipping/endpointUrl`
- `/services/streamShippingHttp/timeoutSeconds`
- `/services/streamShippingHttp/allowInvalidTlsForEndpointHost` (DEBUG-only, dev bypass)
