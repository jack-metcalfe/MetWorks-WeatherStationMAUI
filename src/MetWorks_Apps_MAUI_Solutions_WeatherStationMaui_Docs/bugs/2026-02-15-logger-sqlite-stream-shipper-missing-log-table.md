# LoggerSQLiteStreamShipper fails: SQLite "no such table: log"

Date: 2026-02-15

## Symptom

Background shipper task fails during startup/first run:

- `LoggerSQLiteStreamShipper` unhandled background task exception: `SQLite Error 1: 'no such table: log'`

Caller label identified the failure originating from `StartBackground(...)` in `LoggerSQLiteStreamShipper.InitializeAsync`.

## Root cause

There were multiple competing/legacy defaults for the logger SQLite table name:

- `LoggingDatabaseReadiness` + `LoggingSqlScripts` create `logger_sqlite_log`.
- `LoggerSQLiteStreamShipper` defaulted to `log` when `/services/loggerSQLite/tableName` is empty.

If the app is using the newer persistence DDL (`logger_sqlite_log`) but the shipper (or settings) points to `log`, the shipper will query a table that does not exist.

## Fix

Aligned defaults to the `LoggingSqlScripts` canonical table name:

- Default shipper table is now `logger_sqlite_log`.
- Legacy bootstrapper now creates `logger_sqlite_log`.
- Logger shipper purge logic now uses the configured table name instead of hard-coding `log`.

Files:
- `src/MetWorks_Ingest_SQLite/Shipping/LoggerSQLiteStreamShipper.cs`

## Follow-ups

- Consider removing/replacing legacy log-table bootstrapping (`MetWorks_Persistence_SQLite`) once migration is complete.
- Consider centralizing the canonical logger table name into a single constant to prevent divergence.
