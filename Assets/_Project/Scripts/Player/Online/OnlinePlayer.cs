using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Game.Core.Damages;
using Game.Core.Events;
using Game.Core.Maps;
using Game.Core.Player;
using Game.Core.Player.Movement;
using Game.Events.UI;
using Game.Inputs;
using Game.Network.Messages;
using Game.Other;
using Game.Player.Online.Events;
using Mirror;
using UnityEngine;

namespace Game.Player.Online
{
    [RequireComponent(typeof(PlayerCore))]
    public class OnlinePlayer : NetworkBehaviour, IPlayerMovementController
    {
        public static OnlinePlayer localPlayer;

        public GameObject cameraPrefab;
        public PlayerCore player;
        public Transform cameraHolder;

        [Space(9)]
        public int speedRecordSize;

        private Queue<float> _speedRecord;

        [Header("Audio")]
        public AudioSource leftFootstepSource;
        public AudioSource rightFootstepSource;
        public AudioSource wallLockSource;
        public AudioSource itemPickupSource;

        private float _footstepTimer;

        [Space(9)]
        public AudioSource jumpSource;
        public float jumpPitchUpRate;
        public float jumpPitchDownRate;
        public AnimationCurve jumpVolumeCurve;
        public float jumpVolumeMultiplier;

        private float _jumpVolumeTimer;

        [Space(9)]
        public AudioSource windAudioSource;
        public float windVolumeSmoothingSpeed;
        public float windVolumeMultiplier;

        [Header("Speedlines")]
        public bool enableSpeedlines;
        public Material speedlinesFullscreenMaterial;
        public float minSpeedlinesSpeed;
        public float maxSpeedlinesSpeed;
        public AnimationCurve speedlinesAlphaCurve;
        public float speedlinesAlphaSmoothingSpeed;

        private float _currentSpeedlinesAlpha;

        [Header("Speed Affects FOV")]
        public bool enableSpeedAffectsFOV;
        public float idleFOV;
        public float maxFOV;
        public float maxFOVSpeed;
        public AnimationCurve FOVCurve;
        public float FOVSmoothingSpeed;

        [Header("Side Run Tilt")]
        public float maxSideRunTilt;
        public float sideRunTiltSmoothingSpeed;

        private float _sideRunTilt;

        [Header("Camera Bop")]
        public float cameraBopAmplitude;
        public float cameraBopTiltAmplitude;
        public float cameraBopFrequency;
        public float cameraBopStopSpeed;

        private float _cameraBopHeight;
        private float _cameraBopTilt;

        [Header("Camera Shake")]
        public AnimationCurve shakeDistanceCurve;
        public float shakeFrequency;
        public float shakeFalloffSpeed;
        private ShakeGenerator _shakeGenerator;

        [Space(9)]
        public float groundSlamCameraShakeMultiplier;
        public float maxGroundSlamCameraShake;

        [Header("Item Animations")]
        public float itemUseFOVAddition;
        public float itemUseFOVDuration;
        public AnimationCurve itemUseFOVCurve;

        [Space(9)]
        public Vector3 itemPickScale;
        public float itemPickScaleDuration;
        public AnimationCurve itemPickScaleCurve;

        [Space(9)]
        public float itemPickZOffset;
        public float itemPickZOffsetDuration;
        public AnimationCurve itemPickZOffsetCurve;

        private PlayerCamera _camera;
        private float _cameraRotX;
        private Vector3 _prevPosition;
        private float _timeSinceRunning;
        private float _mouseSens;

        private float _cameraSpeedFOV;
        private float _itemUseFOVAddition;
        private int _itemVisualLayer;
        private TweenerCore<float, float, FloatOptions> _itemUseFOVTween;
        private TweenerCore<Vector3, Vector3, VectorOptions> _itemPickScaleTween;
        private TweenerCore<Vector3, Vector3, VectorOptions> _itemPickZOffsetTween;

        private PlayerActions _actions;

        protected override void OnValidate()
        {
            base.OnValidate();

            if (Application.isPlaying) return;
            player = GetComponent<PlayerCore>();
        }

        private void Start()
        {
            windAudioSource.volume = 0f;
            jumpSource.volume = 0f;
        }

        public override void OnStartLocalPlayer()
        {
            _speedRecord = new(speedRecordSize);
            _shakeGenerator = new();

            Cursor.lockState = CursorLockMode.Locked;
            _camera = Instantiate(cameraPrefab, cameraHolder).GetComponent<PlayerCamera>();
            player.modelContainer.SetActive(false);

            // TODO: ts
            _mouseSens = PlayerPrefs.GetFloat("sens");

            _actions = new();

            player.movementModule.onGroundSlamLanded.AddListener((dist) =>
            {
                var amplitude = Mathf.Min(dist * groundSlamCameraShakeMultiplier, maxGroundSlamCameraShake);
                _shakeGenerator.Shake(amplitude, shakeFrequency, shakeFalloffSpeed);
            });

            player.movementModule.onJump.AddListener(() =>
            {
                jumpSource.pitch = 0.6f;
                _jumpVolumeTimer = player.movementModule.config.jumpDuration;
                jumpSource.Play();
            });

            player.movementModule.onWalled.AddListener((_) => wallLockSource.Play());

            player.onLocalTriggerEnter.AddListener((collider) =>
            {
                if (!collider.CompareTag("Portal")) return;
                NetworkClient.Send<ClientRequestMapLoad>(new());
            });

            player.itemModule.onItemPickup.AddListener(() =>
            {
                if (!isLocalPlayer) return;
                itemPickupSource.Play();
            });

            player.movementModule.onDash.AddListener(() =>
            {
                if (!isLocalPlayer) return;
                EventBus<OnLocalPlayerDash>.Invoke(new()
                {
                    cooldown = player.movementModule.config.dashCooldown
                });
            });

            player.movementModule.onEndDash.AddListener((reset) =>
            {
                if (!isLocalPlayer) return;
                EventBus<OnLocalPlayerEndDash>.Invoke(new()
                {
                    reset = reset,
                });
            });

            player.deathModule.onRespawn.AddListener(() =>
            {
                EventBus<RespawnEffectRequest>.Invoke(new());
            });

            player.itemModule.onDestroyItem.AddListener(() =>
            {
                _itemPickScaleTween?.Kill();
                _itemPickZOffsetTween?.Kill();
            });

            _itemVisualLayer = LayerMask.NameToLayer("ItemVisual");
            player.itemModule.onItemPickup.AddListener(() =>
            {
                var children = player.itemModule.item.GetComponentsInChildren<Transform>(includeInactive: true);
                foreach (var child in children)
                {
                    child.gameObject.layer = _itemVisualLayer;
                }

                PickItemAnimation();
            });

            player.itemModule.onItemUsed.AddListener((fullyUsed) =>
            {
                OnItemUsedAnimation();
                if (!fullyUsed) PickItemAnimation();
            });

            player.movementModule.controller = this;
            _actions.Enable();

            player.onDealtDamage.AddListener((source, type, amount) =>
            {
                EventBus<OnLocalPlayerDealtDamage>.Invoke(new()
                {
                    source = source,
                    type = type,
                    amount = amount
                });
            });

            player.onDealtDamageOnPlayer.AddListener((target, source, type, amount) =>
            {
                EventBus<OnLocalPlayerDealtDamageOnPlayer>.Invoke(new()
                {
                    target = target,
                    source = source,
                    type = type,
                    amount = amount
                });
            });

            player.HandleThisPlayer(new()
            {
                name = Environment.UserName,
                guid = OnlinePlayerGuid.Guid
            });

            EventBus<OnCameraShakerSpawn>.Listen((data) =>
            {
                var shaker = data.shaker;
                var distance = Vector3.Distance(_camera.transform.position, shaker.transform.position);
                var amplitude = shaker.amplitude;
                if (distance > shaker.minDistance)
                {
                    var distance01 = Mathf.InverseLerp(shaker.minDistance, shaker.maxDistance, distance);
                    amplitude *= shakeDistanceCurve.Evaluate(1f - distance01);
                }

                _shakeGenerator.Shake(Mathf.Max(amplitude, _shakeGenerator.shakeAmplitude), shakeFrequency, shakeFalloffSpeed);
            });

            _cameraSpeedFOV = idleFOV;
            localPlayer = this;
            EventBus<OnLocalPlayerStarted>.Invoke(new() { player = player });
        }

        private void OnItemUsedAnimation()
        {
            _itemUseFOVTween?.Kill(false);
            _itemUseFOVAddition = 0f;
            _itemUseFOVTween = DOTween.To(() => _itemUseFOVAddition, x => _itemUseFOVAddition = x, itemUseFOVAddition, itemUseFOVDuration).SetEase(itemUseFOVCurve);
        }

        private void PickItemAnimation()
        {
            if (!player.itemModule.item) return;
            var item = player.itemModule.item;
            item.transform.localScale = itemPickScale;
            item.transform.localPosition = item.offset + Vector3.forward * itemPickZOffset;

            _itemPickScaleTween?.Kill();
            _itemPickZOffsetTween?.Kill();

            _itemPickScaleTween = item.transform.DOScale(Vector3.one, itemPickScaleDuration).SetEase(itemPickScaleCurve);
            _itemPickZOffsetTween = item.transform.DOLocalMoveZ(item.offset.z, itemPickZOffsetDuration).SetEase(itemPickZOffsetCurve);
        }

        private void OnDestroy()
        {
            if (!isLocalPlayer) return;
            _actions.Disable();
            speedlinesFullscreenMaterial.SetFloat("_alpha", 0f);
        }

        private int _hostPreviousSceneBuildIdx;

        private void Update()
        {
            if (!isLocalPlayer) return;

            // enviroment apply fot host player
            if (NetworkServer.active && gameObject.scene.isLoaded)
            {
                if (gameObject.scene.buildIndex != _hostPreviousSceneBuildIdx)
                {
                    SceneEnviromentData.TryApplyOnScene(gameObject.scene);
                }
                _hostPreviousSceneBuildIdx = gameObject.scene.buildIndex;
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (Cursor.lockState == CursorLockMode.Locked) Cursor.lockState = CursorLockMode.None;
                else Cursor.lockState = CursorLockMode.Locked;
            }

#if DEBUG
            if (NetworkServer.active && Input.GetKeyDown(KeyCode.F2))
            {
                player.healthModule.ApplyDamage(new(DamageType.Indirect, 10f, player, player.teamReference.team, DamageIdentification.From(null)));
            }

            if (NetworkServer.active && Input.GetKeyDown(KeyCode.F3))
            {
                player.healthModule.Heal(10f);
            }
#endif

            // TODO: new input system
            if (Input.GetMouseButtonDown(0)) player.itemModule.TryUseItem(false);
            else if (Input.GetMouseButtonDown(1)) player.itemModule.TryUseItem(true);

            // SIDE RUN TILT
            var targetSideRunTilt = player.movementModule.Inputs.move.normalized.x * maxSideRunTilt;
            _sideRunTilt = Mathf.Lerp(_sideRunTilt, targetSideRunTilt, Time.deltaTime * sideRunTiltSmoothingSpeed);

            // CAMERA BOP
            if (player.movementModule.Inputs.move.sqrMagnitude > 0 && player.movementModule.motor.GroundingStatus.IsStableOnGround)
            {
                _timeSinceRunning += Time.deltaTime;
                _footstepTimer += Time.deltaTime;
            }
            else
            {
                _timeSinceRunning = 0f;
                _footstepTimer = 0f;
            }

            if (_timeSinceRunning == 0f)
            {
                _cameraBopHeight = Mathf.Lerp(_cameraBopHeight, 0f, Time.deltaTime * cameraBopStopSpeed);
                _cameraBopTilt = Mathf.Lerp(_cameraBopTilt, 0f, Time.deltaTime * cameraBopStopSpeed);
            }
            else
            {
                _cameraBopHeight = Mathf.Max(Mathf.Sin(_timeSinceRunning * cameraBopFrequency), Mathf.Sin(_timeSinceRunning * cameraBopFrequency + Mathf.PI)) * cameraBopAmplitude;
                _cameraBopTilt = Mathf.Sin(_timeSinceRunning * cameraBopFrequency) * cameraBopTiltAmplitude;
            }

            var shake = _shakeGenerator.GetShake();
            cameraHolder.localPosition = shake + Vector3.up * _cameraBopHeight;

            // FOOTSTEPS
            if (_footstepTimer >= Mathf.PI / cameraBopFrequency)
            {
                if (_cameraBopTilt < 0f) leftFootstepSource.Play();
                else rightFootstepSource.Play();
                _footstepTimer = 0f;
            }

            // CAMERA ROTATION
            var delta = _actions.Camera.Look.ReadValue<Vector2>() * _mouseSens;
            if (player.itemModule.item) player.itemModule.item.Sway(delta, player.movementModule.LocalTransientVelocity, player.verticalOrientation);

            _cameraRotX -= delta.y;
            _cameraRotX = Mathf.Clamp(_cameraRotX, -90f, 90f);

            player.horizontalOrientation.localEulerAngles += new Vector3(0f, delta.x, 0f);
            player.verticalOrientation.localEulerAngles = Vector3.right * _cameraRotX;
            _camera.transform.localEulerAngles = Vector3.forward * (_sideRunTilt + _cameraBopTilt);

            // FIND SPEED
            var velocity = transform.position - _prevPosition; ;
            var rawSpeed = velocity.magnitude / Time.deltaTime;
            var dir = velocity.normalized;

            if (_speedRecord.Count == speedRecordSize) _speedRecord.Dequeue();
            _speedRecord.Enqueue(rawSpeed);
            var speed = _speedRecord.ToArray().Average();

            // WIND SOUND
            windAudioSource.volume = Mathf.Lerp
            (
                windAudioSource.volume,
                speedlinesAlphaCurve.Evaluate((speed - minSpeedlinesSpeed) / maxSpeedlinesSpeed) * windVolumeMultiplier,
                Time.deltaTime * windVolumeSmoothingSpeed
            );

            // FOV
            if (enableSpeedAffectsFOV)
            {
                var dot = Vector3.Dot(_camera.transform.transform.forward, dir);
                var targetFov = Mathf.Lerp(idleFOV, maxFOV, FOVCurve.Evaluate(speed / maxFOVSpeed * Mathf.Abs(dot)));
                _cameraSpeedFOV = Mathf.Lerp(_cameraSpeedFOV, targetFov, Time.deltaTime * FOVSmoothingSpeed);
            }
            else _cameraSpeedFOV = idleFOV;

            _camera.camera.fieldOfView = _cameraSpeedFOV + _itemUseFOVAddition;

            // SPEEDLINES
            if (enableSpeedlines)
            {
                if (speed >= minSpeedlinesSpeed)
                {
                    var targetAlpha = speedlinesAlphaCurve.Evaluate((speed - minSpeedlinesSpeed) / maxSpeedlinesSpeed);
                    _currentSpeedlinesAlpha = Mathf.Lerp(_currentSpeedlinesAlpha, targetAlpha, Time.deltaTime * speedlinesAlphaSmoothingSpeed);

                    _camera.speedlines.transform.SetPositionAndRotation(_camera.transform.position + dir * 2.3f, Quaternion.LookRotation(-dir));
                }
                else _currentSpeedlinesAlpha = Mathf.Lerp(_currentSpeedlinesAlpha, 0f, Time.deltaTime * speedlinesAlphaSmoothingSpeed);

                speedlinesFullscreenMaterial.SetFloat("_alpha", _currentSpeedlinesAlpha);
            }
            else speedlinesFullscreenMaterial.SetFloat("_alpha", 0f);

            // JUMP SOUND
            var pitchDir = Mathf.Round((transform.position.y - _prevPosition.y) * 20f) / 20f;
            var pitchFactor = pitchDir > 0 ? jumpPitchUpRate : pitchDir == 0 ? 0f : -jumpPitchDownRate;
            jumpSource.pitch += pitchFactor * (1f - _jumpVolumeTimer / player.movementModule.config.jumpDuration);
            jumpSource.volume = jumpVolumeCurve.Evaluate(1f - _jumpVolumeTimer / player.movementModule.config.jumpDuration) * jumpVolumeMultiplier;
            _jumpVolumeTimer -= Time.deltaTime;

            _prevPosition = transform.position;
        }

        public PlayerMovementInputs GetInputs()
        {
            if (!isLocalPlayer) return new();

            return new()
            {
                move = _actions.Movement.Move.ReadValue<Vector2>(),
                wishJumping = _actions.Movement.Jump.inProgress,
                wishDashing = _actions.Movement.Dash.inProgress,
                wishGroundSlam = _actions.Movement.GroundSlam.inProgress,
            };
        }
    }
}