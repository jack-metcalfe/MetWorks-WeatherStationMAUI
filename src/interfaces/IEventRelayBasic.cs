namespace Interfaces;

public interface IEventRelayBasic
{
    void Send<TMessage>(TMessage message) where TMessage : class;
    void Register<TMessage>(object recipient, Action<TMessage> handler) where TMessage : class;
    void Unregister<TMessage>(object recipient) where TMessage : class;
}