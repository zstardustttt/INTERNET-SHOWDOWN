using Game.Core.Hits;
using UnityEngine;

namespace Game.Hits
{
    [RequireComponent(typeof(HitListener))]
    public class DeactivateHitListenerAfterTime : MonoBehaviour
    {
        public HitListener hitListener;
        public float time;

        private float _timer;

        private void Awake()
        {
            hitListener.beforeHitScan.AddListener(BeforeHitScan);
        }

        private void BeforeHitScan()
        {
            if (_timer >= time)
            {
                hitListener.active = false;
                return;
            }

            _timer += Time.deltaTime;
        }
    }
}