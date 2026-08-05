namespace MetWorks.Persistence.Rollups;

internal static class PrecipitationRollupSql
{
    internal const string SourceTableName = "precipitation";

    // Intentionally no BuildUpsertRollupSql: precipitation rollups are watermark-only at this time.
}
