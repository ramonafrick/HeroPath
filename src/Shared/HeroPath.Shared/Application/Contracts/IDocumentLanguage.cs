namespace HeroPath.Shared.Application.Contracts;

/// <summary>
/// Tells the platform which language the document is in (Web: the <c>lang</c> attribute of the html element, so screen readers
/// and the browser use the right pronunciation and hyphenation). Implementations never throw because the platform is unavailable.
/// Only a cancellation requested through the caller's token is propagated.
/// </summary>
public interface IDocumentLanguage
{
    /// <summary>Sets the document language to <paramref name="cultureName"/> (for example <c>de-CH</c> or <c>en</c>).</summary>
    ValueTask SetAsync(string cultureName, CancellationToken cancellationToken);
}
