using System.Collections.Generic;
using System.Linq;
using Game.Core.Events;
using Game.Core.Maps;
using Game.Player;
using Game.Player.Events;
using UnityEngine;

namespace Game.Systems
{
    public class PlayerDeathWatcher : MonoBehaviour
    {
        public float respawnDuration;
        public float respawnInvincibilityDuration;

        private void Awake()
        {
            EventBus<OnPlayerHealthChanged>.Listen(OnPlayerHealthChanged);
        }

        private void Update()
        {
            if (MapLoader.loadedMap == null) return;

            foreach (var (_, player) in MapLoader.loadedMap.players)
            {
                if (!player.dead) continue;

                if (player.respawnTimer <= 0f)
                {
                    RespawnPlayer(player);
                    continue;
                }

                player.respawnTimer -= Time.deltaTime;
            }
        }

        private void OnPlayerHealthChanged(OnPlayerHealthChanged data)
        {
            if (data.newHealth <= 0f) RegisterDeath(data.healthModule.player);
        }

        public void RespawnPlayer(PlayerBase player)
        {
            if (!player.dead) return;
            player.dead = false;
            player.respawnTimer = 0f;

            var position = MapLoader.IsPlayerOnMap(player) ?
                    MapLoader.loadedMap.info.spawnPoints[Random.Range(0, MapLoader.loadedMap.info.spawnPoints.Length)].position :
                    Vector3.zero;

            player.ServerMovePlayer(position);
            player.ResetPlayer();
            player.healthModule.ActivateInvincibility(respawnInvincibilityDuration);
            player.motorLocks--;
            player.hitEntity.active = true;
        }

        public void RegisterDeath(PlayerBase player)
        {
            if (player.dead) return;
            player.dead = true;
            player.respawnTimer = respawnDuration;

            player.hitEntity.active = false;
            player.itemModule.itemData = PlayerItemData.Default();
            player.motorLocks++;

            var damages = new Dictionary<PlayerBase, float>();
            foreach (var damage in player.healthModule.damageHistory)
            {
                if (!damage.author) continue;

                if (damages.TryAdd(damage.author, damage.amount)) continue;
                damages[damage.author] += damage.amount;
            }

            PlayerBase supporter = null;
            if (damages.Count != 0)
            {
                var sortedDamages = damages.OrderByDescending(x => x.Value).ToArray();
                supporter = sortedDamages[0].Key;
            }

            var finishingDamage = player.healthModule.damageHistory.Peek();
            var killer = finishingDamage.author;

            if (killer && killer != player && killer == supporter)
            {
                killer.stats.pureKills++;
                killer.TargetOnKill(true);
                killer.healthModule.Heal(damages[killer] / 2f);
            }
            else
            {
                if (killer && killer != player)
                {
                    killer.stats.finishingKills++;
                    killer.TargetOnKill(false);
                    killer.healthModule.Heal(damages[killer] / 2f);
                }

                if (supporter && supporter != player)
                {
                    supporter.stats.supportingKills++;
                    supporter.TargetOnKill(false);
                    supporter.healthModule.Heal(damages[supporter] / 2f);
                }
            }
        }
    }
}