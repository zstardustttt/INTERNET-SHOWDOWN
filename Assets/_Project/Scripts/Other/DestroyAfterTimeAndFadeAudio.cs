using Game.Network;
using UnityEngine;

namespace Game.Other
{
    public class DestroyAfterTimeAndFadeAudio : NetworkDestroyAfterTime
    {
        public AudioSource source;
        public float volume;
        public AnimationCurve volumeFalloffCurve;

        protected override void OnUpdate()
        {
            source.volume = volumeFalloffCurve.Evaluate(_timer / time) * volume;
        }
    }
}