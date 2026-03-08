using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Player
{
    public enum PlayerLock
    {
        Input,
        Motor,
        Health,
        Hit,
        Force
    }

    public class PlayerLocks : NetworkBehaviour
    {
        private enum Modification
        {
            Lock,
            Unlock,
            Drop
        }

        public static readonly PlayerLock[] all = new[]
        {
            PlayerLock.Input,
            PlayerLock.Motor,
            PlayerLock.Health,
            PlayerLock.Hit,
            PlayerLock.Force
        };

        [Header("Objects")]
        public PlayerBase player;
        public UnityEvent<PlayerLock, bool> onLockStateChange = new();

        private Dictionary<PlayerLock, int> _locks;

        private void Awake()
        {
            _locks = new();
        }

        public bool Locked(PlayerLock plock)
        {
            return _locks.ContainsKey(plock);
        }

        [Server]
        public void Lock(params PlayerLock[] locks) => Modify(Modification.Lock, locks);

        [Server]
        public void Unlock(params PlayerLock[] locks) => Modify(Modification.Unlock, locks);

        [Server]
        public void Drop(params PlayerLock[] locks) => Modify(Modification.Drop, locks);

        [Server]
        private void Modify(Modification modification, PlayerLock[] locks)
        {
            ModifyInternal(modification, locks);

            if (isLocalPlayer || netIdentity.connectionToClient == null) return;
            TargetModify(modification, locks);
        }

        [TargetRpc]
        private void TargetModify(Modification modification, PlayerLock[] locks) => ModifyInternal(modification, locks);

        private void ModifyInternal(Modification modification, PlayerLock[] locks)
        {
            Debug.Log($"{modification} on {player.playerName}: {string.Join(", ", locks)}");

            if (modification == Modification.Lock)
            {
                foreach (var plock in locks)
                {
                    if (_locks.TryAdd(plock, 0))
                    {
                        onLockStateChange.Invoke(plock, true);
                        continue;
                    }
                    _locks[plock]++;
                }
            }
            else if (modification == Modification.Unlock)
            {
                foreach (var plock in locks)
                {
                    if (!_locks.TryGetValue(plock, out var currentCounter))
                    {
                        Debug.LogError($"Unlock failed: Player {player.playerName} isn't locked on {plock}");
                        continue;
                    }

                    if (currentCounter <= 0)
                    {
                        _locks.Remove(plock);
                        onLockStateChange.Invoke(plock, false);
                        continue;
                    }

                    _locks[plock]--;
                }
            }
            else if (modification == Modification.Drop)
            {
                foreach (var plock in locks)
                {
                    _locks.Remove(plock);
                    onLockStateChange.Invoke(plock, false);
                }
            }
            else throw new($"Unsupported modification \"{modification}\"");
        }
    }
}