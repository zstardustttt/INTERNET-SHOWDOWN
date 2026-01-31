using Game.Player;
using UnityEngine;

namespace Game.Core.Items
{
    public abstract class Item : MonoBehaviour
    {
        public Vector3 offset;

        // Called on the server
        public abstract bool Use(PlayerBase user, ItemUseClientContext context, ItemArgument[] args);

        private Quaternion _previousSwayRotation;

        public void Sway(Quaternion targetRotation)
        {
            _previousSwayRotation = Quaternion.Slerp(_previousSwayRotation, targetRotation, 0.5f);
            transform.rotation = _previousSwayRotation;
        }
    }
}