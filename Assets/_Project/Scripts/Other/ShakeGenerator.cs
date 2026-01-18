using Unity.Mathematics;
using UnityEngine;

namespace Game.Other
{
    public class ShakeGenerator
    {
        public float shakeAmplitude;
        public float shakeFrequency;
        public float shakeFalloffSpeed;

        public void Shake(float amplitude, float frequency, float falloffSpeed)
        {
            shakeAmplitude = amplitude;
            shakeFrequency = frequency;
            shakeFalloffSpeed = falloffSpeed;
        }

        public Vector3 GetShake()
        {
            var x = Time.time * shakeFrequency;
            var shake = new Vector3
            (
                noise.snoise(new float2(x)),
                noise.snoise(new float2(x + 1000f)),
                noise.snoise(new float2(x - 1000f))
            ) * shakeAmplitude;
            shakeAmplitude = Mathf.Lerp(shakeAmplitude, 0f, Time.deltaTime * shakeFalloffSpeed);

            return shake;
        }
    }
}