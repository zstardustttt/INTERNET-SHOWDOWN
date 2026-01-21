using System.Collections.Generic;
using System.Linq;
using Game.Core.Events;
using Game.Core.Items;
using Game.Core.Maps;
using Game.Core.Projectiles;
using Game.Events.Player;
using Game.Events.UI;
using Game.Maps;
using KinematicCharacterController;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

using Random = UnityEngine.Random;

namespace Game.Player
{
    [RequireComponent(typeof(KinematicCharacterMotor))]
    public class PlayerBase : NetworkBehaviour, ICharacterController
    {
        public Bounds WorldHitbox => new(transform.position + config.hitbox.center, config.hitbox.size);

        public PlayerConfig config;
        public IPlayerController controller;
        public KinematicCharacterMotor motor;
        public Transform horizontalOrientation;
        public Transform verticalOrientation;
        public Transform itemHolder;

        // movement
        private float _targetSpeed;
        private Vector2 _prevMoveInput;
        private Vector2 _fromMoveInput;
        private Vector2 _targetMoveInput;
        private float _elapsedFromMoveInputChange;
        private float _movementTime;
        private float _idleTime;

        // jumping
        private bool _jumping;
        private float _jumpTimer;
        private float _currentJumpHeight;
        private bool _endingJump;
        private float _jumpEndTimer;
        private float _releaseY;
        private float _endJumpHeight;
        private float _jumpEndFalloffValue;
        private float _coyoteTimer;
        private float _bufferTimer;
        private bool _prevWishJumping;

        // dash
        private bool _dashing;
        private bool _canDash;
        private float _dashTimer;
        private Vector3 _dashStartPos;
        private Vector3 _dashDirection;
        private float _dashCooldownTimer;
        private float _dashBufferTimer;
        private bool _prevWishDashing;

        // ground slam
        private bool _groundSlamming;
        private float _groundSlamForce;
        private bool _canGroundSlam;
        private float _groundSlamDistance;

        // wall running
        private bool _walled;
        private bool _prevWalled;
        private RaycastHit _wallHitInfo;
        private bool _jumpingFromGround;

        // other
        public PlayerInputs inputs;
        private Vector3 _additionalVelocity;
        private float _gravityVelocity;

        [HideInInspector] public UnityEvent<float> onGroundSlamLanded = new();
        [HideInInspector] public UnityEvent onJump = new();
        [HideInInspector] public UnityEvent<Vector3> onWalled = new();
        [HideInInspector] public UnityEvent onUnwalled = new();
        [HideInInspector] public UnityEvent onDash = new();
        [HideInInspector] public UnityEvent<bool> onEndDash = new();
        [HideInInspector] public UnityEvent<Collider> onCollide = new();
        [HideInInspector] public UnityEvent onItemPickup = new();
        [HideInInspector] public UnityEvent onResetPlayer = new();
        [HideInInspector] public UnityEvent onDamage = new();
        [HideInInspector] public UnityEvent onDeath = new();

        public struct ItemData
        {
            public int rarityIndex;
            public int itemIndex;

            public static ItemData Default()
            {
                return new()
                {
                    rarityIndex = 0,
                    itemIndex = -1
                };
            }
        }

        [SyncVar(hook = nameof(OnItemChange))] public ItemData itemData;
        private Item _item;

        public struct DamageCapture
        {
            public PlayerBase author;
            public float damage;
        }
        [SyncVar(hook = nameof(OnHealthChange))] public float health;
        private Stack<DamageCapture> _damageHistory;

        [SyncVar] public bool invincible;
        private float _invincibleTimer; // server only

        [HideInInspector] public Vector3 previousObservedPosition;
        [HideInInspector] public Vector3 observedDelta;

        private bool _guidReceived;
        [SyncVar] public string playerGuid;
        [SyncVar] public string playerName;

        private PlayerStats _prevStats;
        [SyncVar] public PlayerStats stats;

        [Server]
        public void SelectItem()
        {
            int rarityIdx;
            for (rarityIdx = 0; rarityIdx < ItemPool.rarities.Length - 1; rarityIdx++)
            {
                if (Random.value <= 0.6f) break;
            }

            Debug.Log($"Picked rarity {rarityIdx} for player {gameObject.name}");

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
            Debug.Log($"Picked item index {itemIdx} for player {gameObject.name}");

            itemData = new()
            {
                rarityIndex = rarityIdx,
                itemIndex = itemIdx
            };
        }

        private void InnerSetGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid) || _guidReceived) return;
            playerGuid = guid;
            _guidReceived = true;
        }

        [Command]
        private void CmdSetGUID(string guid)
        {
            InnerSetGuid(guid);
        }

        public void SetGUID(string guid)
        {
            if (NetworkServer.active) InnerSetGuid(guid);
            else CmdSetGUID(guid);
        }

        [Command]
        private void CmdSetPlayerName(string name)
        {
            playerName = name;
        }

        public void SetPlayerName(string name)
        {
            if (NetworkServer.active) playerName = name;
            else CmdSetPlayerName(name);
        }

        [Server]
        public void ServerMovePlayer(Vector3 position)
        {
            TargetMovePlayer(position);
        }

        [TargetRpc]
        private void TargetMovePlayer(Vector3 position)
        {
            SetPosition(position);
        }

        [Server]
        public void ResetPlayer()
        {
            _damageHistory.Clear();
            itemData = ItemData.Default();
            health = config.maxHealth;
            onResetPlayer.Invoke();
        }

        [Server]
        public void ResetStats()
        {
            stats = default;
        }

        public override void OnStartServer()
        {
            _damageHistory = new();
            ResetPlayer();
        }

        private void OnItemChange(ItemData old, ItemData _new)
        {
            if (_item) Destroy(_item.gameObject);

            if (_new.itemIndex != -1)
            {
                _item = Instantiate(ItemPool.items[_new.rarityIndex][_new.itemIndex].prefab, itemHolder).GetComponent<Item>();
                _item.transform.localPosition = _item.offset;
                if (isLocalPlayer)
                {
                    var layer = LayerMask.NameToLayer("ItemVisual");
                    var children = _item.GetComponentsInChildren<Transform>(includeInactive: true);
                    foreach (var child in children)
                    {
                        child.gameObject.layer = layer;
                    }
                }

                onItemPickup.Invoke();
            }
        }

        [Server]
        public void Damage(float damage, PlayerBase author)
        {
            _damageHistory.Push(new() { damage = damage, author = author });
            health -= damage;
            onDamage.Invoke();
        }

        [Server]
        public void Heal(float value)
        {
            var targetHealth = Mathf.Clamp(health + value, 0f, config.maxHealth);
            var difference = targetHealth - health;

            var differenceCounter = 0f;
            while (_damageHistory.Count > 0)
            {
                var capture = _damageHistory.Pop();
                differenceCounter += capture.damage;

                if (differenceCounter <= difference) continue;
                _damageHistory.Push(new() { author = capture.author, damage = differenceCounter - difference });
                break;
            }

            health = targetHealth;
        }

        private void OnHealthChange(float old, float _new)
        {
            if (NetworkServer.active && _new < old)
                _invincibleTimer = config.invincibleDuration;

            if (NetworkServer.active && _new <= 0f)
            {
                // Death logic
                var killer = _damageHistory.Peek().author;
                var damages = new Dictionary<PlayerBase, float>();
                foreach (var capture in _damageHistory)
                {
                    if (!capture.author) continue;

                    if (damages.TryAdd(capture.author, capture.damage)) continue;
                    damages[capture.author] += capture.damage;
                }

                PlayerBase supporter = null;
                if (damages.Count != 0)
                {
                    var sortedDamages = damages.OrderByDescending(x => x.Value).ToArray();
                    supporter = sortedDamages[0].Key;
                }

                if (killer && killer != this && killer == supporter)
                {
                    killer.stats.pureKills++;
                    killer.TargetOnKill(true);
                    killer.Heal(damages[killer] / 2f);
                }
                else
                {
                    if (killer && killer != this)
                    {
                        killer.stats.finishingKills++;
                        killer.TargetOnKill(false);
                        killer.Heal(damages[killer] / 2f);
                    }

                    if (supporter && supporter != this)
                    {
                        supporter.stats.supportingKills++;
                        supporter.TargetOnKill(false);
                        killer.Heal(damages[supporter] / 2f);
                    }
                }

                var position = MapLoader.loadedMap.info.spawnPoints[Random.Range(0, MapLoader.loadedMap.info.spawnPoints.Length)].position;
                ServerMovePlayer(position);
                ResetPlayer();

                onDeath.Invoke();
            }

            if (netIdentity.isLocalPlayer)
            {
                EventBus<OnHealthUpdate>.Invoke(new() { maxHealth = config.maxHealth, health = health });
                if (_new < old) EventBus<DamageIndicatorRequest>.Invoke(new());
                if (_new <= 0f) _additionalVelocity = Vector3.zero;
            }
        }

        public void TryUseItem()
        {
            if (itemData.itemIndex == -1) return;

            var crosshairHit = Physics.Raycast(verticalOrientation.position, verticalOrientation.forward, out var hitInfo, 1000f, LayerMask.GetMask("Player", "Enviroment"));
            var ctx = new ItemUseClientContext()
            {
                visualPosition = _item.transform.position,
                visualRotation = _item.transform.rotation,
                headPosition = verticalOrientation.position,
                headRotation = verticalOrientation.rotation,
                crosshairHitPoint = hitInfo.point,
                crosshairHit = crosshairHit,
                useTime = NetworkTime.time,
            };

            if (NetworkServer.active) UseItem(ctx);
            else CmdUseItem(ctx);
        }

        private void Update()
        {
            if (!NetworkServer.active) return;

            // Handle invincibility
            _invincibleTimer -= Time.deltaTime;
            if (_invincibleTimer > 0f && !invincible)
                invincible = true;
            else if (_invincibleTimer <= 0f && invincible)
                invincible = false;

            // Teleport back if clipped out of bounds
            if (MapLoader.loadedMap != null && transform.position.y < MapLoader.loadedMap.info.boundsMin.y)
            {
                var position = MapLoader.loadedMap.info.spawnPoints[Random.Range(0, MapLoader.loadedMap.info.spawnPoints.Length)].position;
                ServerMovePlayer(position);
            }
            else if (transform.position.y < -50f)
            {
                ServerMovePlayer(Vector3.zero);
            }

            if (!stats.Equals(_prevStats))
            {
                EventBus<OnStatsChanged>.Invoke(new() { player = this });
            }
            _prevStats = stats;
        }

        [Command]
        private void CmdUseItem(ItemUseClientContext context)
        {
            stats.activity++;
            // TODO: Validate context
            UseItem(context);
        }

        private void UseItem(ItemUseClientContext context)
        {
            if (_item) _item.Use(this, context);
            itemData = ItemData.Default();
        }

        public void SetPosition(Vector3 position)
        {
            if (_dashing)
            {
                _dashing = false;
                onEndDash.Invoke(true);
            }

            _dashCooldownTimer = 0f;

            _jumping = false;
            _endingJump = false;
            motor.SetPosition(position);
        }

        private void Awake()
        {
            motor.enabled = false;
        }

        private void OnDestroy()
        {
            EventBus<OnDestroyPlayer>.Invoke(new() { guid = playerGuid });
        }

        public void EnableMotor()
        {
            motor.enabled = true;
            motor.CharacterController = this;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            motor = GetComponent<KinematicCharacterMotor>();
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            var drag = motor.GroundingStatus.IsStableOnGround ? config.groundAdditionalVelocityDrag : config.airAdditionalVelocityDrag;
            _additionalVelocity *= 1f - drag * deltaTime;

            if (new Vector2(_additionalVelocity.x, _additionalVelocity.z).magnitude <= 0.5f)
                _additionalVelocity = Vector3.up * _additionalVelocity.y;
        }

        private void CheckWalled()
        {
            var hit = new RaycastHit();
            _prevWalled = _walled;

            _walled = false;
            if (!motor.GroundingStatus.IsStableOnGround && !_groundSlamming && !_jumpingFromGround)
            {
                var origin = transform.position + Vector3.up * motor.Capsule.height / 2f;
                var maxdist = config.wallDetectionDistance + motor.Capsule.radius;
                for (int i = 0; i < config.wallCheckRayCount; i++)
                {
                    var x = i * Mathf.PI * 2 / config.wallCheckRayCount;
                    var dir = new Vector3(Mathf.Sin(x), 0f, Mathf.Cos(x));
                    if (Physics.Raycast(origin, horizontalOrientation.rotation * dir, out hit, maxdist, config.wallLayers, QueryTriggerInteraction.Ignore))
                    {
                        _walled = true;
                        break;
                    }
                }
            }

            if (_walled) _wallHitInfo = hit;

            if (!_prevWalled && _walled)
            {
                _jumping = false;
                _endingJump = false;

                if (_dashing)
                {
                    _dashing = false;
                    onEndDash.Invoke(true);
                }
                _dashCooldownTimer = 0f;

                onWalled.Invoke(_wallHitInfo.normal);
            }
            else if (_prevWalled && !_walled) onUnwalled.Invoke();
        }

        public void BeforeCharacterUpdate(float deltaTime)
        {
            if (controller == null) return;
            CheckWalled();

            _prevWishJumping = inputs.wishJumping;
            _prevWishDashing = inputs.wishDashing;
            inputs = controller.GetInputs();

            if (!inputs.wishGroundSlam) _canGroundSlam = true;
            if (!motor.GroundingStatus.IsStableOnGround
                && inputs.wishGroundSlam
                && !_groundSlamming
                && _canGroundSlam
                && Physics.Raycast(transform.position, Vector3.down, out var hitInfo, 1000f, motor.StableGroundLayers))
            {
                _jumping = false;
                _endingJump = false;

                if (_dashing)
                {
                    _dashing = false;
                    onEndDash.Invoke(false);
                }

                _groundSlamForce = Mathf.Lerp(config.minGroundSlamForce, config.maxGroundSlamForce, hitInfo.distance / config.groundSlamForceInterpolationDistance);

                _groundSlamming = true;
                _groundSlamDistance = hitInfo.distance;
                _canGroundSlam = false;
                _bufferTimer = config.bufferTime;
            }

            if (inputs.wishDashing && !_prevWishDashing)
            {
                _dashBufferTimer = 0f;
            }
            _dashBufferTimer += deltaTime;

            if (_dashBufferTimer < config.dashBuffer && !_dashing && _canDash && _dashCooldownTimer <= 0f)
            {
                _dashing = true;
                _canDash = false;
                _dashTimer = 0f;
                _dashStartPos = transform.position;

                var playerViewRot = Quaternion.Euler(new(inputs.orientationX, horizontalOrientation.eulerAngles.y, 0f));
                if (_walled)
                {
                    var playerViewDir = playerViewRot * Vector3.forward;
                    var playerViewDirMasked = new Vector3(playerViewDir.x, 0f, playerViewDir.z);
                    if (Vector3.Dot(playerViewDirMasked, _wallHitInfo.normal) > config.higherWallDashDirectionThreshold)
                        _dashDirection = playerViewDir;
                    else if (Vector3.Dot(playerViewDirMasked, _wallHitInfo.normal) < config.lowerWallDashDirectionThreshold)
                        _dashDirection = -playerViewDir;
                    else _dashDirection = _wallHitInfo.normal;
                }
                else
                {
                    var relative = inputs.move.sqrMagnitude == 0 ? Vector3.forward : new Vector3(inputs.move.x, 0f, inputs.move.y);
                    _dashDirection = playerViewRot * relative;

                    if (motor.GroundingStatus.IsStableOnGround)
                    {
                        var dashDirProjected = Vector3.ProjectOnPlane(_dashDirection, motor.GroundingStatus.GroundNormal);
                        _dashDirection = (dashDirProjected + Vector3.up * _dashDirection.y).normalized;
                    }
                }

                _jumping = false;
                _endingJump = false;
                _groundSlamming = false;
                motor.ForceUnground(config.dashDuration);

                _dashBufferTimer = config.dashBuffer;
                onDash.Invoke();
            }

            if (inputs.wishJumping && !_prevWishJumping)
            {
                _bufferTimer = 0f;
            }
            _bufferTimer += deltaTime;

            if (_dashing)
            {
                _coyoteTimer = 0f;
                _dashTimer += deltaTime;
                motor.MoveCharacter(Vector3.Lerp(_dashStartPos, _dashStartPos + _dashDirection * config.dashDistance, _dashTimer / config.dashDuration));

                _dashCooldownTimer = config.dashCooldown;
                return;
            }

            _dashCooldownTimer -= deltaTime;

            if (motor.GroundingStatus.IsStableOnGround || _dashing || _walled)
            {
                _coyoteTimer = 0f;
                if (!inputs.wishDashing) _canDash = true;
            }
            else _coyoteTimer += deltaTime;

            if (_coyoteTimer < config.coyoteTime)
            {
                _jumpTimer = 0f;
                _currentJumpHeight = 0f;

                if (_bufferTimer <= config.bufferTime) BeginJump();
            }

            var currentY = transform.position.y - _currentJumpHeight;
            if (!inputs.wishJumping)
            {
                if (_jumping)
                {
                    _jumping = false;
                    _endingJump = true;
                    _jumpEndTimer = 0f;
                    _jumpEndFalloffValue = config.jumpEndFalloffCurve.Evaluate(1f - _jumpTimer / config.jumpDuration);
                    _endJumpHeight = _currentJumpHeight + (config.jumpCurve.Evaluate(Mathf.Min(_jumpTimer + config.jumpEndDuration, config.jumpDuration) / config.jumpDuration) * config.jumpHeight - _currentJumpHeight) * config.jumpEndMultiplier * _jumpEndFalloffValue;
                    _releaseY = currentY;
                }
            }

            if (_jumping)
            {
                _jumpTimer += deltaTime;
                _currentJumpHeight = config.jumpCurve.Evaluate(Mathf.Min(_jumpTimer, config.jumpDuration) / config.jumpDuration) * config.jumpHeight;
                motor.MoveCharacter(new(transform.position.x, currentY + _currentJumpHeight, transform.position.z));

                if (_jumpTimer >= config.jumpDuration) _jumping = false;
            }
            else
            {
                _jumpingFromGround = false;
                if (_endingJump)
                {
                    motor.MoveCharacter(
                    new(
                        transform.position.x,
                        _releaseY + Mathf.Lerp(_currentJumpHeight, _endJumpHeight, config.jumpEndCurve.Evaluate(_jumpEndTimer / (config.jumpEndDuration * _jumpEndFalloffValue))),
                        transform.position.z
                    ));

                    _jumpEndTimer += deltaTime;
                    if (_jumpEndTimer > config.jumpEndDuration * _jumpEndFalloffValue) _endingJump = false;
                }
            }
        }

        private void BeginJump()
        {
            _coyoteTimer = config.coyoteTime;
            motor.ForceUnground();
            _jumping = true;
            if (!_walled) _jumpingFromGround = true;

            onJump.Invoke();
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            return true;
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {

        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {

        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            onCollide.Invoke(hitCollider);

            if (Vector3.Dot(hitNormal, _dashDirection) < -0.9f && _dashing)
            {
                _dashing = false;
                onEndDash.Invoke(false);
            }

            if (hitNormal.y < 0f)
            {
                _jumping = false;
                _endingJump = false;
            }

            if (hitCollider.TryGetComponent(out JumpPad jumpPad))
            {
                _additionalVelocity.y = jumpPad.jumpForce;
            }
        }

        public void PostGroundingUpdate(float deltaTime)
        {

        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {

        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {

        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_dashing)
            {
                if ((_dashTimer >= config.dashDuration) || (inputs.wishJumping && !_prevWishJumping))
                {
                    _dashing = false;
                    onEndDash.Invoke(false);
                    _additionalVelocity = _dashDirection * (config.dashDistance / config.dashDuration);
                }
            }

            if (_walled)
            {
                if (_jumping) _additionalVelocity = _wallHitInfo.normal * config.wallJumpSpeed;
                else _additionalVelocity = Vector3.zero;
            }

            if (inputs.move != _prevMoveInput)
            {
                _fromMoveInput = _targetMoveInput;
                _elapsedFromMoveInputChange = 0f;
            }
            _prevMoveInput = inputs.move;

            if (inputs.move.sqrMagnitude != 0)
            {
                _idleTime = 0f;

                _movementTime += deltaTime;
                _elapsedFromMoveInputChange += Time.deltaTime;

                var smoothingDuration = motor.GroundingStatus.IsStableOnGround ? config.groundMoveSmoothingDuration : config.airMoveSmoothingDuration;
                _targetMoveInput = Vector2.Lerp
                (
                    _fromMoveInput,
                    inputs.move.normalized,
                    config.moveSmootingCurve.Evaluate(Mathf.Min(_elapsedFromMoveInputChange, smoothingDuration) / smoothingDuration)
                );

                _targetSpeed = config.speed * config.accelerationCurve.Evaluate(Mathf.Min(_movementTime, config.accelerationDuration) / config.accelerationDuration);
            }
            else
            {
                _movementTime = 0f;

                _idleTime += deltaTime;
                _targetMoveInput = Vector2.Lerp
                (
                    _fromMoveInput,
                    Vector2.zero,
                    config.deccelerationCurve.Evaluate(Mathf.Min(_idleTime, config.deccelerationDuration) / config.deccelerationDuration)
                );
            }
            var dir = horizontalOrientation.rotation * new Vector3(_targetMoveInput.x, 0f, _targetMoveInput.y);

            var movementVelocity = dir * _targetSpeed;

            if (Vector3.Dot(new Vector3(_additionalVelocity.x, 0f, _additionalVelocity.z), dir) < 0f)
            {
                _additionalVelocity.x += dir.x;
                _additionalVelocity.z += dir.z;
            }

            currentVelocity = movementVelocity + _additionalVelocity;

            var addvel = _jumping ? new Vector3(_additionalVelocity.x, 0f, _additionalVelocity.z) : _additionalVelocity;

            if (_groundSlamming) currentVelocity = new(currentVelocity.x, _groundSlamForce, currentVelocity.z);
            else if (_walled) currentVelocity = new Vector3(movementVelocity.x, -config.slidingDownSpeed, movementVelocity.z) + addvel;
            else currentVelocity = movementVelocity + addvel + Vector3.up * _gravityVelocity;

            if (motor.GroundingStatus.IsStableOnGround) UpdateVelocityOnGround(ref currentVelocity, deltaTime);
            else UpdateVelocityInAir(ref currentVelocity, deltaTime);
        }

        private void UpdateVelocityOnGround(ref Vector3 currentVelocity, float deltaTime)
        {
            _gravityVelocity = 0f;
            if (_groundSlamming)
            {
                _groundSlamming = false;
                onGroundSlamLanded?.Invoke(_groundSlamDistance);
            }
        }

        private void UpdateVelocityInAir(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_jumping || _endingJump || _dashing || _walled) _gravityVelocity = 0f;
            else if (currentVelocity.y > config.gravityClamp)
            {
                _gravityVelocity += config.gravity * deltaTime;
            }
        }

        [Server]
        public void RegisterHit(DamageType damageType)
        {
            stats.AddHit(damageType);
            TargetOnHit();
        }

        [TargetRpc]
        public void TargetOnHit()
        {
            EventBus<HitIndicatorRequest>.Invoke(new());
        }

        [TargetRpc]
        public void TargetOnKill(bool pure)
        {
            if (pure)
                EventBus<PureKillIndicatorRequest>.Invoke(new());
            else
                EventBus<UnpureKillIndicatorRequest>.Invoke(new());
        }

        [TargetRpc]
        public void TargetKnockback(Vector3 force)
        {
            _additionalVelocity += force;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            var origin = transform.position + Vector3.up * motor.Capsule.height / 2f;
            var distance = config.wallDetectionDistance + motor.Capsule.radius;
            for (int i = 0; i < config.wallCheckRayCount; i++)
            {
                var x = i * Mathf.PI * 2 / config.wallCheckRayCount;
                Gizmos.DrawRay(origin, new Vector3(Mathf.Sin(x), 0f, Mathf.Cos(x)) * distance);
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(config.hitbox.center, config.hitbox.size);
        }
    }
}