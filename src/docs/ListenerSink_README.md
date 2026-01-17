# ListenerSink (Postgres writer)

Purpose
- Receives typed packet records and writes them into PostgreSQL tables.
- Provides automatic degraded-mode operation, reconnection logic and optional in-memory buffering.

Key settings
- `XMLToPostgreSQL_connectionString` — Postgres connection string.
- `XMLToPostgreSQL_enableBuffering` — enable bounded in-memory buffering while DB is unavailable.

Behavior summary
- Builds a `PostgresConnectionFactory`, validates the connection and initializes schema asynchronously.
- If DB is unreachable at startup, enters degraded mode and runs periodic reconnection attempts.
- On write failures that indicate connection loss the factory is cleared and reconnection is attempted.
- Optional bounded in-memory queue holds messages while DB is down and flushes when connectivity returns.

Operational notes
- The in-memory buffer is for short outages only. Use a persistent queue (SQLite) to survive process restarts or long outages.
- Monitor `_isDatabaseAvailable`, `_failureCount` and health logs to detect persistent failures.
- Test by stopping/starting the DB and verifying buffer flush and reconnection behavior.

Files & locations
- Implementation: `src/raw_packet_record_type_in_postgres_out/ListenerSink.cs`