using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Game.Player.Effects
{
    // i want to die
    [RequireComponent(typeof(PlayerBase))]
    public class PlayerEffectsModule : NetworkBehaviour
    {
        public bool serverSide;
        public PlayerEffectsConfig config;
        public Transform audioSourcesContainer;
        public PlayerBase player;

        private bool _wasOnGround;

        private Vector3 _velocity;
        private Vector3 _prevPosition;
        private Queue<Vector3> _velocityRecord;

        private AudioSource _jumpPrimaryAudioSource;
        private AudioSource _dashAudioSource;
        private AudioSource _groundSlamAudioSource;
        private AudioSource _landAudioSource;
        private AudioSource _wallSlideAudioSource;
        private AudioSource _skidAudioSource;
        private AudioSource _respawnAudioSource;

        private bool Owned => serverSide ? isServer : isOwned;
        [SyncVar] private bool _walled;
        [SyncVar] private Vector3 _wallNormal;

        protected override void OnValidate()
        {
            base.OnValidate();
            player = GetComponent<PlayerBase>();
        }

        private void Awake()
        {
            _jumpPrimaryAudioSource = Instantiate(config.jumpPrimaryAudioSource, audioSourcesContainer).GetComponent<AudioSource>();
            _dashAudioSource = Instantiate(config.dashAudioSource, audioSourcesContainer).GetComponent<AudioSource>();
            _groundSlamAudioSource = Instantiate(config.groundSlamAudioSource, audioSourcesContainer).GetComponent<AudioSource>();
            _landAudioSource = Instantiate(config.landAudioSource, audioSourcesContainer).GetComponent<AudioSource>();
            _wallSlideAudioSource = Instantiate(config.wallSlideAudioSource, audioSourcesContainer).GetComponent<AudioSource>();
            _skidAudioSource = Instantiate(config.skidAudioSource, audioSourcesContainer).GetComponent<AudioSource>();
            _respawnAudioSource = Instantiate(config.respawnAudioSource, audioSourcesContainer).GetComponent<AudioSource>();

            _wallSlideAudioSource.volume = 0f;
        }

        private void Start()
        {
            if (!Owned) return;
            _velocityRecord = new(config.velocityRecordSize);

            player.onDash.AddListener(OnDash);
            player.onJump.AddListener(OnJump);
            player.onGroundSlamLanded.AddListener((_) => OnGroundSlamLand());

            player.onWalled.AddListener(OnWalled);
            player.onUnwalled.AddListener(OnUnwalled);
            player.deathModule.onRespawn.AddListener(OnRespawn);

            if (isOwned) _respawnAudioSource.spatialBlend = 0f;
        }

        private void OnRespawn()
        {
            _respawnAudioSource.Play();

            if (serverSide && isServer) RpcOnRespawn();
            else if (isOwned) CmdOnRespawn();
        }
        [Command]
        private void CmdOnRespawn() => RpcOnRespawn();
        [ClientRpc(includeOwner = false)]
        private void RpcOnRespawn()
        {
            _respawnAudioSource.Play();
        }

        private void OnWalled(Vector3 normal)
        {
            if (serverSide && isServer)
            {
                _walled = true;
                _wallNormal = normal;
            }
            else if (isOwned) CmdOnWalled(normal);
        }
        [Command]
        private void CmdOnWalled(Vector3 normal)
        {
            _walled = true;
            _wallNormal = normal;
        }

        private void OnUnwalled()
        {
            if (serverSide && isServer) _walled = false;
            else if (isOwned) CmdOnUnwalled();
        }
        [Command]
        private void CmdOnUnwalled() => _walled = false;

        private void OnDash()
        {
            _dashAudioSource.Play();

            if (serverSide && isServer) RpcOnDash();
            else if (isOwned) CmdOnDash();
        }
        [Command]
        private void CmdOnDash() => RpcOnDash();
        [ClientRpc(includeOwner = false)]
        private void RpcOnDash()
        {
            _dashAudioSource.Play();
        }

        private void OnJump()
        {
            _jumpPrimaryAudioSource.Play();

            if (serverSide && isServer) RpcOnJump();
            else if (isOwned) CmdOnJump();
        }
        [Command]
        private void CmdOnJump() => RpcOnJump();
        [ClientRpc(includeOwner = false)]
        private void RpcOnJump()
        {
            _jumpPrimaryAudioSource.Play();
        }

        private void OnGroundSlamLand()
        {
            _groundSlamAudioSource.Play();

            if (serverSide && isServer) RpcOnGroundSlamLand();
            else if (isOwned) CmdOnGroundSlamLand();
        }
        [Command]
        private void CmdOnGroundSlamLand() => RpcOnGroundSlamLand();
        [ClientRpc(includeOwner = false)]
        private void RpcOnGroundSlamLand()
        {
            _groundSlamAudioSource.Play();
        }

        private void FixedUpdate()
        {
            if (!Owned) return;
            _velocity = transform.position - _prevPosition;

            if (_velocityRecord.Count == config.velocityRecordSize) _velocityRecord.Dequeue();
            _velocityRecord.Enqueue(_velocity);

            _prevPosition = transform.position;
        }

        private void Update()
        {
            // WALL SLIDE
            if (_walled)
            {
                _wallSlideAudioSource.transform.localPosition = -_wallNormal * player.motor.Capsule.radius + Vector3.up * player.motor.Capsule.height / 2f;
                _wallSlideAudioSource.volume = Mathf.Min(_wallSlideAudioSource.volume + config.wallSlideVolumeIncreaseRate * Time.deltaTime, config.wallSlideVolume);
            }
            else
                _wallSlideAudioSource.volume = Mathf.Lerp(_wallSlideAudioSource.volume, 0f, Time.deltaTime * config.wallSlideVolumeSmoothingSpeed);

            if (!Owned) return;

            // SKID
            if (_velocityRecord.Count > 0)
            {
                var oldVelocity = _velocityRecord.Peek();
                var diff = new Vector2(_velocity.x, _velocity.z).magnitude - new Vector2(oldVelocity.x, oldVelocity.z).magnitude;
                if (diff <= config.skidThreshold && player.motor.GroundingStatus.IsStableOnGround && !_skidAudioSource.isPlaying)
                    OnSkid();
            }

            // LAND
            if (player.motor.GroundingStatus.IsStableOnGround && !_wasOnGround) OnLand();
            _wasOnGround = player.motor.GroundingStatus.IsStableOnGround;
        }

        private void OnSkid()
        {
            _skidAudioSource.Play();

            if (serverSide && isServer) RpcOnSkid();
            else if (isOwned) CmdOnSkid();
        }
        [Command]
        private void CmdOnSkid() => RpcOnSkid();
        [ClientRpc(includeOwner = false)]
        private void RpcOnSkid()
        {
            _skidAudioSource.Play();
        }

        private void OnLand()
        {
            _landAudioSource.Play();

            if (serverSide && isServer) RpcOnLand();
            else if (isOwned) CmdOnLand();
        }
        [Command]
        private void CmdOnLand() => RpcOnLand();
        [ClientRpc(includeOwner = false)]
        private void RpcOnLand()
        {
            _landAudioSource.Play();
        }
    }
}