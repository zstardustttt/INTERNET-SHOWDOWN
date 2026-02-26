using UnityEngine;
using System.Linq;

namespace Game.Core.Broadcast
{
    public interface IBroadcastReceiver<T>
    {
        public void Receive(T broadcast);
    }

    public static class GameObjectExtensions
    {
        public static void BroadcastOnGameObject<T>(this GameObject obj, T broadcast)
        {
            foreach (var c in obj.GetInterfaceComponents<IBroadcastReceiver<T>>())
                c.Receive(broadcast);
        }

        public static void BroadcastOnChildren<T>(this GameObject obj, T broadcast)
        {
            foreach (var c in obj.GetInterfaceComponentsInChildren<IBroadcastReceiver<T>>())
                c.Receive(broadcast);
        }

        public static void BroadcastOnHierarchy<T>(this GameObject obj, T broadcast)
        {
            var root = obj.transform.root.gameObject;
            foreach (var c in root.GetInterfaceComponentsInChildren<IBroadcastReceiver<T>>())
                c.Receive(broadcast);
        }

        public static I[] GetInterfaceComponents<I>(this GameObject obj) where I : class
        {
            return obj.GetComponents(typeof(I)).Select(c => c as I).ToArray();
        }

        public static I[] GetInterfaceComponentsInChildren<I>(this GameObject obj) where I : class
        {
            return obj.GetComponentsInChildren(typeof(I)).Select(c => c as I).ToArray();
        }
    }
}