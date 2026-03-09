using System;
using Game.Core.Broadcast;
using Mirror;

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
        [SyncVar] public Guid team;

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