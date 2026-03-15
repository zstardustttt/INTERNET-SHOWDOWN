using Game.Core.Player;
using UnityEngine;

namespace Game.Core.Items
{
    public struct ItemUseOptions
    {
        public bool reset;
        public bool activity;
        public bool events;

        public ItemUseOptions(bool reset, bool activity, bool events)
        {
            this.reset = reset;
            this.activity = activity;
            this.events = events;
        }
    }

    public abstract class Item : MonoBehaviour
    {
        public ItemArgument[] arguments;
        public Vector3 offset;

        // Called on the server
        public abstract ItemUseOptions Use(PlayerCore user, ItemUseClientContext context);

        public void Sway(Vector2 cameraMoveDelta, Vector3 velocity, Transform orientation)
        {
            var directions = orientation.right + orientation.up;
            var moveDelta = Vector3.ClampMagnitude(new Vector3(
                velocity.x * directions.x,
                velocity.y * directions.y,
                velocity.z * directions.z
            ) * 0.5f, 10f);
            var totalDelta = cameraMoveDelta - new Vector2(moveDelta.x + moveDelta.z, moveDelta.y);

            var rotationX = Quaternion.AngleAxis(-totalDelta.y * 1.5f, Vector3.right);
            var rotationY = Quaternion.AngleAxis(totalDelta.x * 1.5f, Vector3.up);
            var targetRotation = rotationX * rotationY;

            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, 7.5f * Time.deltaTime);
        }
    }
}