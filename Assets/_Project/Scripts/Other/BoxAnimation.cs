using System;
using UnityEngine;

namespace Game.Other
{
    [Serializable]
    public struct PossibleBoxMesh
    {
        public Mesh mesh;
        public float scale;
    }

    public class BoxAnimation : MonoBehaviour
    {
        public float moveFrequency;
        public float moveAmplitude;
        public float spinSpeed;
        public PossibleBoxMesh[] possibleMeshes;
        public MeshFilter outerFieldFilter;
        public MeshFilter outlineFilter;
        public float visualUpdateInterval;
        public Transform visual;

        private float _hash;
        private int _meshIdx;
        private float _timer;

        private void Awake()
        {
            _hash = transform.position.magnitude / 3f;
            _timer = _hash;
        }

        private void Update()
        {
            visual.localPosition = Mathf.Sin(_hash + Time.time * moveFrequency) * moveAmplitude * Vector3.up;
            visual.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
            visual.Rotate(Vector3.forward, spinSpeed * Time.deltaTime * Mathf.Sin(_hash + Time.time * 0.5f));
            visual.Rotate(Vector3.right, spinSpeed * Time.deltaTime * Mathf.Cos(_hash + Time.time * 0.5f));

            if (_timer >= _hash + visualUpdateInterval)
            {
                _meshIdx = (_meshIdx + 1) % possibleMeshes.Length;
                var mesh = possibleMeshes[_meshIdx];
                visual.localScale = mesh.scale * Vector3.one;
                outerFieldFilter.mesh = mesh.mesh;
                outlineFilter.mesh = mesh.mesh;
                _timer = _hash;
            }
            _timer += Time.deltaTime;
        }
    }
}