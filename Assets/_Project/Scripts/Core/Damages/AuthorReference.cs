using Game.Core.Broadcast;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Core.Damages
{
    public struct SetAuthorBroadcast
    {
        public PlayerBase author;

        public SetAuthorBroadcast(PlayerBase author)
        {
            this.author = author;
        }
    }

    public class AuthorReference : NetworkBehaviour, IBroadcastReceiver<SetAuthorBroadcast>
    {
        [HideInInspector] public PlayerBase author;

        public void Receive(SetAuthorBroadcast broadcast)
        {
            author = broadcast.author;
        }
    }

    public static class AuthorUtils
    {
        public static PlayerBase Unwrap(this AuthorReference self)
            => self ? self.author : null;
    }
}