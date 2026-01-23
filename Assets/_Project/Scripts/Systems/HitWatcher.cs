using UnityEngine;
using System.Collections.Generic;
using Game.Core.Events;
using Game.Events.HitWatcher;
using System;
using Game.Core.Damage;

namespace Game.Systems
{
    public class HitWatcher : MonoBehaviour
    {
        public LayerMask playerDealerCheckLayerMask;
        public float castMargin;
        public CapsuleCollider playerPrefabCollider;

        private Dictionary<Guid, DamageDealer> _dealers;
        private List<DamageDealer> _dealersToAdd;
        private List<Guid> _dealersToRemove;

        private Dictionary<Guid, DamageReceiver> _receivers;
        private List<DamageReceiver> _receiversToAdd;
        private List<Guid> _receiversToRemove;

        private GameObject _receiverCheckGameObject;
        private Collider _receiverCheckCollider;

        public void Awake()
        {
            _dealers = new();
            _dealersToAdd = new();
            _dealersToRemove = new();
            EventBus<OnDamageDealerCreate>.Listen((data) => _dealersToAdd.Add(data.dealer));
            EventBus<OnDamageDealerDestroy>.Listen((data) => _dealersToRemove.Add(data.guid));

            _receivers = new();
            _receiversToAdd = new();
            _receiversToRemove = new();
            EventBus<OnDamageReceiverRegister>.Listen((data) => _receiversToAdd.Add(data.receiver));
            EventBus<OnDamageReceiverUnregister>.Listen((data) => _receiversToRemove.Add(data.guid));

            _receiverCheckGameObject = new GameObject("Receiver Check")
            {
                layer = LayerMask.NameToLayer("ReceiverCheck")
            };

            EventBus<RequestTwoPointsDealerCheck>.Listen((data) => TwoPointsDealerCheck(data.dealer, data.point1, data.point2));
        }

        // Insanity
        // Previously was implemented with more cleaner custom Utility.CopyComponent but that ate alot of frames
        private void ReplaceReceiverCheckCollider(DamageReceiver receiver)
        {
            _receiverCheckGameObject.transform.rotation = receiver.transform.rotation;
            _receiverCheckGameObject.transform.localScale = receiver.transform.lossyScale;

            if (receiver.coll is CapsuleCollider cc)
            {
                if (_receiverCheckCollider is not CapsuleCollider rcc)
                {
                    Destroy(_receiverCheckCollider);
                    rcc = _receiverCheckGameObject.AddComponent<CapsuleCollider>();
                }

                rcc.center = cc.center;
                rcc.radius = cc.radius;
                rcc.height = cc.height;

                _receiverCheckCollider = rcc;
            }
            else if (receiver.coll is BoxCollider bc)
            {
                if (_receiverCheckCollider is not BoxCollider rbc)
                {
                    Destroy(_receiverCheckCollider);
                    rbc = _receiverCheckGameObject.AddComponent<BoxCollider>();
                }

                rbc.center = bc.center;
                rbc.size = bc.size;

                _receiverCheckCollider = rbc;
            }
            else if (receiver.coll is SphereCollider sc)
            {
                if (_receiverCheckCollider is not SphereCollider rsc)
                {
                    Destroy(_receiverCheckCollider);
                    rsc = _receiverCheckGameObject.AddComponent<SphereCollider>();
                }

                rsc.center = sc.center;
                rsc.radius = sc.radius;

                _receiverCheckCollider = rsc;
            }
        }

        private void Update()
        {
            foreach (var dealer in _dealersToAdd)
            {
                if (!dealer) continue;
                _dealers.Add(dealer.DealerGuid, dealer);
            }
            _dealersToAdd.Clear();

            foreach (var receiver in _receiversToAdd)
            {
                if (!receiver) continue;
                _receivers.Add(receiver.Guid, receiver);
            }
            _receiversToAdd.Clear();

            foreach (var (_, receiver) in _receivers)
            {
                if (!receiver) continue;
                receiver.observedDelta = receiver.transform.position - receiver.previousObservedPosition;

                if (receiver.active)
                {
                    ReplaceReceiverCheckCollider(receiver);

                    foreach (var (_, dealer) in _dealers)
                    {
                        if (!dealer) continue;
                        dealer.observedDelta = dealer.transform.position - dealer.previousObservedPosition;

                        if (!dealer.active) continue;
                        if (dealer.singleHitScan && dealer.hitScanCount > 0) continue;
                        ReceiverDealerCheck(receiver, dealer, receiver.previousObservedPosition, receiver.observedDelta, dealer.previousObservedPosition, dealer.observedDelta);
                    }
                }

                receiver.previousObservedPosition = receiver.transform.position;
            }

            foreach (var guid in _dealersToRemove)
            {
                _dealers.Remove(guid);
            }
            _dealersToRemove.Clear();

            foreach (var guid in _receiversToRemove)
            {
                _receivers.Remove(guid);
            }
            _receiversToRemove.Clear();

            foreach (var (_, dealer) in _dealers)
            {
                dealer.previousObservedPosition = dealer.transform.position;
                if (dealer.singleHitScan && dealer.hitScanCount > 0) continue;
                dealer.hitScanCount++;
            }
        }

        public void TwoPointsDealerCheck(DamageDealer dealer, Vector3 point1, Vector3 point2)
        {
            if (!dealer.active) return;

            foreach (var (_, receiver) in _receivers)
            {
                if (!receiver || !receiver.active) continue;
                ReplaceReceiverCheckCollider(receiver);
                ReceiverDealerCheck(receiver, dealer, receiver.previousObservedPosition, receiver.observedDelta, point1, point2 - point1);
            }
        }

        public void ReceiverDealerCheck(DamageReceiver player, DamageDealer dealer, Vector3 receiverPosition, Vector3 receiverDelta, Vector3 dealerPosition, Vector3 dealerDelta)
        {
            var relativePosition = dealerPosition - receiverPosition;
            var relativeDelta = dealerDelta - receiverDelta;

            var deltaLength = relativeDelta.magnitude + castMargin;

            bool didHit;
            RaycastHit hit;
            if (dealer.coll is BoxCollider bc)
            {
                var halfExtents = bc.size / 2f;
                var rotation = bc.transform.rotation;
                didHit = Physics.BoxCast(relativePosition, halfExtents, relativeDelta.normalized, out hit, rotation, deltaLength, playerDealerCheckLayerMask)
                || Physics.CheckBox(relativePosition, halfExtents, rotation, playerDealerCheckLayerMask);
            }
            else if (dealer.coll is SphereCollider sc)
                didHit = Physics.SphereCast(relativePosition, sc.radius, relativeDelta.normalized, out hit, deltaLength, playerDealerCheckLayerMask)
                || Physics.CheckSphere(relativePosition, sc.radius, playerDealerCheckLayerMask);
            else
            {
                Debug.LogError("Collider not supported");
                return;
            }

            // TODO: observed position might be broken if setting dealer's position manually
            if (!didHit) return;

            var hitPoint = Vector3.Lerp(dealerPosition, dealerPosition + dealerDelta, hit.distance / deltaLength);
            RegisterHit(player, dealer, hitPoint);
        }

        private void RegisterHit(DamageReceiver receiver, DamageDealer dealer, Vector3 point)
        {
            var damage = dealer.damageType == DamageType.None ? 0f : dealer.EvaluateDamage(receiver);
            receiver.onDamage.Invoke(dealer, damage);
            dealer.onHit.Invoke(receiver, damage);

            EventBus<OnRegisterHit>.Invoke(new() { dealer = dealer, receiver = receiver });
            Debug.Log($"{dealer.damageType} hit! on: {receiver.gameObject.name} by: {dealer.owner.gameObject.name} at: {point} damage: {damage}");
        }
    }
}