using Game.Player;
using UnityEngine;

namespace Game.Core.Items
{
    public abstract class Item : MonoBehaviour
    {
        public ItemArgument[] arguments;
        public Vector3 offset;

        // Called on the server
        public abstract bool Use(PlayerBase user, ItemUseClientContext context);

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