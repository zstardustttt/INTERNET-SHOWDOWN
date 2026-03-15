using Game.Boxes.Events;
using Game.Core.Events;
using Game.Core.Hits;
using Game.Core.Hits.Events;
using Game.Core.Maps;
using Game.Core.Player;
using Game.Core.Player.Items;
using Game.Core.Player.Items.Events;
using Game.GameLoop;
using Game.GameLoop.Events;
using Mirror;
using UnityEngine;

namespace Game.Boxes
{
    // This object is only active on the server
    public class BoxHandler : MonoBehaviour
    {
        public BoxSpawnBakeData bakeData;
        public GameObject boxPrefab;
        public HitLayer boxesLayer;

        [Header("Ambient Spawning")]
        public float ambientSpawnRate;
        public int maxBoxesPerPlayer;

        [Header("Need Spawning")]
        public float randomOffsetFromPlayerAmplitude;

        private bool _active;
        private float _timer;
        private int _spawnedBoxesCounter;

        private void Awake()
        {
            if (!NetworkServer.active) return;

            EventBus<OnGameStateChange>.Listen((data) =>
            {
                _timer = 0f;
                _active = data.state.phase.type == GamePhaseType.Match;
            });

            EventBus<OnBoxSpawn>.Listen((_) => _spawnedBoxesCounter++);
            EventBus<OnBoxDestroy>.Listen((_) => _spawnedBoxesCounter--);

            EventBus<HitEvent>.Listen(OnHit);

            EventBus<OnItemUsed>.Listen((data) =>
            {
                if (!data.reset) return;
                OnItemUsed(data.player);
            });
        }

        private void OnHit(HitEvent hitEvent)
        {
            if (hitEvent.source.layer != boxesLayer) return;
            if (hitEvent.target is not PlayerItemModule playerItemModule) return;

            if (playerItemModule.ItemData.itemIndex == -1)
            {
                playerItemModule.PickRandomItem();
                NetworkServer.Destroy(hitEvent.source.gameObject);
            }
        }

        private void OnItemUsed(PlayerCore player)
        {
            var randomInsideCircle = Random.insideUnitCircle * randomOffsetFromPlayerAmplitude;
            var offset = new Vector3(randomInsideCircle.x, 0f, randomInsideCircle.y);
            var position = bakeData.GetClosestSpawnPosition(player.hitEntity.Collider.bounds.center + offset);
            MapLoader.NetworkSpawnOnMap(boxPrefab, position, Quaternion.identity);
        }

        private void Update()
        {
            if (!_active) return;

            if (MapLoader.loadedMap == null || !MapLoader.loadedMap.scene.IsValid())
            {
                Debug.LogWarning("Box spawner cant function without a loaded map");
                _active = false;
                return;
            }

            HandleBoxSpawning();
        }

        private void HandleBoxSpawning()
        {
            var playerCount = MapLoader.loadedMap.players.Count;
            if (_spawnedBoxesCounter >= maxBoxesPerPlayer * playerCount) return;

            if (_timer <= 0f)
            {
                _timer = 1f / (ambientSpawnRate * playerCount);
                var position = bakeData.GetRandomSpawnPosition();
                MapLoader.NetworkSpawnOnMap(boxPrefab, position, Quaternion.identity);
            }
            else _timer -= Time.deltaTime;
        }
    }
}