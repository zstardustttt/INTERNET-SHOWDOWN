using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Game.Core.Maps
{
    public struct BoxSpawnShapeTriangulationData
    {
        public float totalArea;
        public BoxSpawnTriangle[] triangles;

        public static BoxSpawnShapeTriangulationData Default()
        {
            return new()
            {
                totalArea = 0f,
                triangles = Array.Empty<BoxSpawnTriangle>()
            };
        }
    }

    public struct BoxSpawnTriangle
    {
        public Vector2 a;
        public Vector2 b;
        public Vector2 c;
        public float area;
    }

    public class MapInfo : MonoBehaviour
    {
        [Tooltip("Create an empty object under this MapInfo object and assign it")]
        public Transform boxSpawnShapePointsContainer;
        [Tooltip("Possible points for spawning/respawning players")]
        public Transform[] spawnPoints;
        [Tooltip("Bounds used for destroying far projectiles, teleporting back players, etc. Always negative on each axis")]
        public Vector3 boundsMin;
        [Tooltip("Bounds used for destroying far projectiles, teleporting back players, etc. Always positive on each axis")]
        public Vector3 boundsMax;
        [HideInInspector] public Bounds Bounds { get; private set; }
        [HideInInspector] public BoxSpawnShapeTriangulationData boxSpawnShapeTriangulationData;

        private void OnValidate()
        {
            boundsMin.x = Mathf.Min(boundsMin.x, 0f);
            boundsMin.y = Mathf.Min(boundsMin.y, 0f);
            boundsMin.z = Mathf.Min(boundsMin.z, 0f);

            boundsMax.x = Mathf.Max(boundsMax.x, 0f);
            boundsMax.y = Mathf.Max(boundsMax.y, 0f);
            boundsMax.z = Mathf.Max(boundsMax.z, 0f);
        }

        private void Awake()
        {
            Bounds = new(transform.position + (boundsMin + boundsMax) / 2f, boundsMax - boundsMin);
            boxSpawnShapeTriangulationData = TriangulateBoxSpawnShape();
        }

        // Triangulation with Ear Clipping algorithm
        private BoxSpawnShapeTriangulationData TriangulateBoxSpawnShape()
        {
            if (!boxSpawnShapePointsContainer) return BoxSpawnShapeTriangulationData.Default();

            var pointsCount = boxSpawnShapePointsContainer.childCount;
            if (pointsCount < 3) return BoxSpawnShapeTriangulationData.Default();

            var verticesPool = new List<Vector2>();
            foreach (Transform point in boxSpawnShapePointsContainer)
            {
                verticesPool.Add(new(point.localPosition.x, point.localPosition.z));
            }

            var triangles = new List<BoxSpawnTriangle>();
            var totalArea = 0f;
            for (int triIdx = 0; triIdx < pointsCount - 2; triIdx++)
            {
                for (int vertBIdx = 0; vertBIdx < verticesPool.Count; vertBIdx++)
                {
                    var vertAIdx = NegMod(vertBIdx - 1, verticesPool.Count);
                    var vertCIdx = NegMod(vertBIdx + 1, verticesPool.Count);

                    var vertA = verticesPool[vertAIdx];
                    var vertB = verticesPool[vertBIdx];
                    var vertC = verticesPool[vertCIdx];

                    // Angle check
                    var vecBA = vertB - vertA;
                    var vecCB = vertC - vertB;
                    if (Vec2Cross(vecBA, vecCB) >= 0) continue;

                    // Inside triangle check
                    var somePointInsideTriangle = false;
                    for (int i = 0; i < verticesPool.Count; i++)
                    {
                        if (i == vertAIdx || i == vertBIdx || i == vertCIdx) continue;
                        if (IsPointInsideTriangle(verticesPool[i], vertA, vertB, vertC))
                        {
                            somePointInsideTriangle = true;
                            break;
                        }
                    }

                    if (somePointInsideTriangle) continue;

                    var triangleArea = Mathf.Abs(vertA.x * (vertB.y - vertC.y) + vertB.x * (vertC.y - vertA.y) + vertC.x * (vertA.y - vertB.y)) / 2f;
                    totalArea += triangleArea;
                    triangles.Add(new()
                    {
                        a = vertA,
                        b = vertB,
                        c = vertC,
                        area = triangleArea
                    });

                    verticesPool.RemoveAt(vertBIdx);
                    break;
                }
            }

            return new()
            {
                totalArea = totalArea,
                triangles = triangles.ToArray()
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int NegMod(int x, int m) => (x % m + m) % m;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float Vec2Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private bool IsPointInsideTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            var ab = b - a;
            var bc = c - b;
            var ca = a - c;

            var ap = p - a;
            var bp = p - b;
            var cp = p - c;

            var crossA = Vec2Cross(ab, ap);
            var crossB = Vec2Cross(bc, bp);
            var crossC = Vec2Cross(ca, cp);

            return crossA <= 0f && crossB <= 0f && crossC <= 0f;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + (boundsMin + boundsMax) / 2f, boundsMax - boundsMin);

            if (!boxSpawnShapePointsContainer || boxSpawnShapePointsContainer.childCount == 0) return;

            var tris = TriangulateBoxSpawnShape().triangles;
            for (int i = 0; i < tris.Length; i++)
            {
                Gizmos.color = Color.HSVToRGB((float)i / tris.Length, 1f, 1f) - Color.black * 0.5f;
                var tri = tris[i];

                var aWorld = transform.position + new Vector3(tri.a.x, boundsMax.y, tri.a.y);
                var bWorld = transform.position + new Vector3(tri.b.x, boundsMax.y, tri.b.y);
                var cWorld = transform.position + new Vector3(tri.c.x, boundsMax.y, tri.c.y);

                Gizmos.DrawLine(aWorld, bWorld);
                Gizmos.DrawLine(bWorld, cWorld);
                Gizmos.DrawLine(cWorld, aWorld);
            }

            Gizmos.color = Color.red;
            var previousPoint = boxSpawnShapePointsContainer.GetChild(boxSpawnShapePointsContainer.childCount - 1);
            foreach (Transform point in boxSpawnShapePointsContainer)
            {
                Gizmos.DrawLine(previousPoint.position + Vector3.up * boundsMax.y, point.position + Vector3.up * boundsMax.y);
                previousPoint = point;
            }
        }
    }
}