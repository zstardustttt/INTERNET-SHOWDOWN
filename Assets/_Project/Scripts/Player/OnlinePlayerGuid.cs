using UnityEngine;

namespace Game.Player
{
    public static class OnlinePlayerGuid
    {
        public static string Guid { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void GenerateGuid()
        {
            Guid = System.Guid.NewGuid().ToString();
            Debug.Log(Guid);
        }
    }
}