namespace HeroPath.Modules.Game.Domain;

internal static class CommandNesting
{
    public static int DepthOf(IEnumerable<ICommand> commands) =>
        commands.OfType<RepeatCommand>().Select(r => 1 + DepthOf(r.Body)).DefaultIfEmpty(0).Max();

    public static IReadOnlyList<ICommand> CopyChecked(IEnumerable<ICommand> commands, string paramName)
    {
        ArgumentNullException.ThrowIfNull(commands, paramName);
        var copy = commands.ToList();
        if (copy.Any(c => c is null))
        {
            throw new ArgumentNullException(paramName, "Commands must not contain null.");
        }

        return copy.AsReadOnly();
    }
}
