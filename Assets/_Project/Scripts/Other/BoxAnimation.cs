using UnityEngine;

namespace Game.Other
{
    public class BoxAnimation : MonoBehaviour
    {
        public float moveFrequency;
        public float moveAmplitude;

        private float _hash;
        private Vector3 _spawnPosition;

        private void Awake()
        {
            _spawnPosition = transform.position;
            _hash = _spawnPosition.magnitude / 3f;
        }

        private void Update()
        {
            transform.position = _spawnPosition + Mathf.Sin(_hash + Time.time * moveFrequency) * moveAmplitude * Vector3.up;
        }
    }
}