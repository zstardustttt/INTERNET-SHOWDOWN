using UnityEngine;
using Mirror;

namespace Game.Network
{
    public class NetworkDestroyAfterTime : NetworkBehaviour
    {
        [Tooltip("Lifetime duration in seconds")] public float time;
        protected float _timer;
        private bool _destroyed;

        private void Update()
        {
            if (!NetworkServer.active || _destroyed) return;

            if (_timer >= time)
            {
                NetworkServer.Destroy(gameObject);
                _destroyed = true;
                return;
            }

            _timer += Time.deltaTime;
            OnUpdate();
        }

        protected virtual void OnUpdate() { }
    }
}