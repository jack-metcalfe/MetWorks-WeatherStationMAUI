namespace BasicEventRelay;
public interface ISingletonEventRelay
{
    static void Send<TMessage>(TMessage message) where TMessage : class
        => BasicEventRelay.Send(message);
    static void Register<TMessage>(object recipient, Action<TMessage> handler) where TMessage : class
        => BasicEventRelay.Register<TMessage>(recipient, handler);
    static void Unregister<TMessage>(object recipient) where TMessage : class
        => BasicEventRelay.Unregister<TMessage>(recipient);
}
