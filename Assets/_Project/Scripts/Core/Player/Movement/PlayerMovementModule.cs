using Game.Core.Player.InteractionObjects;
using Game.Core.Player.Locks;
using KinematicCharacterController;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Core.Player.Movement
{
    public struct PlayerMovementInputs
    {
        public Vector2 move;
        public bool wishJumping;
        public bool wishDashing;
        public bool wishGroundSlam;
        public float orientationX;
    }

    public interface IPlayerMovementController
    {
        public abstract PlayerMovementInputs GetInputs();
    }

    [RequireComponent(typeof(PlayerCore), typeof(KinematicCharacterMotor))]
    public class PlayerMovementModule : NetworkBehaviour, ICharacterController
    {
        public PlayerMovementConfig config;
        public IPlayerMovementController controller;
        public PlayerCore player;
        public KinematicCharacterMotor motor;

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
        private bool _jumpingFromJumpPad;

        // other
        public PlayerMovementInputs Inputs { get; private set; }

        private Vector3 _additionalVelocity;
        private float _gravityVelocity;

        public Vector3 LocalTransientVelocity { get; private set; }
        private Vector3 _prevTransientPosition;

        [HideInInspector] public UnityEvent<float> onGroundSlamLanded = new();
        [HideInInspector] public UnityEvent onJump = new();
        [HideInInspector] public UnityEvent<Vector3> onWalled = new();
        [HideInInspector] public UnityEvent onUnwalled = new();
        [HideInInspector] public UnityEvent onDash = new();
        [HideInInspector] public UnityEvent<bool> onEndDash = new();

        protected override void OnValidate()
        {
            base.OnValidate();

            if (Application.isPlaying) return;
            player = GetComponent<PlayerCore>();
            motor = GetComponent<KinematicCharacterMotor>();

            motor.SetCapsuleDimensions
            (
                config.colliderCapsuleRadius,
                config.colliderCapsuleHeight,
                config.colliderCapsuleOffset
            );
        }

        private void Awake()
        {
            motor.enabled = false;
            player.onHandlingThisPlayer.AddListener(() =>
            {
                motor.CharacterController = this;
                motor.enabled = true;

                player.locks.onLockStateChange.AddListener((plock, locked) =>
                {
                    if (plock == PlayerLock.Motor) motor.enabled = !locked;
                });

                player.onLocalTriggerEnter.AddListener((collider) =>
                {
                    if (!collider.CompareTag("DashOrb")) return;

                    _canDash = true;
                    _dashCooldownTimer = 0f;
                });
            });
        }

        [Server]
        public void ServerMove(Vector3 position)
        {
            if (player.HandlingThisPlayer) Move(position);
            else TargetMove(position);

            player.hitEntity.MoveEntityObservation(position);
        }

        [TargetRpc]
        private void TargetMove(Vector3 position)
        {
            Move(position);
        }

        public void Move(Vector3 position)
        {
            _additionalVelocity = Vector3.zero;
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

        public void AfterCharacterUpdate(float deltaTime)
        {
            var drag = motor.GroundingStatus.IsStableOnGround ? config.groundAdditionalVelocityDrag : config.airAdditionalVelocityDrag;
            _additionalVelocity *= 1f - drag * deltaTime;

            if (new Vector2(_additionalVelocity.x, _additionalVelocity.z).magnitude <= 0.5f)
                _additionalVelocity = Vector3.up * _additionalVelocity.y;

            LocalTransientVelocity = (motor.TransientPosition - _prevTransientPosition) / deltaTime;
            _prevTransientPosition = motor.TransientPosition;
        }

        public void BeforeCharacterUpdate(float deltaTime)
        {
            if (controller == null) return;
            CheckWalled();

            _prevWishJumping = Inputs.wishJumping;
            _prevWishDashing = Inputs.wishDashing;
            Inputs = player.locks.Locked(PlayerLock.Input) ? default : controller.GetInputs();

            if (!Inputs.wishGroundSlam) _canGroundSlam = true;
            if (!motor.GroundingStatus.IsStableOnGround
                && Inputs.wishGroundSlam
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

            if (Inputs.wishDashing && !_prevWishDashing)
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

                var playerViewRot = Quaternion.Euler(new(Inputs.orientationX, player.horizontalOrientation.eulerAngles.y, 0f));
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
                    var relative = Inputs.move.sqrMagnitude == 0 ? Vector3.forward : new Vector3(Inputs.move.x, 0f, Inputs.move.y);
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

            if (Inputs.wishJumping && !_prevWishJumping)
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
                if (!Inputs.wishDashing)
                    _canDash = true;
            }
            else _coyoteTimer += deltaTime;

            if (_coyoteTimer < config.coyoteTime)
            {
                _jumpTimer = 0f;
                _currentJumpHeight = 0f;

                if (_bufferTimer <= config.bufferTime && !_jumpingFromJumpPad) BeginJump();
            }
            else if (_jumpingFromJumpPad) _jumpingFromJumpPad = false;

            var currentY = transform.position.y - _currentJumpHeight;
            if (!Inputs.wishJumping)
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

        public bool IsColliderValidForCollisions(Collider coll) => true;
        public void OnDiscreteCollisionDetected(Collider hitCollider) { }
        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            if (Vector3.Dot(hitNormal, _dashDirection) < -0.9f && _dashing)
            {
                _dashing = false;
                onEndDash.Invoke(false);
            }

            if (hitNormal.y < 0f)
            {
                _jumping = false;
                _endingJump = false;
                _additionalVelocity.y = Mathf.Min(0f, _additionalVelocity.y);
                _gravityVelocity = Mathf.Min(0f, _gravityVelocity);
            }

            if (hitCollider.TryGetComponent(out JumpPad jumpPad))
            {
                _additionalVelocity.y = jumpPad.force;
                _gravityVelocity = 0f;
                _jumpingFromJumpPad = true;
                _coyoteTimer = 0f;
            }
        }

        public void PostGroundingUpdate(float deltaTime) { }
        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) { }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_dashing)
            {
                if ((_dashTimer >= config.dashDuration) || (Inputs.wishJumping && !_prevWishJumping))
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

            if (Inputs.move != _prevMoveInput)
            {
                _fromMoveInput = _targetMoveInput;
                _elapsedFromMoveInputChange = 0f;
            }
            _prevMoveInput = Inputs.move;

            if (Inputs.move.sqrMagnitude != 0)
            {
                _idleTime = 0f;

                _movementTime += deltaTime;
                _elapsedFromMoveInputChange += Time.deltaTime;

                var smoothingDuration = motor.GroundingStatus.IsStableOnGround ? config.groundMoveSmoothingDuration : config.airMoveSmoothingDuration;
                _targetMoveInput = Vector2.Lerp
                (
                    _fromMoveInput,
                    Inputs.move.normalized,
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
            var dir = player.horizontalOrientation.rotation * new Vector3(_targetMoveInput.x, 0f, _targetMoveInput.y);

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

                _canDash = true;
                _dashCooldownTimer = 0f;
                onEndDash.Invoke(true);

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

        private void BeginJump()
        {
            _coyoteTimer = config.coyoteTime;
            motor.ForceUnground();
            _jumping = true;
            if (!_walled) _jumpingFromGround = true;

            onJump.Invoke();
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
                    if (Physics.Raycast(origin, player.horizontalOrientation.rotation * dir, out hit, maxdist, config.wallLayers, QueryTriggerInteraction.Ignore))
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

        [TargetRpc]
        public void TargetSetAdditionalForce(Vector3 force)
        {
            if (player.locks.Locked(PlayerLock.Force)) return;
            _additionalVelocity = force;
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
        }
    }
}