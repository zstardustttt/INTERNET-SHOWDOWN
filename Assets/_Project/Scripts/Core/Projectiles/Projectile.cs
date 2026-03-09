using System;
using Game.Core.Broadcast;
using Game.Core.Damages;
using Game.Core.Maps;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Core.Projectiles
{
    [RequireComponent(typeof(Rigidbody), typeof(NetworkTransformReliable), typeof(NetworkRigidbodyReliable))]
    [RequireComponent(typeof(AuthorReference), typeof(TeamReference))]
    public abstract class Projectile : NetworkBehaviour
    {
        [Header("Base Objects")]
        public Rigidbody rb;
        public NetworkTransformReliable netTransform;
        public NetworkRigidbodyReliable netRb;
        public AuthorReference authorReference;
        public TeamReference teamReference;

        [HideInInspector] public float lifetime;
        [HideInInspector] public Vector3 spawnPosition;
        [HideInInspector] public Quaternion spawnRotation;
        [HideInInspector] public double spawnTime;

        protected override void OnValidate()
        {
            base.OnValidate();

            rb = GetComponent<Rigidbody>();
            netTransform = GetComponent<NetworkTransformReliable>();
            netRb = GetComponent<NetworkRigidbodyReliable>();
            authorReference = GetComponent<AuthorReference>();
            teamReference = GetComponent<TeamReference>();

            rb.interpolation = RigidbodyInterpolation.Interpolate;
            netTransform.syncDirection = SyncDirection.ServerToClient;
            netRb.syncDirection = SyncDirection.ServerToClient;
        }

        [Server]
        public static T Spawn<T>(T prefab, PlayerBase author, Guid team, Vector3 position, Quaternion rotation, double spawnTime, Action<T> setup = null) where T : Projectile
        {
            if (MapLoader.loadedMap == null || !MapLoader.loadedMap.scene.IsValid())
                throw new("Can't spawn projectile: Map is not loaded");

            var projectileObject = Instantiate(prefab.gameObject, position, rotation, new InstantiateParameters()
            {
                scene = MapLoader.loadedMap.scene,
            });
            var projectile = projectileObject.GetComponent<T>();
            projectile.authorReference.author = author;
            projectile.teamReference.team = team;
            projectile.spawnPosition = position;
            projectile.spawnRotation = rotation;
            projectile.spawnTime = spawnTime;
            setup?.Invoke(projectile);

            projectile.gameObject.BroadcastOnChildren(new SetAuthorBroadcast(author));

            NetworkServer.Spawn(projectileObject);
            projectile.OnSpawned();
            return projectile;
        }

        protected virtual void OnSpawned() { }
        protected virtual void OnDestroyed() { }
        protected virtual void OnUpdate() { }

        protected void DestroyProjectile()
        {
            NetworkServer.Destroy(gameObject);
            OnDestroyed();
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