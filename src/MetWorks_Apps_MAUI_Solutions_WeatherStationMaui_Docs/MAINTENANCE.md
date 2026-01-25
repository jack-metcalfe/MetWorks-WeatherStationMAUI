# Maintenance

Checklist for common maintenance tasks

- Verify COMB GUID behavior when changing IdGenerator: src/utility/IdGenerator.cs
- Review WeatherDataTransformer behavior around last-packet cache and retransforms: src/metworks_services/WeatherDataTransformer.cs
- Remove obsolete docs/artifacts: src/raw_packet_record_type_in_postgres_out/ListenerSink_README.md (deleted if present)

Release notes
- When releasing breaking changes to packet formats or transformer behavior, include a migration guide that lists old->new fields and recommended replay steps using the last-packet cache for rehydration.

Backup and persistence
- Ensure raw packet store retention policy and COMB GUID ordering are respected during purges; document retention policy in this file when set by ops.
