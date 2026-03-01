# Tempest WebSocket ingest (UDP-like flow) — Implementation Plan

Last Updated: 2026-02-26
Status: Draft (plan only)

## Goal
Add a third ingest source that behaves similarly to the existing UDP pipeline, but uses Tempest’s hosted WebSocket stream.

Use-case:
- When the device is not on the LAN (or UDP is otherwise unavailable), the app can still receive near-real-time readings.

This should integrate cleanly with the existing canonical UI stream selection in `WeatherReadingMux`.

References:
- WebSockets: `https://weatherflow.github.io/Tempest/api/ws.html`
- OAuth: `https://weatherflow.github.io/Tempest/api/oauth.html`

---

## Non-goals
- Do not replace UDP ingest.
- Do not build historical storage off the WebSocket stream (continue using SQLite/Postgres sinks and/or REST snapshots for historical).
- Do not change UI behavior beyond allowing “live” updates when UDP is unavailable.
- Do not finalize OAuth UX (exact UI screens) in this phase; focus on plumbing and minimal configuration.

---

## Decisions (tracked)

### Decision 1: WS mapping strategy (WS → domain readings vs WS → `IRawPacketRecordTyped`)

Status: Proposed

Decision:
- Use **Approach A**: map WebSocket messages (`rapid_wind`, `obs_st`, `evt_*`) directly to the app’s domain readings (or mux-input messages), rather than fabricating `IRawPacketRecordTyped`.

Supporting information:
- The existing UDP transformation path is built around UDP JSON payloads and the UDP-specific parser (`TempestPacketParser`). Tempest WS payloads have different schemas (notably `obs_st`/`rapid_wind` array layouts), so pretending they are UDP packets is likely to be brittle.
- WS also introduces an OAuth token and cloud connectivity concerns; keeping the WS ingestion and mapping boundary explicit should reduce coupling and make source-selection (`WeatherReadingMux`) easier to reason about.

### Decision 2: WS publication strategy (raw WS JSON message vs WS-derived readings)

Status: Accepted

Decision:
- Use **Option 2A**: publish a **WS snapshot message** (raw WS JSON + minimal metadata) via `IEventRelayBasic`.
- `WeatherReadingMux` is responsible for mapping WS snapshot messages into canonical UI readings.

Supporting information:
- Publishing the raw JSON makes it straightforward to later add persistence (SQLite/Postgres/file) without having to reconstitute the source payload from mapped models.
- Keeping mapping centralized (e.g., in `WeatherReadingMux` via a WS mapper) maintains the existing “one canonical UI stream” design and reduces the chance of UI thrash or divergent mapping logic.
- This also avoids introducing source-specific subclasses of `ObservationReading` / `WindReading` solely to preserve provenance/source identity.

### Decision 3: OAuth UX + token storage strategy

Status: Accepted

Decision:
- Use OAuth **Authorization Code + PKCE**.
- Use MAUI `WebAuthenticator` for the user authorization step.
- Use a custom-scheme redirect URI (no fixed public domain required).
- Store `client_id` and `redirect_uri` in settings.
- Store OAuth tokens in MAUI `SecureStorage` (platform-backed secure storage).
- Defer refresh/renewal implementation details until we inspect the Tempest token endpoint response (e.g., `expires_in`, `refresh_token`).

Supporting information:
- `WebAuthenticator` delegates to platform-provided browser/auth surfaces (e.g., custom tabs / ASWebAuthenticationSession) while keeping the app logic consistent across platforms.
- MAUI `SecureStorage` provides a unified API with platform-specific implementations (Android Keystore, iOS Keychain, Windows credential/DPAPI-backed storage).

### Decision 4: Source selection mode + metrics

Status: Accepted

Decision:
- Implement **Auto-select** source selection only (no manual `udp_only` / `ws_only` / `rest_only` modes in phase 1).
- Add lightweight metrics to quantify how often each mode is selected and how much data it consumes.

Supporting information:
- Auto-select keeps the UX simple and aligns with the intent: use UDP when available, otherwise WS, otherwise REST.
- Having explicit metrics makes it easier to tune refresh intervals and understand cost/latency tradeoffs.
- Metrics should be keyed by effective refresh parameters (e.g., REST polling interval; WS subscribed message types) so “mode behavior” is measurable.
- Primary user-cost driver is internet usage: UDP is local-only, but REST/WS consume WAN data.

### Decision 5: Persistence strategy (phase 1)

Status: Accepted

Decision:
- Phase 1 is **UI-only** for WebSocket ingest (no DB/file persistence of WS messages beyond any transient in-memory state needed for mapping).

Supporting information:
- Avoids duplicate fact storage while we validate whether WS `obs_st` is a strict subset of the existing UDP-derived observation model.
- Mux already prevents UI thrash by selecting a single canonical source; persistence is the remaining duplication risk.

Future direction (not in phase 1):
- If we persist WS messages, prefer storing raw WS payloads in **separate tables** to avoid loss of fidelity.
- Consider an explicit merge/de-dupe process into canonical fact tables (or drop WS facts if they are proven to be a strict subset of UDP facts).

## Existing baseline (today)
Current live + near-live sources:
- UDP → `TempestPacketTransformer` → `IRawPacketRecordTyped` → `SensorReadingTransformer` → `IObservationReading`/`IWindReading` → `WeatherReadingMux` → canonical `ObservationReading`/`WindReading`
- REST polling → `TempestRestObservationsProvider` → `TempestRestObservationsSnapshot` → `WeatherReadingMux` → canonical `ObservationReading`/`WindReading`

Source selection:
- `WeatherReadingMux` selects UDP vs REST based on freshness and ingest-mode settings, then publishes a single canonical stream to the UI.

---

## Proposed WebSocket ingest architecture
### High-level concept
Add:
- A WebSocket listener service that connects to Tempest (`wss://ws.weatherflow.com/swd/data?token=...`).
- A lightweight mapper that converts WS messages (`rapid_wind`, `obs_st`, `evt_*`) into the app’s domain readings.
- Extend `WeatherReadingMux` to consider WebSocket as an additional candidate source when UDP is stale/unavailable.

### Design choice: where to map
There are two viable approaches:

A) Map WS → domain readings (recommended)
- WebSocket service parses WS message JSON.
- Publishes concrete `ObservationReading`/`WindReading` (or new WS-specific DTO message types that mux maps).
- Pros: avoids pretending WS payloads are UDP packets; avoids binding WS JSON into `IRawPacketRecordTyped` shape.
- Cons: adds one more mapping path similar to the REST mapper.

B) Wrap WS as `IRawPacketRecordTyped` (not recommended initially)
- WebSocket service fabricates an `IRawPacketRecordTyped` with `PacketEnum` and “raw JSON”.
- Reuses `SensorReadingTransformer`.
- Risk: existing `TempestPacketParser` is likely UDP-schema specific; WS schemas differ (`obs_st`, arrays).

This plan assumes **Approach A**.

---

## Message flow (planned)
### New: `TempestWebSocketConnectionStatus` (optional but recommended)
Purpose:
- Surface WS connectivity/auth state and last message times.

SENT BY:
- `TempestWebSocketObservationsProvider`

RECEIVED BY:
- `WeatherViewModel` (optional UI display)
- `WeatherReadingMux` (for freshness computation, if desired)

### New: WebSocket → mux input message(s)
Publish a WS snapshot message (raw WS JSON + minimal metadata), then map in the mux.

MESSAGE:
- `TempestWebSocketObservationsSnapshot` (WS snapshot)
  - `DeviceId`
  - `ReceivedUtc`
  - `MessageType` (e.g., `"rapid_wind"`, `"obs_st"`, `"evt_strike"`, `"evt_precip"`)
  - `RawJson`

SENT BY:
- `TempestWebSocketObservationsProvider`

RECEIVED BY:
- `WeatherReadingMux` (maps to canonical `ObservationReading` / `WindReading`)
- (Future) a persistence component that stores WS raw messages (optional)

Then `WeatherReadingMux` publishes canonical concrete readings (`ObservationReading`, `WindReading`) as it does today.

---

## OAuth + token strategy (design notes)
OAuth doc describes authorization-code and PKCE, but does not document refresh semantics.

Plan:
1. Implement Authorization Code + PKCE.
2. Use MAUI `WebAuthenticator` for interactive authorization.
3. Persist tokens securely using MAUI `SecureStorage`.
4. Detect auth failure.
   - WS will fail to connect or will disconnect; REST may return 401.
5. Renewal strategy:
   - Inspect the token endpoint response fields (e.g., `expires_in`, `refresh_token`).
   - If a refresh token is provided, implement refresh.
   - Otherwise, require re-auth (user interaction) when expired.

---

## Reliability requirements
- Only open **one** WebSocket connection (per upstream guidance).
- Handle server disconnects (including “10 minutes idle” rule) with keepalive and/or reconnect.
- Reconnect with backoff.
- Never allow WS failures to crash the app.
- Publish ingest status so mux/UI can make decisions.

---

## Settings / configuration
Add settings (paths TBD; keep lexicographically sorted in `settings.yaml`):
- `/services/weatherIngest/sourceMode` (prefer a single Auto mode)
- `/services/tempest/websocket/enabled` (bool)
- `/services/tempest/websocket/deviceId` (long; optional if discoverable from station snapshot)
- `/services/tempest/oauth/clientId` (string)
- `/services/tempest/oauth/redirectUri` (string)

Token storage:
- Do not store access tokens in `settings.yaml`.

---

## Changes by component (planned)
### 1) New provider/service
Create `TempestWebSocketObservationsProvider` (naming TBD) similar to:
- `TempestRestObservationsProvider` (looping background refresher)
- `TempestPacketTransformer` (always-on listener)

Responsibilities:
- Connect to WS endpoint.
- Send `listen_start` and `listen_rapid_start`.
- Receive loop.
- Parse JSON.
- Publish WS-derived readings/messages.

Device id selection:
- Prefer explicit `/services/tempest/websocket/deviceId`.
- If not configured, derive from the REST station snapshot (`tempest.station.snapshot.json`) by selecting the `devices[]` entry where `device_type == "ST"`.

### 2) New WS message parser/mapper
Create `TempestWebSocketReadingsMapper` (or similar):
- Input: raw WS JSON message strings.
- Output: best-effort readings, likely:
  - wind from `rapid_wind`
  - observation from `obs_st` (preferred) and/or `obs_air`+`obs_sky`

### 3) Extend `WeatherReadingMux`
- Add a third source: `WeatherIngestSource.WebSocket`.
- Track freshness: last WS message time.
- Decide priority (suggested):
  - Prefer UDP when fresh (LAN best latency).
  - Otherwise prefer WS when fresh.
  - Otherwise fall back to REST when fresh.

Metrics to capture (phase 1):
- Total bytes transferred per source (UDP / WS / REST)
  - Prefer tracking **incoming + outgoing** bytes; if that is too invasive initially, track incoming bytes first.
  - Focus reporting on **internet sources** (REST / WS) since UDP is LAN-local.
- Total active time per selected source
- Breakdowns by refresh configuration where applicable (e.g., REST poll interval)

### 4) Documentation
Update:
- `MessageChain.md` to include WS flow.
- Add a mini-spec doc in the new folder if needed.

---

## Implementation steps
1. Add new settings definitions
- Update `src/MetWorks_Resource_Store/data/settings.yaml`.
- Update `SettingConstants` and `LookupDictionaries` as needed.

2. Implement `device_id` discovery from station snapshot
- Extend `StationMetadataProvider` / `StationMetadata` to extract and publish/store the Tempest `ST` `device_id` from the station snapshot.
- Use that derived `device_id` as the default for WebSocket subscription when `/services/tempest/websocket/deviceId` is not configured.

3. Add OAuth token acquisition plumbing (minimal)
- Define interfaces and a service boundary that can provide an access token.
- Use PKCE.
- Persist token via secure storage.

4. Implement `TempestWebSocketObservationsProvider`
- Use `ClientWebSocket`.
- Implement connect + subscribe messages.
- Implement receive loop + basic JSON parsing.
- Publish WS-derived readings/events.

5. Implement WS → readings mapping
- `rapid_wind` → wind reading.
- `obs_st` → observation reading.
- `evt_strike`/`evt_precip` → lightning/precipitation messages (optional in phase 1).

6. Integrate mux
- Add new registrations and caching.
- Extend selection logic.
- Extend `WeatherIngestStatus` to report WS freshness/last error.

7. Wire into DDI startup
- Add instances + initialization order in DDI YAML.
- Ensure the provider starts in the background like other services.

8. Manual validation
- Validate when:
  - UDP is fresh → mux selects UDP.
  - UDP is stale/unavailable and WS is authenticated → mux selects WS.
  - UDP + WS unavailable → mux selects REST.

9. Update docs
- `MessageChain.md`
- This plan’s status/notes

---

## Detailed work breakdown (files/classes)

This section decomposes the implementation steps into specific files/classes to create or modify.

1) Add new settings definitions

- Modify `src/MetWorks_Resource_Store/data/settings.yaml`
  - Add `/services/tempest/oauth/{clientId,redirectUri}`
  - Add `/services/tempest/websocket/{enabled,deviceId}`
  - Keep `definitions` sorted by `path`.
- Modify `src/MetWorks_Constants/SettingConstants.cs`
  - Add new constants for the Tempest OAuth + WS settings.
  - Consider whether these should remain under the existing `Tempest_groupName` or introduce a new `tempestWebSocket`/`tempestOAuth` subgroup (prefer minimal disruption).
- Modify `src/MetWorks_Constants/LookupDictionaries.cs`
  - Add/update `GroupSettingDefinition` entries so lookups can use `LookupDictionaries.*.BuildPath(...)` consistently.

2) Implement `device_id` discovery from station snapshot

- Modify `src/MetWorks_Interfaces/IStationMetadataProvider.cs`
  - Extend `StationMetadata` to include the discovered Tempest ST `device_id` (e.g., `long? TempestDeviceId`).
- Modify `src/MetWorks_Common/StationMetadataProvider.cs`
  - Extend `TryExtractStationMetadata(...)` to extract `device_id` for the `devices[]` entry where `device_type == "ST"`.
  - Persist remains `tempest.station.snapshot.json` under `PlatformPaths.AppDataDirectory`.
- Modify call sites that consume `StationMetadata` (compile-guided)
  - Any constructors/records creating `StationMetadata` must be updated for the new field.

3) Add OAuth token acquisition plumbing (minimal)

- Add `src/MetWorks_Interfaces/ITempestOAuthTokenProvider.cs` (new)
  - Interface for getting a valid `access_token` (and optionally metadata like `ExpiresUtc`).
- Add `src/MetWorks_Apps_MAUI_WeatherStationMaui/Services/TempestOAuthTokenProvider.cs` (new; location TBD)
  - Uses MAUI `WebAuthenticator` for interactive authorization.
  - Stores token material in MAUI `SecureStorage`.
- Modify MAUI platform callback registration
  - Android: `src/MetWorks_Apps_MAUI_WeatherStationMaui/Platforms/Android/*` (intent filter / callback activity as required by `WebAuthenticator`).
  - iOS: `src/MetWorks_Apps_MAUI_WeatherStationMaui/Platforms/iOS/*` (URL types as required).
  - Ensure the redirect URI matches the Tempest application registration.

4) Implement `TempestWebSocketObservationsProvider`

- Add `src/MetWorks_Interfaces/ITempestWebSocketObservationsProvider.cs` (new)
  - Defines `InitializeAsync(...)` dependencies and the contract for starting/stopping WS ingest.
- Add `src/MetWorks_Interfaces/TempestWebSocketObservationsSnapshot.cs` (or include in the interface file)
  - `public sealed record TempestWebSocketObservationsSnapshot(long DeviceId, DateTimeOffset ReceivedUtc, string MessageType, string RawJson);`
- Add `src/MetWorks_Common/TempestWebSocketObservationsProvider.cs` (new)
  - `ServiceBase` derived.
  - Uses `ClientWebSocket`.
  - Connect → send `listen_start` + `listen_rapid_start`.
  - Receive loop → publish `TempestWebSocketObservationsSnapshot`.
  - Reconnect/backoff.
  - Device id selection: explicit setting, else `StationMetadata.TempestDeviceId`.
  - (Metrics) count bytes in/out per message.

5) Implement WS → readings mapping

- Add `src/MetWorks_Ingest_Transformer/TempestWebSocketReadingsMapper.cs` (new)
  - Best-effort mapping from `TempestWebSocketObservationsSnapshot` to `ObservationReading`/`WindReading`.
  - Start with `obs_st` + `rapid_wind`.

6) Integrate mux

- Modify `src/MetWorks_Ingest_Transformer/WeatherReadingMux.cs`
  - Register for `TempestWebSocketObservationsSnapshot`.
  - Track freshness/time/error for WS like existing UDP/REST.
  - Auto-select priority: UDP → WS → REST.
  - Record data-usage metrics (bytes, duration) by active source.
- Modify `WeatherIngestStatus` model if necessary to surface WS status (file TBD; locate via compile-guided changes).

7) Wire into DDI startup

- Modify the DDI input YAML
  - Likely `src/MetWorks_Apps_MAUI_Solutions_WeatherStationMaui_Docs/WeatherStationMaui.yaml` (confirm in repo).
  - Add instances for WS provider + OAuth token provider.
  - Ensure instance ordering dependencies are satisfied.
- Regenerate DDI output
  - Do not edit `src/MetWorks_DdiRegistry/*.g.cs` by hand.

8) Manual validation

- Validate selection: UDP fresh → UDP selected; UDP stale + WS connected → WS selected; WS unavailable → REST.
- Validate bandwidth: confirm REST polling interval has visible impact on bytes transferred; WS message volume aligns with `rapid_wind` (~3s) and `obs_st` reporting interval.

9) Update docs

- Modify `src/MetWorks_Apps_MAUI_Solutions_WeatherStationMaui_Docs/MessageChain.md`.
- Update this plan with observed token response fields (`expires_in`, `refresh_token`) once known.

---

## Open questions
- How will `device_id` be discovered/configured? (manual setting vs REST lookup)
  - Rule of thumb from Tempest station snapshots: prefer the `devices[]` entry where `device_type == "ST"` when you want `obs_st` messages.
  - Example rationale: in a station snapshot, the `ST` device typically advertises the outdoor capabilities (wind/rain/light/lightning/pressure/temp-humidity) and corresponds to the combined `obs_st` WebSocket stream.
- Does token response include refresh semantics? (verify actual response fields)
- Do we want WS to publish only observation+wind in phase 1, or also precip/lightning events?
- Should WS-derived readings persist to the same sinks as UDP-derived readings? (likely yes, but avoid duplicates if both UDP and WS are live)
