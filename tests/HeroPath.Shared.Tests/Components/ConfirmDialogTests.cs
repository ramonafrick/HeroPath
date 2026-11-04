using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using HeroPath.Shared.UI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Shared.Tests.Components;

[Collection("Culture")]
public class ConfirmDialogTests : CultureBunitContext
{
    private const string FocusIdentifier = "Blazor._internal.domWrapper.focus";

    public ConfirmDialogTests()
    {
        Services.AddSharedUI();
        Services.AddFakeDocumentLanguage();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
    }

    private IRenderedComponent<ConfirmDialog> RenderDialog(
        bool visible = true,
        Action? onConfirm = null,
        Action? onCancel = null,
        string? confirmLabel = null,
        string? cancelLabel = null) =>
        Render<ConfirmDialog>(p => p
            .Add(c => c.Visible, visible)
            .Add(c => c.Title, "Level löschen?")
            .Add(c => c.Message, "Wirklich?")
            .Add(c => c.ConfirmLabel, confirmLabel)
            .Add(c => c.CancelLabel, cancelLabel)
            .Add(c => c.OnConfirm, EventCallback.Factory.Create(this, onConfirm ?? (() => { })))
            .Add(c => c.OnCancel, EventCallback.Factory.Create(this, onCancel ?? (() => { }))));

    [Fact]
    public void Render_NotVisible_RendersNothing()
    {
        var cut = RenderDialog(visible: false);

        Assert.Empty(cut.FindAll("[data-testid=confirm-dialog]"));
        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void Render_Visible_HasDialogRoleModalAndLinkedTitleAndMessage()
    {
        var cut = RenderDialog();

        var dialog = cut.Find("[data-testid=confirm-dialog]");
        Assert.Equal("dialog", dialog.GetAttribute("role"));
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));
        Assert.Equal(cut.Find("[data-testid=confirm-title]").Id, dialog.GetAttribute("aria-labelledby"));
        Assert.Equal(cut.Find("[data-testid=confirm-message]").Id, dialog.GetAttribute("aria-describedby"));
        Assert.Equal("Level löschen?", cut.Find("[data-testid=confirm-title]").TextContent);
        Assert.Equal("Wirklich?", cut.Find("[data-testid=confirm-message]").TextContent);
    }

    [Fact]
    public void Render_TwoDialogs_HaveDifferentLabelIds()
    {
        var first = RenderDialog();
        var second = RenderDialog();

        Assert.NotEqual(
            first.Find("[data-testid=confirm-dialog]").GetAttribute("aria-labelledby"),
            second.Find("[data-testid=confirm-dialog]").GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void Render_DefaultLabels_AreGenericGerman()
    {
        var cut = RenderDialog();

        Assert.Equal("Ja", cut.Find("[data-testid=confirm-yes]").TextContent);
        Assert.Equal("Abbrechen", cut.Find("[data-testid=confirm-no]").TextContent);
    }

    [Fact]
    public void Render_DefaultLabels_AreGenericEnglishInEnglish()
    {
        using var scope = new CultureScope("en");

        var cut = RenderDialog();

        Assert.Equal("Yes", cut.Find("[data-testid=confirm-yes]").TextContent);
        Assert.Equal("Cancel", cut.Find("[data-testid=confirm-no]").TextContent);
    }

    [Fact]
    public void Render_CustomLabels_ReplaceTheDefaults()
    {
        var cut = RenderDialog(confirmLabel: "Ja, löschen", cancelLabel: "Nein");

        Assert.Equal("Ja, löschen", cut.Find("[data-testid=confirm-yes]").TextContent);
        Assert.Equal("Nein", cut.Find("[data-testid=confirm-no]").TextContent);
    }

    [Fact]
    public void Render_TextWithMarkup_IsShownAsText()
    {
        var cut = Render<ConfirmDialog>(p => p
            .Add(c => c.Visible, true)
            .Add(c => c.Message, "<b>Level</b>"));

        Assert.Equal("<b>Level</b>", cut.Find("[data-testid=confirm-message]").TextContent);
        Assert.Empty(cut.FindAll("[data-testid=confirm-message] b"));
    }

    [Fact]
    public void Open_MovesFocusToTheFirstButton()
    {
        var cut = RenderDialog(visible: false);

        cut.Render(p => p.Add(c => c.Visible, true));

        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(cut.Find("[data-testid=confirm-yes]"));
    }

    [Fact]
    public void Render_StaysVisible_FocusesOnlyOnOpen()
    {
        var cut = RenderDialog();
        cut.Render(p => p.Add(c => c.Title, "Anderer Titel"));

        Assert.Single(JSInterop.Invocations, i => i.Identifier == FocusIdentifier);
    }

    [Fact]
    public void Click_Yes_RaisesOnConfirmOnly()
    {
        var confirmed = 0;
        var cancelled = 0;
        var cut = RenderDialog(onConfirm: () => confirmed++, onCancel: () => cancelled++);

        cut.Find("[data-testid=confirm-yes]").Click();

        Assert.Equal(1, confirmed);
        Assert.Equal(0, cancelled);
    }

    [Fact]
    public void Click_No_RaisesOnCancelOnly()
    {
        var confirmed = 0;
        var cancelled = 0;
        var cut = RenderDialog(onConfirm: () => confirmed++, onCancel: () => cancelled++);

        cut.Find("[data-testid=confirm-no]").Click();

        Assert.Equal(0, confirmed);
        Assert.Equal(1, cancelled);
    }

    [Fact]
    public void Escape_RaisesOnCancel()
    {
        var cancelled = 0;
        var confirmed = 0;
        var cut = RenderDialog(onConfirm: () => confirmed++, onCancel: () => cancelled++);

        cut.Find("[data-testid=confirm-backdrop]").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(1, cancelled);
        Assert.Equal(0, confirmed);
    }

    [Theory]
    [InlineData("Enter")]
    [InlineData("a")]
    [InlineData("Tab")]
    public void OtherKeys_DoNotCancel(string key)
    {
        var cancelled = 0;
        var cut = RenderDialog(onCancel: () => cancelled++);

        cut.Find("[data-testid=confirm-backdrop]").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.Equal(0, cancelled);
    }

    [Fact]
    public void Focus_OnTheTrailingSentinel_WrapsToTheFirstButton()
    {
        var cut = RenderDialog();
        var before = JSInterop.Invocations.Count(i => i.Identifier == FocusIdentifier);

        cut.FindAll(".confirm-sentinel")[1].Focus();

        Assert.Equal(before + 1, JSInterop.Invocations.Count(i => i.Identifier == FocusIdentifier));
        JSInterop.Invocations.Last(i => i.Identifier == FocusIdentifier).Arguments[0].ShouldBeElementReferenceTo(cut.Find("[data-testid=confirm-yes]"));
    }

    [Fact]
    public void Focus_OnTheLeadingSentinel_WrapsToTheLastButton()
    {
        var cut = RenderDialog();
        var before = JSInterop.Invocations.Count(i => i.Identifier == FocusIdentifier);

        cut.FindAll(".confirm-sentinel")[0].Focus();

        Assert.Equal(before + 1, JSInterop.Invocations.Count(i => i.Identifier == FocusIdentifier));
        JSInterop.Invocations.Last(i => i.Identifier == FocusIdentifier).Arguments[0].ShouldBeElementReferenceTo(cut.Find("[data-testid=confirm-no]"));
    }
}
