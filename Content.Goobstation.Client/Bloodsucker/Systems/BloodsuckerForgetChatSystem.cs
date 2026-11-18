using Content.Client.UserInterface.Systems.Chat;
using Content.Goobstation.Shared.Bloodsuckers.Events;
using Content.Shared.Chat;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Goobstation.Client.Bloodsucker.Systems;

/// <summary>
/// Hides the in-character chat a Forget victim saw in the last minute.
/// </summary>
public sealed partial class BloodsuckerForgetChatSystem : EntitySystem
{
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private IGameTiming _timing = default!;

    private const ChatChannel ForgottenChannels = ChatChannel.Local | ChatChannel.Whisper | ChatChannel.Emotes | ChatChannel.Radio;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<BloodsuckerForgetChatEvent>(OnForgetChat);
    }

    private void OnForgetChat(BloodsuckerForgetChatEvent ev)
    {
        var ticks = (uint) (ev.Seconds * _timing.TickRate);
        var cutoff = _timing.CurTick.Value > ticks ? _timing.CurTick.Value - ticks : 0;

        var chat = _ui.GetUIController<ChatUIController>();
        foreach (var (tick, msg) in chat.History)
        {
            if (tick.Value >= cutoff && (msg.Channel & ForgottenChannels) != 0)
                msg.WrappedMessage = "[color=gray][italic]...[/italic][/color]";
        }

        chat.Repopulate();
    }
}
