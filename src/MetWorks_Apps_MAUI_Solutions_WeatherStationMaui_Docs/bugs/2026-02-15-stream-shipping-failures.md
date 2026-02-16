# Stream shipping failures (interval out-of-range, timestamp parsing, HTTP timeout)

Date: 2026-02-15

## Symptoms observed

While stepping through initialization/ship loops, background task exceptions were logged:

- `LightningStreamShipper` (`InitializeAsync`): "Specified argument was out of the range of valid values."
- `StationMetadataStreamShipper` (`InitializeAsync`): "The input string '2026-02-15T23:22:28.2303161Z' was not in a correct format."
- `LoggerSQLiteStreamShipper` (`InitializeAsync`): "The request was canceled due to the configured HttpClient.Timeout of 30 seconds elapsing."

## Root causes

### 1) Ship interval seconds could produce an invalid `TimeSpan`

All three shippers create the loop delay interval using:

- `TimeSpan.FromSeconds(_shipIntervalSeconds)`

If `_shipIntervalSeconds` is extremely large (e.g., due to a bad config value or parse), `TimeSpan.FromSeconds(...)` throws `ArgumentOutOfRangeException`, which manifests as an unhandled background task exception originating at the `StartBackground(...)` call site.

### 2) `station_metadata.application_received_utc_timestampz` is `TEXT` in SQLite

The SQLite schema stores `application_received_utc_timestampz` as `TEXT` (ISO-8601 timestampz).

`StreamShippingRepository.ReadStandardReadingsBatchAsync(...)` was reading that column as `Int64` (epoch seconds). When the underlying value is text (for station metadata), the row getter can throw/propagate a format error.

This matches the observed failure for an ISO-8601 `...Z` timestamp.

### 3) Stream-shipping HTTP client timeout defaulted to 30 seconds

`StreamShippingHttpClientProvider` created an `HttpClient` with a default `Timeout` of 30 seconds.

NDJSON uploads can take longer than that (especially when paused in the debugger), causing request cancellation due to the `Timeout` elapsing.

## Fixes applied

### Interval clamping (prevents `ArgumentOutOfRangeException`)

Clamped `_shipIntervalSeconds` to a safe range before it is used:

- min: 1 second
- max: 86400 seconds (24 hours)

Files:
- `src/MetWorks_Ingest_SQLite/Shipping/LightningStreamShipper.cs`
- `src/MetWorks_Ingest_SQLite/Shipping/StationMetadataStreamShipper.cs`
- `src/MetWorks_Ingest_SQLite/Shipping/LoggerSQLiteStreamShipper.cs`

### Robust timestamp handling for `application_received_utc_timestampz`

Updated `StreamShippingRepository.ReadStandardReadingsBatchAsync(...)` to:

1. Try reading `application_received_utc_timestampz` as `Int64` (epoch)
2. If that fails, try reading it as `string` and parse via `DateTimeOffset.TryParse(...)`
3. Convert to epoch seconds

File:
- `src/MetWorks_Persistence/StreamShipping/StreamShippingRepository.cs`

### More forgiving HTTP timeout

Updated `StreamShippingHttpClientProvider` to:

- default timeout: 120 seconds
- clamp configured values to 5..600 seconds

File:
- `src/MetWorks_Common/Networking/StreamShippingHttpClientProvider.cs`

## Notes / lessons learned

- **Validate config-derived timing values before producing `TimeSpan`s.** A bad value can fail before the background loop even starts.
- **Don’t assume a column type across tables if the same logical field is stored differently.** Here, some tables likely store epoch seconds while `station_metadata` stores ISO-8601 text.
- **`HttpClient.Timeout` interacts poorly with debugging.** If you pause execution, the request continues timing out.

## Follow-ups

- Consider making shipper config validation log the clamped values (to surface misconfiguration).
- Consider normalizing `application_received_utc_timestampz` storage across SQLite tables (all epoch seconds, or all ISO-8601 text) to reduce conditional parsing in shipper paths.
