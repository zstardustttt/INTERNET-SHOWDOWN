using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Damage;
using Game.Core.Events;
using Game.Core.Maps;
using Game.Events.Player;
using Game.Events.UI;
using Game.Inputs;
using Game.Network.Messages;
using Game.Other;
using Mirror;
using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(PlayerBase))]
    public class OnlinePlayer : NetworkBehaviour, IPlayerController
    {
        public GameObject cameraPrefab;
        public PlayerBase player;
        public Transform cameraOrientation;

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
        public float shakeFrequency;
        public float shakeFalloffSpeed;
        private ShakeGenerator _shakeGenerator;

        [Space(9)]
        public float groundSlamCameraShakeMultiplier;
        public float maxGroundSlamCameraShake;

        private PlayerCamera _camera;
        private float _cameraRotX;
        private Vector3 _prevPosition;
        private float _timeSinceRunning;

        private float _mouseSens;

        private PlayerActions _actions;

        protected override void OnValidate()
        {
            base.OnValidate();
            player = GetComponent<PlayerBase>();
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
            _camera = Instantiate(cameraPrefab, cameraOrientation).GetComponent<PlayerCamera>();
            player.modelContainer.SetActive(false);

            // TODO: ts
            _mouseSens = PlayerPrefs.GetFloat("sens");

            _actions = new();

            player.onGroundSlamLanded.AddListener((dist) =>
            {
                var amplitude = Mathf.Min(dist * groundSlamCameraShakeMultiplier, maxGroundSlamCameraShake);
                _shakeGenerator.Shake(amplitude, shakeFrequency, shakeFalloffSpeed);
            });

            player.onJump.AddListener(() =>
            {
                jumpSource.pitch = 0.6f;
                _jumpVolumeTimer = player.config.jumpDuration;
                jumpSource.Play();
            });

            player.onWalled.AddListener((_) => wallLockSource.Play());
            player.onCollide.AddListener((collider) =>
            {
                if (!collider.CompareTag("Portal")) return;
                NetworkClient.Send<ClientRequestMapLoad>(new());
            });

            player.onItemPickup.AddListener(() =>
            {
                if (!isLocalPlayer) return;
                itemPickupSource.Play();
            });

            player.onDash.AddListener(() =>
            {
                if (!isLocalPlayer) return;
                EventBus<OnDash>.Invoke(new()
                {
                    player = player,
                    cooldown = player.config.dashCooldown
                });
            });

            player.onEndDash.AddListener((reset) =>
            {
                if (!isLocalPlayer) return;
                EventBus<OnEndDash>.Invoke(new()
                {
                    player = player,
                    reset = reset,
                });
            });

            player.onRespawn.AddListener(() =>
            {
                EventBus<RespawnEffectRequest>.Invoke(new());
            });

            player.controller = this;
            _actions.Enable();

            player.HandleThisPlayer();

            player.SetPlayerName(Environment.UserName);
            player.SetGUID(OnlinePlayerGuid.Guid);
            CmdPlayerInitialized();

            EventBus<OnCameraShakerSpawn>.Listen((data) =>
            {
                var shaker = data.shaker;
                var distance = Vector3.Distance(_camera.transform.position, shaker.transform.position);
                var amplitude = shaker.amplitude;
                if (distance > shaker.minDistance)
                    amplitude *= 1f - Mathf.InverseLerp(shaker.minDistance, shaker.maxDistance, distance);

                _shakeGenerator.Shake(amplitude, shakeFrequency, shakeFalloffSpeed);
            });
        }

        [Command]
        public void CmdPlayerInitialized()
        {
            EventBus<OnServerOnlinePlayerInitialized>.Invoke(new() { player = player });
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
                player.Damage(10f, null, false);
            }

            if (NetworkServer.active && Input.GetKeyDown(KeyCode.F3))
            {
                player.Heal(10f);
            }
#endif

            // TODO: new input system
            if (Input.GetMouseButtonDown(0)) player.TryUseItem(false);
            else if (Input.GetMouseButtonDown(1)) player.TryUseItem(true);

            // SIDE RUN TILT
            var targetSideRunTilt = player.inputs.move.normalized.x * maxSideRunTilt;
            _sideRunTilt = Mathf.Lerp(_sideRunTilt, targetSideRunTilt, Time.deltaTime * sideRunTiltSmoothingSpeed);

            // CAMERA BOP
            if (player.inputs.move.sqrMagnitude > 0 && player.motor.GroundingStatus.IsStableOnGround)
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
            cameraOrientation.localPosition = shake + Vector3.up * _cameraBopHeight;

            // FOOTSTEPS
            if (_footstepTimer >= Mathf.PI / cameraBopFrequency)
            {
                if (_cameraBopTilt < 0f) leftFootstepSource.Play();
                else rightFootstepSource.Play();
                _footstepTimer = 0f;
            }

            // CAMERA ROTATION
            var delta = _actions.Camera.Look.ReadValue<Vector2>() * _mouseSens;
            if (player.item) player.item.Sway(delta, player.localTransientVelocity, player.verticalOrientation);

            player.horizontalOrientation.localEulerAngles += new Vector3(0f, delta.x, 0f);
            _cameraRotX -= delta.y;
            _cameraRotX = Mathf.Clamp(_cameraRotX, -90f, 90f);
            cameraOrientation.localRotation = Quaternion.Euler
            (
                _cameraRotX,
                0f,
                _sideRunTilt + _cameraBopTilt
            );

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
                var dot = Vector3.Dot(cameraOrientation.transform.forward, dir);
                var targetFov = Mathf.Lerp(idleFOV, maxFOV, FOVCurve.Evaluate(speed / maxFOVSpeed * Mathf.Abs(dot)));
                _camera.camera.fieldOfView = Mathf.Lerp(_camera.camera.fieldOfView, targetFov, Time.deltaTime * FOVSmoothingSpeed);
            }
            else _camera.camera.fieldOfView = idleFOV;

            // SPEEDLINES
            if (enableSpeedlines)
            {
                if (speed >= minSpeedlinesSpeed)
                {
                    var targetAlpha = speedlinesAlphaCurve.Evaluate((speed - minSpeedlinesSpeed) / maxSpeedlinesSpeed);
                    _currentSpeedlinesAlpha = Mathf.Lerp(_currentSpeedlinesAlpha, targetAlpha, Time.deltaTime * speedlinesAlphaSmoothingSpeed);

                    _camera.speedlines.transform.SetPositionAndRotation(cameraOrientation.position + dir * 2.3f, Quaternion.LookRotation(-dir));
                }
                else _currentSpeedlinesAlpha = Mathf.Lerp(_currentSpeedlinesAlpha, 0f, Time.deltaTime * speedlinesAlphaSmoothingSpeed);

                speedlinesFullscreenMaterial.SetFloat("_alpha", _currentSpeedlinesAlpha);
            }
            else speedlinesFullscreenMaterial.SetFloat("_alpha", 0f);

            // JUMP SOUND
            var pitchDir = Mathf.Round((transform.position.y - _prevPosition.y) * 20f) / 20f;
            var pitchFactor = pitchDir > 0 ? jumpPitchUpRate : pitchDir == 0 ? 0f : -jumpPitchDownRate;
            jumpSource.pitch += pitchFactor * (1f - _jumpVolumeTimer / player.config.jumpDuration);
            jumpSource.volume = jumpVolumeCurve.Evaluate(1f - _jumpVolumeTimer / player.config.jumpDuration) * jumpVolumeMultiplier;
            _jumpVolumeTimer -= Time.deltaTime;

            _prevPosition = transform.position;
        }

        public PlayerInputs GetInputs()
        {
            if (!isLocalPlayer) return new();

            return new()
            {
                move = _actions.Movement.Move.ReadValue<Vector2>(),
                wishJumping = _actions.Movement.Jump.inProgress,
                wishDashing = _actions.Movement.Dash.inProgress,
                wishGroundSlam = _actions.Movement.GroundSlam.inProgress,
                orientationX = _cameraRotX,
            };
        }
    }
}