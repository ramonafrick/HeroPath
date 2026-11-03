using HeroPath.Shared.Application.Contracts;

namespace HeroPath.Testing;

public sealed class FakeFileDownload : IFileDownload
{
    public List<(string FileName, string Content)> Saved { get; } = [];

    /// <summary>The result reported to callers; when false nothing is recorded in <see cref="Saved"/>.</summary>
    public bool Result { get; set; } = true;

    public ValueTask<bool> SaveTextAsync(string fileName, string content, CancellationToken cancellationToken)
    {
        if (Result)
        {
            Saved.Add((fileName, content));
        }

        return ValueTask.FromResult(Result);
    }
}
