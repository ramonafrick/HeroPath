using HeroPath.Shared.Application.Contracts;

namespace HeroPath.Shared.UI.Localization;

/// <summary>The default <see cref="IDocumentLanguage"/>: a platform without a document stays silent. A host registers its own after <c>AddSharedUI()</c>.</summary>
internal sealed class NullDocumentLanguage : IDocumentLanguage
{
    public ValueTask SetAsync(string cultureName, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
