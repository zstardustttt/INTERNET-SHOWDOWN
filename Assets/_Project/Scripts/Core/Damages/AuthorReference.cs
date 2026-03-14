using Game.Core.Broadcast;
using Game.Core.Player;
using Mirror;
using UnityEngine;

namespace Game.Core.Damages
{
    public struct SetAuthorBroadcast
    {
        public PlayerCore author;

        public SetAuthorBroadcast(PlayerCore author)
        {
            this.author = author;
        }
    }

    public class AuthorReference : NetworkBehaviour, IBroadcastReceiver<SetAuthorBroadcast>
    {
        [HideInInspector] public PlayerCore author;

        public void Receive(SetAuthorBroadcast broadcast)
        {
            author = broadcast.author;
        }
    }

    public static class AuthorUtils
    {
        public static PlayerCore Unwrap(this AuthorReference self)
            => self ? self.author : null;
    }
}