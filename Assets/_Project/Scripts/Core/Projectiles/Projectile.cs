using Game.Core.Maps;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Core.Projectiles
{
    [RequireComponent(typeof(Rigidbody), typeof(NetworkTransformReliable), typeof(NetworkRigidbodyReliable))]
    public abstract class Projectile : NetworkBehaviour
    {
        [Header("Base Objects")]
        public Rigidbody rb;
        public NetworkTransformReliable netTransform;
        public NetworkRigidbodyReliable netRb;
        public ProjectileCollision collision;

        [HideInInspector] public PlayerBase author;
        [HideInInspector] public float lifetime;
        [HideInInspector] public Vector3 spawnPosition;
        [HideInInspector] public Quaternion spawnRotation;

        protected override void OnValidate()
        {
            base.OnValidate();

            rb = GetComponent<Rigidbody>();
            netTransform = GetComponent<NetworkTransformReliable>();
            netRb = GetComponent<NetworkRigidbodyReliable>();

            rb.interpolation = RigidbodyInterpolation.Interpolate;
            netTransform.syncDirection = SyncDirection.ServerToClient;
            netRb.syncDirection = SyncDirection.ServerToClient;

            TryGetComponent(out collision);
        }

        [Server]
        public static T Spawn<T>(T prefab, PlayerBase author, Vector3 headPosition, Vector3 position, Quaternion rotation) where T : Projectile
        {
            if (MapLoader.loadedMap == null || !MapLoader.loadedMap.scene.IsValid()) throw new("Map is not loaded");

            var projectileObject = Instantiate(prefab.gameObject, position, rotation, new InstantiateParameters()
            {
                scene = MapLoader.loadedMap.scene,
            });
            var projectile = projectileObject.GetComponent<T>();
            projectile.author = author;
            projectile.spawnPosition = position;
            projectile.spawnRotation = rotation;

            NetworkServer.Spawn(projectileObject);

            if (projectile.collision)
            {
                projectile.collision.onCollision.AddListener(projectile.OnCollision);
                projectile.collision.CheckLinecastBetweenTwoPoints(headPosition, position);
            }

            return projectile;
        }

        protected abstract void OnCollision(Vector3 point, Vector3 normal, Collider other);
        protected abstract void OnUpdate();

        protected void DestroyProjectile()
        {
            if (collision)
                collision.onCollision.RemoveAllListeners();

            NetworkServer.Destroy(gameObject);
        }

        private void Update()
        {
            lifetime += Time.deltaTime;
            OnUpdate();

            if (!NetworkServer.active) return;

            if (MapLoader.loadedMap == null || !MapLoader.loadedMap.info.Bounds.Contains(transform.position))
            {
                NetworkServer.Destroy(gameObject);
                return;
            }
        }
    }
}