using Game.Player;
using UnityEngine;

namespace Game.Core.Items
{
    public abstract class Item : MonoBehaviour
    {
        public Vector3 offset;

        // Called on the server
        public abstract bool Use(PlayerBase user, ItemUseClientContext context, ItemArgument[] args);
    }
}