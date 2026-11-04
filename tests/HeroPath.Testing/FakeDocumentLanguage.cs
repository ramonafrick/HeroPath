using HeroPath.Shared.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Testing;

public sealed class FakeDocumentLanguage : IDocumentLanguage
{
    public List<string> Values { get; } = [];

    /// <summary>When set, <see cref="SetAsync"/> throws it synchronously, to prove a failing platform does not break callers.</summary>
    public Exception? Throws { get; set; }

    public ValueTask SetAsync(string cultureName, CancellationToken cancellationToken)
    {
        Values.Add(cultureName);
        return Throws is { } exception ? throw exception : ValueTask.CompletedTask;
    }
}

public static class FakeDocumentLanguageExtensions
{
    /// <summary>Registers a <see cref="FakeDocumentLanguage"/>, which <c>LanguageService</c> needs; returns it for assertions.</summary>
    public static FakeDocumentLanguage AddFakeDocumentLanguage(this IServiceCollection services)
    {
        var fake = new FakeDocumentLanguage();
        services.AddSingleton<IDocumentLanguage>(fake);
        return fake;
    }
}
