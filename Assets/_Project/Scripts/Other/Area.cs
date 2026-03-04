using UnityEngine;

namespace Game.Other
{
    public class Area : MonoBehaviour
    {
        public Vector3 areaMin;
        public Vector3 areaMax;

        private void OnValidate()
        {
            areaMin.x = Mathf.Min(areaMin.x, 0f);
            areaMin.y = Mathf.Min(areaMin.y, 0f);
            areaMin.z = Mathf.Min(areaMin.z, 0f);

            areaMax.x = Mathf.Max(areaMax.x, 0f);
            areaMax.y = Mathf.Max(areaMax.y, 0f);
            areaMax.z = Mathf.Max(areaMax.z, 0f);
        }

        public Vector3 RandomSampleArea(Space space)
        {
            var sample = new Vector3
            (
                Random.Range(areaMin.x, areaMax.x),
                Random.Range(areaMin.y, areaMax.y),
                Random.Range(areaMin.z, areaMax.z)
            );

            if (space == Space.Self) return sample;
            else return transform.TransformPoint(sample);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;

            var min = transform.TransformPoint(areaMin);
            var max = transform.TransformPoint(areaMax);
            Gizmos.DrawWireCube((min + max) / 2f, max - min);
        }
    }
}