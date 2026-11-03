using System.Text;
using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.Infrastructure;
using HeroPath.Modules.Levels.UI.Resources;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

namespace HeroPath.Modules.Levels.UI.Components;

/// <summary>
/// Level files of the editor: the import button with its result message, the export (started from the list through <see cref="ExportAsync"/>)
/// with its failure message, and the privacy hint for exported names. The imported level goes straight into the list and the draft of the editor stays untouched.
/// </summary>
public partial class EditorLevelFiles : LocalizedComponentBase
{
    private const long MaxImportBytes = 256 * 1024;

    private readonly CancellationTokenSource _disposed = new();
    private ImportMessage _import;
    private string? _importName;
    private int _importKey;
    private bool _exportFailed;
    private bool _focusMessage;
    private ElementReference _messageElement;

    /// <summary>Builds the default name («Mein Level N») of an imported level that has none.</summary>
    [Parameter, EditorRequired]
    public required Func<int, string> DefaultName { get; set; }

    [Inject]
    private LevelRepository Repository { get; set; } = default!;

    [Inject]
    private IFileDownload FileDownload { get; set; } = default!;

    [Inject]
    private IStringLocalizer<LevelsResources> Localizer { get; set; } = default!;

    private enum ImportMessage
    {
        None,
        Ok,
        Invalid,
        TooLarge,
        Unreadable,
        NotSavable,
        LimitReached,
        StorageFailed,
    }

    private string? ImportText => _import switch
    {
        ImportMessage.Ok => Localizer["Editor_Import_Ok", _importName ?? string.Empty].Value,
        ImportMessage.Invalid => Localizer["Editor_Import_Invalid"].Value,
        ImportMessage.TooLarge => Localizer["Editor_Import_TooLarge"].Value,
        ImportMessage.Unreadable => Localizer["Editor_Import_Unreadable"].Value,
        ImportMessage.NotSavable => Localizer["Editor_Import_NotSavable"].Value,
        ImportMessage.LimitReached => Localizer["Editor_Import_LimitReached", LevelLimits.MaxCustomLevels].Value,
        ImportMessage.StorageFailed => Localizer["Editor_Import_StorageFailed"].Value,
        _ => null,
    };

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Repository.Changed += HandleChanged;
    }

    /// <summary>
    /// A new result message takes the focus, so the browser scrolls it into view inside the scrolling side panel of the editor (no custom JavaScript).
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusMessage)
        {
            _focusMessage = false;
            await _messageElement.FocusAsync();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Repository.Changed -= HandleChanged;
            _disposed.Cancel();
            _disposed.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>Writes the saved custom level as a file. A failed download shows <c>editor-export-failed</c>; an unknown id does nothing.</summary>
    public async Task ExportAsync(string id)
    {
        _exportFailed = false;
        _import = ImportMessage.None;
        if (Repository.Custom.FirstOrDefault(level => level.Id == id) is not { } level)
        {
            return;
        }

        _exportFailed = !await FileDownload.SaveTextAsync(LevelEditing.ExportFileName(level.Name), LevelJson.Write(level), _disposed.Token);
        _focusMessage = _exportFailed;
        HandleChanged();
    }

    private async Task ImportAsync(InputFileChangeEventArgs args)
    {
        _importKey++;
        _exportFailed = false;
        _importName = null;
        var (json, failure) = await ReadFileAsync(args.File);
        if (json is null)
        {
            _import = failure;
            _focusMessage = true;
            return;
        }

        var result = await Repository.ImportAsync(json, DefaultName, _disposed.Token);
        _importName = result.Level?.Name;
        _import = result.Outcome switch
        {
            ImportOutcome.Imported => ImportMessage.Ok,
            ImportOutcome.NotSavable => ImportMessage.NotSavable,
            ImportOutcome.LimitReached => ImportMessage.LimitReached,
            ImportOutcome.StorageFailed => ImportMessage.StorageFailed,
            _ => ImportMessage.Invalid,
        };
        _focusMessage = true;
    }

    /// <summary>
    /// Reads the file as UTF-8 text up to <see cref="LevelJson.MaxJsonLength"/> characters. A too large file or text and read errors give a message instead of an exception;
    /// only cancellation by this component itself propagates.
    /// </summary>
    private async Task<(string? Json, ImportMessage Failure)> ReadFileAsync(IBrowserFile file)
    {
        if (file.Size > MaxImportBytes)
        {
            return (null, ImportMessage.TooLarge);
        }

        try
        {
            await using var stream = file.OpenReadStream(MaxImportBytes, _disposed.Token);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var buffer = new char[LevelJson.MaxJsonLength + 1];
            var length = 0;
            while (length < buffer.Length)
            {
                var read = await reader.ReadAsync(buffer.AsMemory(length), _disposed.Token);
                if (read == 0)
                {
                    break;
                }

                length += read;
            }

            return length > LevelJson.MaxJsonLength ? (null, ImportMessage.TooLarge) : (new string(buffer, 0, length), ImportMessage.None);
        }
        catch (Exception exception) when (IsReadFailure(exception))
        {
            return (null, ImportMessage.Unreadable);
        }
    }

    private bool IsReadFailure(Exception exception) =>
        exception is IOException or JSException or JSDisconnectedException or InvalidOperationException or UnauthorizedAccessException or NotSupportedException
        || (exception is OperationCanceledException && !_disposed.IsCancellationRequested);
}
