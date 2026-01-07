namespace BasicEventRelay;
internal static class BasicEventRelay
{
    static readonly IMessenger _messenger = new WeakReferenceMessenger();
    public static void Send<TMessage>(TMessage message) where TMessage : class
    {
        _messenger.Send(message);
    }
    public static void Register<TMessage>(object recipient, Action<TMessage> handler) where TMessage : class
    {
        _messenger.Register<object, TMessage>(recipient, (r, m) => handler(m));
    }
    public static void Unregister<TMessage>(object recipient) where TMessage : class
    {
        _messenger.Unregister<TMessage>(recipient);
    }
}