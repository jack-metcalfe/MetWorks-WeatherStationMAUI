namespace EventRelay;
public class EventRelayBasic : IEventRelayBasic
{
    IMessenger _iMessenger = new WeakReferenceMessenger();
    public EventRelayBasic() { }
    public void Send<TMessage>(TMessage message) where TMessage : class
        => _iMessenger.Send(message);
    public void Register<TMessage>(object recipient, Action<TMessage> handler) where TMessage : class
        => _iMessenger.Register<object, TMessage>(recipient, (r, m) => handler(m));
    public void Unregister<TMessage>(object recipient) where TMessage : class
        => _iMessenger.Unregister<TMessage>(recipient);
}