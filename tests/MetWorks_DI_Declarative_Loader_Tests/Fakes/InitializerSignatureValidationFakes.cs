namespace SyntaxLoader.Tests.Fakes;

using System.Threading.Tasks;

public interface IBar
{
}

public interface IAltBar
{
}

public sealed class Bar : IBar
{
}

public sealed class InitService
{
    public Task InitializeAsync(IBar iBar, string connectionString)
        => Task.CompletedTask;
}

public interface IChainRoot
{
}

public sealed class ChainLeaf
{
    public string Value { get; } = "leaf";
}

public sealed class ChainNode
{
    public ChainLeaf Leaf { get; } = new();
}

public sealed class ChainRoot : IChainRoot
{
    public ChainNode Node { get; } = new();
}

public sealed class DottedPropertyInitService
{
    public Task InitializeAsync(string value)
        => Task.CompletedTask;
}
