using Game.Other;
using UnityEngine;

namespace Game.Core.Lobby
{
    public class LobbyInfo : MonoBehaviour
    {
        public static LobbyInfo Singleton { get; private set; }

        public Area spawnArea;
        public float minBoundsHeight;

        private void Awake()
        {
            Singleton = this;
        }
    }
}