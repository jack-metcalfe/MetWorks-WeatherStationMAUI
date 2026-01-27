namespace MetWorks.Interfaces;
public interface IServiceReady
{
    Task Ready { get; }
    bool IsReady { get; }
}