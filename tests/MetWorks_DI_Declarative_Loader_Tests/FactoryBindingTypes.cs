namespace Test.FactoryBinding;

public sealed class Widget
{
}

public sealed class WidgetFactory
{
    public Widget CreateWidget() => new();
}
