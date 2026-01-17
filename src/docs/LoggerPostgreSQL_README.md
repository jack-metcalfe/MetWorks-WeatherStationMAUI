# LoggerPostgreSQL (Serilog Postgres sink)

Purpose
- Serilog sink that writes log events into a Postgres table (lightweight custom sink).

Key settings
- `LoggerPostgreSQL_connectionString` — Postgres connection string.
- `LoggerPostgreSQL_tableName` — target table name (validated).
- `LoggerPostgreSQL_autoCreateTable` — attempt to create table (runs in background).
- `LoggerPostgreSQL_minimumLevel` — minimum log level.

Behavior summary
- Constructor no longer blocks on DB: schema creation (`EnsureTable`) runs in a background retry loop.
- `Emit` is best-effort and swallows exceptions (per design) but writes diagnostics to Serilog `SelfLog`.
- Sink exposes a health callback/flag so the app can observe when writes are failing vs succeeding.

Operational notes
- Initialization will succeed when DB/network is down; schema creation will be retried until success.
- During outages events are dropped (though you can plug in an in-memory or SQLite queue to persist logs).
- Recommended next step: replace `Emit` with a bounded background queue + writer (drop-on-full) or add SQLite persistence for durability.

Files & locations
- Implementation: `src/logging/LoggerPostgreSQL.cs`