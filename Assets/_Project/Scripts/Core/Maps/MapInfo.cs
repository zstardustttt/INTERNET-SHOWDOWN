using UnityEngine;

namespace Game.Core.Maps
{
    public class MapInfo : MonoBehaviour
    {
        public Transform[] spawnPoints;
        [Tooltip("Bounds used for destroying far projectiles, teleporting back players, etc. Always negative on each axis")]
        public Vector3 boundsMin;
        [Tooltip("Bounds used for destroying far projectiles, teleporting back players, etc. Always positive on each axis")]
        public Vector3 boundsMax;
        [Tooltip("Plane used for spawning boxes (pickable items). X - min X; Y - min Z; Z - max X; W - max Z")]
        public Vector4 boxSpawnPlane;
        [HideInInspector] public Bounds Bounds { get; private set; }

        private void OnValidate()
        {
            boundsMin.x = Mathf.Min(boundsMin.x, 0f);
            boundsMin.y = Mathf.Min(boundsMin.y, 0f);
            boundsMin.z = Mathf.Min(boundsMin.z, 0f);

            boundsMax.x = Mathf.Max(boundsMax.x, 0f);
            boundsMax.y = Mathf.Max(boundsMax.y, 0f);
            boundsMax.z = Mathf.Max(boundsMax.z, 0f);

            boxSpawnPlane.x = Mathf.Min(boxSpawnPlane.x, 0f);
            boxSpawnPlane.y = Mathf.Min(boxSpawnPlane.y, 0f);
            boxSpawnPlane.z = Mathf.Max(boxSpawnPlane.z, 0f);
            boxSpawnPlane.w = Mathf.Max(boxSpawnPlane.w, 0f);
        }

        private void Awake()
        {
            Bounds = new(transform.position + (boundsMin + boundsMax) / 2f, boundsMax - boundsMin);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + (boundsMin + boundsMax) / 2f, boundsMax - boundsMin);

            Gizmos.color = Color.red;
            var planeMin = new Vector3(boxSpawnPlane.x, boundsMax.y, boxSpawnPlane.y);
            var planeMax = new Vector3(boxSpawnPlane.z, boundsMax.y, boxSpawnPlane.w);
            Gizmos.DrawWireCube(transform.position + (planeMin + planeMax) / 2f, planeMax - planeMin);
        }
    }
}