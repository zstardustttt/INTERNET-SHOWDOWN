using System;
using Game.Core.Hits;
using Game.Core.Items;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Game.Player
{
    [Serializable]
    public struct PlayerItemData
    {
        public int rarityIndex;
        public int itemIndex;
        public ItemArgument[] arguments;

        public static PlayerItemData Default()
        {
            return new()
            {
                rarityIndex = 0,
                itemIndex = -1,
                arguments = Array.Empty<ItemArgument>()
            };
        }
    }

    public class PlayerItemModule : HitListener
    {
        [Header("Objects")]
        public PlayerBase player;
        public Transform itemHolder;

        [Header("Runtime")]
        [SyncVar(hook = nameof(OnItemChange))] public PlayerItemData itemData;
        public Item item;

        [HideInInspector] public UnityEvent onItemPickup = new();

        private void Update()
        {
            if (!isLocalPlayer || !item) return;
            item.transform.localPosition = Vector3.Lerp(item.transform.localPosition, item.offset, Time.deltaTime * 15f);
            itemHolder.localScale = Vector3.Lerp(itemHolder.localScale, Vector3.one, Time.deltaTime * 30f);
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
            Debug.Log($"Picked item {itemPool[itemIdx].displayName} for player {player.playerName}");

            itemData = new()
            {
                rarityIndex = rarityIdx,
                itemIndex = itemIdx,
                arguments = Array.Empty<ItemArgument>()
            };
        }

        private void OnItemChange(PlayerItemData old, PlayerItemData _new)
        {
            if (item) Destroy(item.gameObject);

            if (_new.itemIndex != -1)
            {
                item = Instantiate(ItemPool.items[_new.rarityIndex][_new.itemIndex].prefab, itemHolder).GetComponent<Item>();
                item.arguments = _new.arguments;

                if (isLocalPlayer)
                {
                    var layer = LayerMask.NameToLayer("ItemVisual");
                    var children = item.GetComponentsInChildren<Transform>(includeInactive: true);
                    foreach (var child in children)
                    {
                        child.gameObject.layer = layer;
                    }

                    item.transform.localPosition = new(item.offset.x, item.offset.y, -Mathf.Abs(player.verticalOrientation.position.z - itemHolder.position.z));
                    itemHolder.localScale = new(0.1f, 4f, 0.1f);
                }
                else item.transform.localPosition = item.offset;

                onItemPickup.Invoke();
            }
        }

        public void TryUseItem(bool secondary)
        {
            if (itemData.itemIndex == -1 || player.inputLocks != 0 || player.dead) return;

            var ctx = new ItemUseClientContext()
            {
                visualPosition = item.transform.position,
                visualRotation = item.transform.rotation,
                headPosition = player.verticalOrientation.position,
                headRotation = player.verticalOrientation.rotation,
                useTime = NetworkTime.time,
                velocity = player.localTransientVelocity,
                secondary = secondary
            };

            if (NetworkServer.active) UseItem(ctx);
            else CmdUseItem(ctx);
        }

        [Command]
        private void CmdUseItem(ItemUseClientContext context)
        {
            if (player.dead) return;

            // TODO: Validate context
            UseItem(context);
        }

        [Server]
        private void UseItem(ItemUseClientContext context)
        {
            if (!item || player.inputLocks != 0) return;

            if (item.Use(player, context))
            {
                itemData = PlayerItemData.Default();
                player.stats.activity++;
            }
            else TargetRestartItemAnimation();
        }

        [TargetRpc]
        private void TargetRestartItemAnimation()
        {
            if (!item) return;
            item.transform.localPosition = new(item.offset.x, item.offset.y, -Mathf.Abs(player.verticalOrientation.position.z - itemHolder.position.z));
            itemHolder.localScale = new(0.1f, 4f, 0.1f);
        }
    }
}