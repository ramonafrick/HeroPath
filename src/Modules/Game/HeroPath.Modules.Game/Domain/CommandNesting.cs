namespace HeroPath.Modules.Game.Domain;

internal static class CommandNesting
{
    public static int DepthOf(IEnumerable<Command> commands) =>
        commands.OfType<RepeatCommand>().Select(r => 1 + DepthOf(r.Body)).DefaultIfEmpty(0).Max();

    public static int CountAll(IEnumerable<Command> commands) =>
        commands.Sum(c => c is RepeatCommand r ? 1 + CountAll(r.Body) : 1);

    public static IReadOnlyList<Command> CopyChecked(IEnumerable<Command> commands, string paramName)
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
