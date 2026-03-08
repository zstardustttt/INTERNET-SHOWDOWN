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
        public const int MAX_BOXES_PER_MESH = 512;

        public float moveFrequency;
        public float moveAmplitude;
        public float spinSpeed;
        public float visualUpdateInterval;
        public PossibleBoxMesh[] possibleMeshes;

        [Header("Instancing")]
        public Material outerFieldMaterial;

        private List<ItemBox> _boxes;
        private Matrix4x4[][] _drawCallData;
        private int[] _counters;

        private void Awake()
        {
            _boxes = new();
            _drawCallData = new Matrix4x4[possibleMeshes.Length][];
            for (int i = 0; i < possibleMeshes.Length; i++)
            {
                _drawCallData[i] = new Matrix4x4[MAX_BOXES_PER_MESH];
            }
            _counters = new int[possibleMeshes.Length];

            EventBus<OnBoxSpawn>.Listen((data) => _boxes.Add(data.box));
            EventBus<OnBoxDestroy>.Listen((data) => _boxes.Remove(data.box));
        }

        private void Update()
        {
            if (_boxes.Count == 0) return;

            foreach (var box in _boxes)
            {
                var visual = box.visual;

                visual.localPosition = Mathf.Sin(box.hash + Time.time * moveFrequency) * moveAmplitude * Vector3.up;

                var spinDelta = spinSpeed * Time.deltaTime;
                visual.Rotate(Vector3.up, spinDelta);

                var spinFactorArg = box.hash + Time.time * 0.5f;
                visual.Rotate(Vector3.forward, spinDelta * Mathf.Sin(spinFactorArg));
                visual.Rotate(Vector3.right, spinDelta * Mathf.Cos(spinFactorArg));

                if (box.timer >= box.hash + visualUpdateInterval)
                {
                    box.meshIdx = (box.meshIdx + 1) % possibleMeshes.Length;
                    var mesh = possibleMeshes[box.meshIdx];
                    visual.localScale = mesh.scale * Vector3.one;
                    box.outlineFilter.mesh = mesh.mesh;
                    box.timer = box.hash;
                }
                box.timer += Time.deltaTime;

                var counter = _counters[box.meshIdx];
                if (counter < MAX_BOXES_PER_MESH)
                {
                    _drawCallData[box.meshIdx][counter] = visual.transform.localToWorldMatrix;
                    _counters[box.meshIdx]++;
                }
            }

            for (int meshIdx = 0; meshIdx < possibleMeshes.Length; meshIdx++)
            {
                var mesh = possibleMeshes[meshIdx].mesh;
                var counter = _counters[meshIdx];
                if (counter == 0) continue;

                UnityEngine.Graphics.RenderMeshInstanced(new RenderParams(outerFieldMaterial), mesh, 0, _drawCallData[meshIdx], counter);
                _counters[meshIdx] = 0;
            }
        }
    }
}