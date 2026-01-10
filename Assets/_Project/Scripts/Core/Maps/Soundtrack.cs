using UnityEngine;

namespace Game.Core.Maps
{
    [CreateAssetMenu(fileName = "Soundtrack", menuName = "Soundtrack", order = 0)]
    public class Soundtrack : ScriptableObject
    {
        public AudioClip clip;
        public string title;
        public string author;
        [Range(0f, 1f)] public float volume = 1f;
    }
}