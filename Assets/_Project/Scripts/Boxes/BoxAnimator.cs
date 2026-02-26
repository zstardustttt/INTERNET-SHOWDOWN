using System;
using System.Collections.Generic;
using Game.Boxes.Events;
using Game.Core.Events;
using UnityEngine;

namespace Game.Boxes
{
    [Serializable]
    public struct PossibleBoxMesh
    {
        public Mesh mesh;
        public float scale;
    }

    public class BoxAnimator : MonoBehaviour
    {
        public float moveFrequency;
        public float moveAmplitude;
        public float spinSpeed;
        public float visualUpdateInterval;
        public PossibleBoxMesh[] possibleMeshes;

        private List<ItemBox> _boxes;

        private void Awake()
        {
            _boxes = new();
            EventBus<OnBoxSpawn>.Listen((data) => _boxes.Add(data.box));
            EventBus<OnBoxDestroy>.Listen((data) => _boxes.Remove(data.box));
        }

        private void Update()
        {
            foreach (var box in _boxes)
            {
                box.visual.localPosition = Mathf.Sin(box.hash + Time.time * moveFrequency) * moveAmplitude * Vector3.up;
                box.visual.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
                box.visual.Rotate(Vector3.forward, spinSpeed * Time.deltaTime * Mathf.Sin(box.hash + Time.time * 0.5f));
                box.visual.Rotate(Vector3.right, spinSpeed * Time.deltaTime * Mathf.Cos(box.hash + Time.time * 0.5f));

                if (box.timer >= box.hash + visualUpdateInterval)
                {
                    box.meshIdx = (box.meshIdx + 1) % possibleMeshes.Length;
                    var mesh = possibleMeshes[box.meshIdx];
                    box.visual.localScale = mesh.scale * Vector3.one;
                    box.outerFieldFilter.mesh = mesh.mesh;
                    box.outlineFilter.mesh = mesh.mesh;
                    box.timer = box.hash;
                }
                box.timer += Time.deltaTime;
            }
        }
    }
}