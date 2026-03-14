using System;
using Game.Core.Events;
using Game.Core.Hits;
using Game.Core.Items;
using Game.Core.Player.Items.Events;
using Game.Core.Player.Locks;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Game.Core.Player.Items
{
    [Serializable]
    public struct PlayerItemData
    {
        public int rarityIndex;
        public int itemIndex;
        public ItemArgument[] arguments;

        public static PlayerItemData Empty()
        {
            return new()
            {
                rarityIndex = 0,
                itemIndex = -1,
                arguments = Array.Empty<ItemArgument>()
            };
        }
    }

    [RequireComponent(typeof(PlayerCore))]
    public class PlayerItemModule : HitListener
    {
        public PlayerCore player;
        public Transform itemHolder;

        [Header("Runtime")]
        [SyncVar(hook = nameof(OnItemChange))] public PlayerItemData itemData;
        public Item item;

        [HideInInspector] public UnityEvent onDestroyItem = new();
        [HideInInspector] public UnityEvent onItemPickup = new();
        [HideInInspector] public UnityEvent<bool> onItemUsed = new();

        protected override void OnValidate()
        {
            base.OnValidate();

            if (Application.isPlaying) return;
            player = GetComponent<PlayerCore>();
        }

        [Server]
        public void ResetItem()
        {
            itemData = PlayerItemData.Empty();
        }

        public override void OnStartServer()
        {
            ResetItem();
        }

        [Server]
        public void SetItem(ItemConfig item, params ItemArgument[] args)
        {
            var rarityIdx = Array.IndexOf(ItemPool.rarities, item.rarity);
            itemData = new()
            {
                rarityIndex = rarityIdx,
                itemIndex = ItemPool.items[rarityIdx].IndexOf(item),
                arguments = args
            };
        }

        [Server]
        public void PickRandomItem()
        {
            int rarityIdx;
            for (rarityIdx = 0; rarityIdx < ItemPool.rarities.Length - 1; rarityIdx++)
            {
                if (Random.value <= 0.6f) break;
            }

            var itemPool = ItemPool.items[rarityIdx];
            while (itemPool == null)
            {
                rarityIdx--;
                if (rarityIdx < 0) break;
                itemPool = ItemPool.items[rarityIdx];
            }

            if (itemPool == null)
            {
                Debug.LogWarning("No valid item pools were found");
                return;
            }

            var itemIdx = Random.Range(0, itemPool.Count);
            Debug.Log($"Picked item {itemPool[itemIdx].displayName} for player {player.Identification.name}");

            itemData = new()
            {
                rarityIndex = rarityIdx,
                itemIndex = itemIdx,
                arguments = Array.Empty<ItemArgument>()
            };
        }

        private void OnItemChange(PlayerItemData old, PlayerItemData _new)
        {
            if (item)
            {
                onDestroyItem.Invoke();
                Destroy(item.gameObject);
            }

            // TODO: cant be used on server only
            if (_new.itemIndex != -1)
            {
                item = Instantiate(ItemPool.items[_new.rarityIndex][_new.itemIndex].prefab, itemHolder).GetComponent<Item>();
                item.arguments = _new.arguments;
                item.transform.localPosition = item.offset;

                onItemPickup.Invoke();
            }
        }

        public void TryUseItem(bool secondary)
        {
            if (itemData.itemIndex == -1 || player.locks.Locked(PlayerLock.Input)) return;

            var ctx = new ItemUseClientContext()
            {
                headPosition = player.verticalOrientation.position,
                headRotation = player.verticalOrientation.rotation,
                useTime = NetworkTime.time,
                velocity = player.movementModule.LocalTransientVelocity,
                secondary = secondary
            };

            if (NetworkServer.active) UseItem(ctx);
            else CmdUseItem(ctx);
        }

        [Command]
        private void CmdUseItem(ItemUseClientContext context)
        {
            // TODO: Validate context
            UseItem(context);
        }

        [Server]
        private void UseItem(ItemUseClientContext context)
        {
            if (!item || player.locks.Locked(PlayerLock.Input)) return;

            var fullyUsed = item.Use(player, context);
            if (fullyUsed)
            {
                ResetItem();
                player.stats.activity++;
            }

            onItemUsed.Invoke(fullyUsed);
            EventBus<OnItemUsed>.Invoke(new()
            {
                player = player,
                fullyUsed = fullyUsed
            });

            RpcOnItemUsed(fullyUsed);
        }

        [ClientRpc]
        private void RpcOnItemUsed(bool fullyUsed)
        {
            if (NetworkServer.active) return;

            onItemUsed.Invoke(fullyUsed);
            EventBus<OnItemUsed>.Invoke(new()
            {
                player = player,
                fullyUsed = fullyUsed
            });
        }
    }
}