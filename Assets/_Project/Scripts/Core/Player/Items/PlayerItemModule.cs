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

        public PlayerItemData ItemData
        {
            get => _itemData;
            set
            {
                if (!NetworkServer.active) return;
                _itemData = value;

                DestroyItem();
                if (value.itemIndex != -1)
                {
                    Item = InstantiateItem(value);
                    onItemPickup.Invoke();
                }
            }
        }

        [SyncVar(hook = nameof(OnItemChange))] private PlayerItemData _itemData;
        public Item Item { get; private set; }

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
            ItemData = PlayerItemData.Empty();
        }

        public override void OnStartServer()
        {
            ResetItem();
        }

        [Server]
        public void SetItem(ItemConfig item, params ItemArgument[] args)
        {
            var rarityIdx = Array.IndexOf(ItemPool.rarities, item.rarity);
            ItemData = new()
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

            ItemData = new()
            {
                rarityIndex = rarityIdx,
                itemIndex = itemIdx,
                arguments = Array.Empty<ItemArgument>()
            };
        }

        private void OnItemChange(PlayerItemData old, PlayerItemData _new)
        {
            if (NetworkServer.active) return;

            DestroyItem();
            if (_new.itemIndex != -1)
            {
                Item = InstantiateItem(_new);
                onItemPickup.Invoke();
            }
        }

        private void DestroyItem()
        {
            if (!Item) return;

            onDestroyItem.Invoke();
            Destroy(Item.gameObject);
        }

        private Item InstantiateItem(PlayerItemData itemData)
        {
            var item = Instantiate(ItemPool.items[itemData.rarityIndex][itemData.itemIndex].prefab, itemHolder).GetComponent<Item>();
            item.arguments = itemData.arguments;
            item.transform.localPosition = item.offset;

            return item;
        }

        public void TryUseItem(bool secondary)
        {
            if (!Item || player.locks.Locked(PlayerLock.Input)) return;

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
            if (!Item || player.locks.Locked(PlayerLock.Input)) return;

            var options = Item.Use(player, context);
            if (options.reset) ResetItem();
            if (options.activity) player.stats.activity++;
            if (options.events) InvokeItemUseEvents(options.reset);
        }

        [Server]
        public void InvokeItemUseEvents(bool reset)
        {
            onItemUsed.Invoke(reset);
            EventBus<OnItemUsed>.Invoke(new()
            {
                player = player,
                reset = reset
            });

            RpcInvokeItemUseEvents(reset);
        }

        [ClientRpc]
        private void RpcInvokeItemUseEvents(bool reset)
        {
            if (NetworkServer.active) return;

            onItemUsed.Invoke(reset);
            EventBus<OnItemUsed>.Invoke(new()
            {
                player = player,
                reset = reset
            });
        }
    }
}