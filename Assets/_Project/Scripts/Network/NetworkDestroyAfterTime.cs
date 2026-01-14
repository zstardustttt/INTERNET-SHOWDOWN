using UnityEngine;
using Mirror;

namespace Game.Network
{
    public class NetworkDestroyAfterTime : NetworkBehaviour
    {
        public float time;
        private float _timer;
        private bool _destroyed;

        private void Update()
        {
            if (!NetworkServer.active || _destroyed) return;

            if (_timer >= time)
            {
                NetworkServer.Destroy(gameObject);
                _destroyed = true;
            }

            _timer += Time.deltaTime;
        }
    }
}