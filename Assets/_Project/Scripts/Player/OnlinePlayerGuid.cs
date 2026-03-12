using System;
using UnityEngine;

namespace Game.Player
{
    public static class OnlinePlayerGuid
    {
        public static Guid Guid { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void GenerateGuid()
        {
            Guid = Guid.NewGuid();
            Debug.Log(Guid);
        }
    }
}