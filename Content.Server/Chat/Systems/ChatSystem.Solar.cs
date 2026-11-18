using Content.Goobstation.Common.Bloodsuckers;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    /// <summary>
    /// The name the listener sees for the speaker. Forget makes the victim see the Bloodsucker as Unknown.
    /// </summary>
    private string ListenerName(EntityUid listener, EntityUid source, string name)
    {
        return TryComp<BloodsuckerForgottenComponent>(listener, out var forgotten) && forgotten.Target == source
            ? Loc.GetString("bloodsucker-forget-unknown-name")
            : name;
    }

    private string ListenerWrapped(EntityUid listener, EntityUid source, string name, string wrapped)
    {
        var listenerName = ListenerName(listener, source, name);
        if (listenerName == name || string.IsNullOrEmpty(name))
            return wrapped;

        var index = wrapped.IndexOf(name, StringComparison.Ordinal);
        return index < 0 ? wrapped : wrapped.Remove(index, name.Length).Insert(index, listenerName);
    }
}
