namespace MetWorks.Interfaces;
public interface IInstanceIdentifier
{
    string GetOrCreateInstallationId();
    bool SetInstallationId(string installationId);
    bool ResetInstallationId();
}
