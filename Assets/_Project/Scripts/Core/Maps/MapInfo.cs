using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Game.Core.Maps
{
    [Serializable]
    public struct SurfaceTriangulationData
    {
        public Vector2 boundsMin;
        public Vector2 boundsMax;
        public float totalArea;
        public SurfaceTriangle[] triangles;

        public static SurfaceTriangulationData Default()
        {
            return new()
            {
                totalArea = 0f,
                triangles = Array.Empty<SurfaceTriangle>()
            };
        }
    }

    [Serializable]
    public struct SurfaceTriangle
    {
        public Vector2 a;
        public Vector2 b;
        public Vector2 c;
        public float area;
    }

    [Serializable]
    public struct SurfacePoint
    {
        public Vector3 position;
        public Vector3 normal;
        public int gridIndex;
    }

    // TODO: Move surface point generation into a seperate SurfaceInformation class
    public class MapInfo : MonoBehaviour
    {
        [Header("Surface Point Sampling")]
        [Tooltip("Create an empty object under this MapInfo object and assign it")]
        public Transform surfaceShapePointsContainer;
        [Min(0.01f)] public float distanceBetweenSurfacePoints;
        public SurfaceTriangulationData surfaceTriangulationData;
        public SurfacePoint[] surfacePoints;
        public LayerMask surfaceLayerMask;
        public bool skipOnOdd;
        public float surfacePointRaycastDownwardsOffset = 0.02f;
        public UnityEvent onSurfacePointsBaked = new();

        [Header("Other")]
        [Tooltip("Possible points for spawning/respawning players")]
        public Transform[] spawnPoints;
        [Tooltip("Bounds used for destroying far projectiles, teleporting back players, etc. Always negative on each axis")]
        public Vector3 boundsMin;
        [Tooltip("Bounds used for destroying far projectiles, teleporting back players, etc. Always positive on each axis")]
        public Vector3 boundsMax;
        [HideInInspector] public Bounds Bounds { get; private set; }

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
        }

        public Vector3 GetRandomSpawnPoint() => spawnPoints[Random.Range(0, spawnPoints.Length)].position;

        [ContextMenu("Bake Surface Points")]
        public void BakeSurfacePoints()
        {
            if (distanceBetweenSurfacePoints <= 0f)
            {
                Debug.LogError("Distance between surface points must be greater than zero!");
                return;
            }

            surfaceTriangulationData = TriangulateSurface();
            surfacePoints = SampleSurfacePoints();

            onSurfacePointsBaked.Invoke();
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        // Triangulation with Ear Clipping algorithm
        private SurfaceTriangulationData TriangulateSurface()
        {
            if (!surfaceShapePointsContainer) return SurfaceTriangulationData.Default();

            var pointsCount = surfaceShapePointsContainer.childCount;
            if (pointsCount < 3) return SurfaceTriangulationData.Default();

            var verticesPool = new List<Vector2>();
            var minPoint = Vector2.zero;
            var maxPoint = Vector3.zero;
            foreach (Transform point in surfaceShapePointsContainer)
            {
                var projectedPoint = new Vector2(point.localPosition.x, point.localPosition.z);
                minPoint = Vector2.Min(minPoint, projectedPoint);
                maxPoint = Vector2.Max(maxPoint, projectedPoint);
                verticesPool.Add(projectedPoint);
            }

            var triangles = new List<SurfaceTriangle>();
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
                triangles = triangles.ToArray(),
                boundsMin = minPoint,
                boundsMax = maxPoint
            };
        }

        private SurfacePoint[] SampleSurfacePoints()
        {
            var previousQueriesHitBackfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;

            var sizeX = surfaceTriangulationData.boundsMax.x - surfaceTriangulationData.boundsMin.x;
            var sizeY = surfaceTriangulationData.boundsMax.y - surfaceTriangulationData.boundsMin.y;

            var countX = (int)(sizeX / distanceBetweenSurfacePoints);
            var countY = (int)(sizeY / distanceBetweenSurfacePoints);

            var maxRaycastDistance = boundsMax.y - boundsMin.y;

            var translationToWorld = new Vector3(surfaceShapePointsContainer.position.x, boundsMax.y, surfaceShapePointsContainer.position.z);
            var output = new List<SurfacePoint>(countX * countY);
            for (int i = 0; i < countX; i++)
            {
                for (int j = 0; j < countY; j++)
                {
                    var originOnShape = new Vector2(i * distanceBetweenSurfacePoints, j * distanceBetweenSurfacePoints) + surfaceTriangulationData.boundsMin;
                    if (!IsPointInsideSurface(originOnShape)) continue;

                    var concaveHitCounter = skipOnOdd ? 0 : -1;
                    var worldRayOrigin = new Vector3(originOnShape.x, 0f, originOnShape.y) + translationToWorld;
                    var raycastDistance = maxRaycastDistance;
                    while (Physics.Raycast(worldRayOrigin, Vector3.down, out var hit, raycastDistance, surfaceLayerMask))
                    {
                        worldRayOrigin = hit.point + Vector3.down * surfacePointRaycastDownwardsOffset;
                        raycastDistance -= hit.distance + surfacePointRaycastDownwardsOffset;
                        if (hit.collider is MeshCollider meshCollider && !meshCollider.convex)
                        {
                            concaveHitCounter++;
                            if (hit.normal.y <= 0f || concaveHitCounter % 2 != 0) continue;
                        }

                        output.Add(new()
                        {
                            position = hit.point,
                            normal = hit.normal,
                            gridIndex = i * countY + j
                        });
                    }
                }
            }

            Physics.queriesHitBackfaces = previousQueriesHitBackfaces;
            return output.ToArray();
        }

        public bool IsPointInsideSurface(Vector2 pointOnShape)
        {
            foreach (var triangle in surfaceTriangulationData.triangles)
            {
                if (IsPointInsideTriangle(pointOnShape, triangle.a, triangle.b, triangle.c))
                    return true;
            }

            return false;
        }

        // TODO: remove
        public Vector3 SelectRandomPointOnSurface()
        {
            var probabilityOffset = 0f;
            var selectedTriangleIdx = -1;
            var triangleSelectionRandom = Random.value;
            for (int i = 0; i < surfaceTriangulationData.triangles.Length; i++)
            {
                var triangle = surfaceTriangulationData.triangles[i];
                var areaRatio = triangle.area / surfaceTriangulationData.totalArea;
                if (triangleSelectionRandom >= probabilityOffset && triangleSelectionRandom < probabilityOffset + areaRatio)
                {
                    selectedTriangleIdx = i;
                    break;
                }
                probabilityOffset += areaRatio;
            }

            if (selectedTriangleIdx == -1) return Vector2.zero;
            var selectedTriangle = surfaceTriangulationData.triangles[selectedTriangleIdx];

            var triangleOrigin = Vector2.Min(selectedTriangle.a, Vector2.Min(selectedTriangle.b, selectedTriangle.c));
            var relativeA = selectedTriangle.a - triangleOrigin;
            var relativeB = selectedTriangle.b - triangleOrigin;
            var relativeC = selectedTriangle.c - triangleOrigin;

            var trianglePointRandom1 = Random.value;
            var trianglePointRandom2 = Random.value;
            var triangleU = 1f - Mathf.Sqrt(trianglePointRandom1);
            var triangleV = Mathf.Sqrt(trianglePointRandom1) * (1f - trianglePointRandom2);
            var triangleW = 1f - triangleU - triangleV;
            var relativeRandomPoint = triangleU * relativeA + triangleV * relativeB + triangleW * relativeC;
            var randomPoint = relativeRandomPoint + triangleOrigin;
            return transform.position + new Vector3(randomPoint.x, boundsMax.y, randomPoint.y);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + (boundsMin + boundsMax) / 2f, boundsMax - boundsMin);

            for (int i = 0; i < surfaceTriangulationData.triangles.Length; i++)
            {
                Gizmos.color = Color.HSVToRGB((float)i / surfaceTriangulationData.triangles.Length, 1f, 1f) - Color.black * 0.5f;
                var tri = surfaceTriangulationData.triangles[i];

                var aWorld = transform.position + new Vector3(tri.a.x, boundsMax.y, tri.a.y);
                var bWorld = transform.position + new Vector3(tri.b.x, boundsMax.y, tri.b.y);
                var cWorld = transform.position + new Vector3(tri.c.x, boundsMax.y, tri.c.y);

                Gizmos.DrawLine(aWorld, bWorld);
                Gizmos.DrawLine(bWorld, cWorld);
                Gizmos.DrawLine(cWorld, aWorld);
            }

            Gizmos.color = Color.darkRed;
            var shapeBoundsMin = new Vector3(surfaceTriangulationData.boundsMin.x, boundsMax.y, surfaceTriangulationData.boundsMin.y);
            var shapeBoundsMax = new Vector3(surfaceTriangulationData.boundsMax.x, boundsMax.y, surfaceTriangulationData.boundsMax.y);
            Gizmos.DrawWireCube(surfaceShapePointsContainer.position + (shapeBoundsMin + shapeBoundsMax) / 2f, shapeBoundsMax - shapeBoundsMin);

            Gizmos.color = Color.red;
            var previousPoint = surfaceShapePointsContainer.GetChild(surfaceShapePointsContainer.childCount - 1);
            foreach (Transform point in surfaceShapePointsContainer)
            {
                Gizmos.DrawLine(previousPoint.position + Vector3.up * boundsMax.y, point.position + Vector3.up * boundsMax.y);
                previousPoint = point;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (surfacePoints == null) return;

            Gizmos.color = Color.blue;
            foreach (var surfacePoint in surfacePoints)
            {
                Gizmos.DrawWireSphere(surfacePoint.position, 0.15f);
            }

            Gizmos.color = Color.darkBlue;
            foreach (var surfacePoint in surfacePoints)
            {
                Gizmos.DrawRay(surfacePoint.position, surfacePoint.normal);
            }
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
    }
}