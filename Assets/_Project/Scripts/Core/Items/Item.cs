using Game.Player;
using UnityEngine;

namespace Game.Core.Items
{
    public abstract class Item : MonoBehaviour
    {
        public Vector3 offset;

        // Called on the server
        public abstract void Use(PlayerBase user, ItemUseClientContext context);
    }
}