namespace MetWorks.Interfaces;
public interface IInstanceIdentifier
{
    string InstallationId { get; }
    string GetOrCreateInstallationId();
    string CreateNewInstallationId();
    bool SetInstallationId(string installationId);
    bool ResetInstallationId();
}
