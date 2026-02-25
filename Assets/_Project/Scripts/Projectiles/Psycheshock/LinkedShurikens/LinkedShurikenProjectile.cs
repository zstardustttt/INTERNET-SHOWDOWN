using Game.Core.Maps;
using Game.Core.Projectiles;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Projectiles.Psycheshock.LinkedShurikens
{
    public class LinkedShurikenProjectile : PredictableProjectile
    {
        [Header("Objects")]
        public Transform visualToRotate;
        public AudioSource flyAudioSource;
        public AudioSource collideAudioSource;

        [Header("Properties")]
        public float flySpeed;
        public float maxLifetime;
        public float flyAudioCenterPitch;
        public float visualRotationFactor;
        public UnityEvent<LinkedShurikenProjectile> onDestroy = new();

        [HideInInspector, SyncVar] public float collideAudioPitch;

        public override ProjectilePredictionData Predict(float timePassed)
        {
            var velocity = flySpeed * transform.forward;
            var predictedPos = spawnPosition + velocity * timePassed;

            return new()
            {
                position = predictedPos,
                rotation = spawnRotation,
                velocity = velocity,
            };
        }

        protected override void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
            var bounds = collision.Collider.bounds;
            var offset = new Vector3
            (
                normal.x * bounds.extents.x,
                normal.y * bounds.extents.y,
                normal.z * bounds.extents.z
            );
            transform.position = point + offset;

            var newVelocity = Vector3.zero;
            var closestDistance = 2000f;
            foreach (var (_, player) in MapLoader.loadedMap.players)
            {
                if (player.dead || player == author) continue;
                var distance = Vector3.Distance(player.transform.position, transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    newVelocity = 0.25f * flySpeed * (player.transform.position - transform.position).normalized;
                }
            }

            rb.linearVelocity = newVelocity;
            if (newVelocity == Vector3.zero)
            {
                var dot = Vector3.Dot(transform.forward, normal);
                if (dot > -0.5f && dot < 0.5f)
                {
                    transform.forward = (transform.forward - normal) / 2f;
                }
            }
            else rb.rotation = Quaternion.LookRotation(newVelocity);

            RpcPlayCollisionAudio();
        }

        [ClientRpc]
        public void RpcPlayCollisionAudio()
        {
            collideAudioSource.pitch = collideAudioPitch;
            collideAudioSource.Play();
        }

        protected override void OnUpdate()
        {
            visualToRotate.Rotate(Vector3.up, rb.linearVelocity.magnitude * visualRotationFactor * Time.deltaTime);
            flyAudioSource.pitch = rb.linearVelocity.magnitude / flySpeed * flyAudioCenterPitch;
            if (!NetworkServer.active) return;
            if (lifetime >= maxLifetime) DestroyProjectile();
        }

        private void OnDestroy()
        {
            if (!NetworkServer.active) return;
            onDestroy.Invoke(this);
        }
    }
}