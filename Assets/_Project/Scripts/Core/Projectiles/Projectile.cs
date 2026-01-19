using System.Collections.Generic;
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

        [Header("Base Settings")]
        public DamageDealer[] damageDealers;

        protected PlayerBase _owner;
        protected float _lifetime;
        protected Vector3 _spawnPosition;
        protected Quaternion _spawnRotation;

        protected override void OnValidate()
        {
            base.OnValidate();

            rb = GetComponent<Rigidbody>();
            netTransform = GetComponent<NetworkTransformReliable>();
            netRb = GetComponent<NetworkRigidbodyReliable>();

            rb.interpolation = RigidbodyInterpolation.Interpolate;
            netTransform.syncDirection = SyncDirection.ServerToClient;
            netRb.syncDirection = SyncDirection.ServerToClient;

            damageDealers = GetComponentsInChildren<DamageDealer>();
        }

        [Server]
        public static T Spawn<T>(T prefab, PlayerBase owner, Vector3 position, Quaternion rotation, bool init = true) where T : Projectile
        {
            if (MapLoader.loadedMap == null || !MapLoader.loadedMap.scene.IsValid()) throw new("Map is not loaded");

            var projectileObject = Instantiate(prefab.gameObject, position, rotation, new InstantiateParameters()
            {
                scene = MapLoader.loadedMap.scene,
            });
            var projectile = projectileObject.GetComponent<T>();
            projectile._owner = owner;
            projectile._spawnPosition = position;
            projectile._spawnRotation = rotation;

            foreach (var dealer in projectile.damageDealers)
            {
                dealer.owner = owner;
                dealer.OnHit.AddListener((player, damage) => projectile.OnDealerHit(dealer, player, damage));
            }

            if (init) projectile.Init();
            NetworkServer.Spawn(projectileObject);
            return projectile;
        }

        protected abstract void OnDealerHit(DamageDealer dealer, PlayerBase player, float damage);
        protected abstract void OnUpdate();

        protected abstract void Init();

        private void Update()
        {
            OnUpdate();
            _lifetime += Time.deltaTime;

            if (!NetworkServer.active) return;

            if (MapLoader.loadedMap == null || !MapLoader.loadedMap.info.Bounds.Contains(transform.position))
            {
                NetworkServer.Destroy(gameObject);
                return;
            }
        }
    }
}