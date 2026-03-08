using System.Collections.Generic;
using System.Linq;
using Game.Core.Events;
using Game.Core.Lobby;
using Game.Core.Maps;
using Game.Player.Events;
using UnityEngine;

namespace Game.Player.Death
{
    public enum KillType
    {
        Pure,
        Finishing,
        Supporting
    }

    public class PlayerDeathWatcher : MonoBehaviour
    {
        public LobbyInfo lobbyInfo;
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
                if (!player.deathModule.Dead) continue;

                if (player.deathModule.respawnTimer <= 0f)
                {
                    RespawnPlayer(player);
                    continue;
                }

                player.deathModule.respawnTimer -= Time.deltaTime;
            }
        }

        private void OnPlayerHealthChanged(OnPlayerHealthChanged data)
        {
            if (data.newHealth <= 0f) RegisterDeath(data.healthModule.player);
        }

        public void RespawnPlayer(PlayerBase player)
        {
            var position = MapLoader.IsPlayerOnMap(player) ? MapLoader.loadedMap.info.GetRandomSpawnPoint() : lobbyInfo.spawnArea.RandomSampleArea(Space.World);
            player.ServerMovePlayer(position);
            player.deathModule.previousPosition = position;

            player.deathModule.Respawn();
        }

        public void RegisterDeath(PlayerBase player)
        {
            player.deathModule.Die();
            player.deathModule.respawnTimer = respawnDuration;

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

            if (killer && killer != player)
            {
                killer.healthModule.Heal(damages[killer]);

                if (killer == supporter)
                {
                    killer.ReportKill(KillType.Pure);
                    return;
                }
                else killer.ReportKill(KillType.Finishing);
            }

            if (supporter && supporter != player)
            {
                supporter.healthModule.Heal(damages[supporter]);
                supporter.ReportKill(KillType.Supporting);
            }
        }
    }
}