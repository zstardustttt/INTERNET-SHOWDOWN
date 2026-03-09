using System;
using Game.Core.Broadcast;
using Mirror;
using UnityEngine.Events;

namespace Game.Core.Damages
{
    public struct SetTeamBroadcast
    {
        public Guid team;

        public SetTeamBroadcast(Guid team)
        {
            this.team = team;
        }
    }

    public class TeamReference : NetworkBehaviour, IBroadcastReceiver<SetTeamBroadcast>
    {
        public UnityEvent<Guid, Guid> onTeamChanged = new();

        [SyncVar(hook = nameof(OnTeamChanged))] public Guid team;

        private void OnTeamChanged(Guid old, Guid _new)
        {
            onTeamChanged.Invoke(old, _new);
        }

        public void Receive(SetTeamBroadcast broadcast)
        {
            team = broadcast.team;
        }
    }

    public static class TeamUtils
    {
        public static Guid Unwrap(this TeamReference self)
            => self ? self.team : Guid.Empty;

        public static bool CompareTeam(this Guid self, Guid other)
            => self != Guid.Empty && other != Guid.Empty && self == other;
    }
}