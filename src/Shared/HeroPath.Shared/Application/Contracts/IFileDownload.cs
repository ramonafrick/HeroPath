namespace HeroPath.Shared.Application.Contracts;

/// <summary>
/// Hands a text file to the user (Web: browser download, MAUI later: share or save dialog). Implementations never throw because
/// saving is unavailable: they report <c>false</c>. Only a cancellation requested through the caller's token is propagated.
/// </summary>
public interface IFileDownload
{
    /// <summary>Offers <paramref name="content"/> as a UTF-8 file named <paramref name="fileName"/>. Returns <c>false</c> when it could not be handed over.</summary>
    ValueTask<bool> SaveTextAsync(string fileName, string content, CancellationToken cancellationToken);
}
