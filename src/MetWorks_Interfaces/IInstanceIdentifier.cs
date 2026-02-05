namespace MetWorks.Interfaces;
public interface IInstanceIdentifier
{
    string InstallationId { get; }
    string GetOrCreateInstallationId();
    bool SetInstallationId(string installationId);
    bool ResetInstallationId();
}
