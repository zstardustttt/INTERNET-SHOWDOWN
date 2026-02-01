using Game.Player;
using UnityEngine;

namespace Game.Core.Items
{
    public abstract class Item : MonoBehaviour
    {
        public Vector3 offset;

        // Called on the server
        public abstract bool Use(PlayerBase user, ItemUseClientContext context, ItemArgument[] args);

        private Vector3 _previousPosition;
        private Vector3 _previousMoveDelta;

        public void Sway(Vector2 cameraMoveDelta)
        {
            var rawMoveDelta = transform.position - _previousPosition;
            var moveDelta = rawMoveDelta.sqrMagnitude < 0.01f ? Vector3.zero : (transform.position - _previousPosition).normalized * 3.5f;
            var smoothedMoveDelta = (moveDelta + _previousMoveDelta) / 2f;
            _previousMoveDelta = moveDelta;
            _previousPosition = transform.position;

            var totalDelta = new Vector2(smoothedMoveDelta.x + smoothedMoveDelta.z, smoothedMoveDelta.y) + cameraMoveDelta;

            var rotationX = Quaternion.AngleAxis(-totalDelta.y * 1.5f, Vector3.right);
            var rotationY = Quaternion.AngleAxis(totalDelta.x * 1.5f, Vector3.up);
            var targetRotation = rotationX * rotationY;

            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, 7.5f * Time.deltaTime);
        }
    }
}