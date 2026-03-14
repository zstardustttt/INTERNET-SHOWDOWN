using UnityEngine;
using Mirror;
using Game.Core.Player;
using System;
using Random = UnityEngine.Random;
using Game.Core.Player.Movement;
using Game.Core.Maps;
using Game.Boxes;
using Game.Player.AI.Actions;
using Game.Core.Damages;

namespace Game.Player.AI
{
    [RequireComponent(typeof(PlayerCore))]
    public class AIPlayer : NetworkBehaviour, IPlayerMovementController
    {
        public PlayerCore player;
        private GameObject _portal;

        protected override void OnValidate()
        {
            base.OnValidate();

            if (Application.isPlaying) return;
            player = GetComponent<PlayerCore>();
        }

        public override void OnStartServer()
        {
            player.HandleThisPlayer(new()
            {
                name = $"Stupid Clanker {Random.Range(1, 100)}",
                guid = Guid.NewGuid()
            });

            player.onLocalTriggerEnter.AddListener((collider) =>
            {
                if (!collider.CompareTag("Portal")) return;

                if (!MapLoader.TryMoveGameObjectToMap(gameObject))
                {
                    Debug.LogWarning($"AI {player.Identification.name} wanted to load into unloaded map or has already loaded that map");
                    return;
                }

                player.movementModule.ServerMove(MapLoader.loadedMap.info.GetRandomSpawnPoint());
            });

            player.movementModule.controller = this;

            _lookAtAction = new LookAtAction(this, Vector3.zero);
            _matchAction = new ParallelAction(this, new FiniteAction[]
            {
                new FrameAction(this, 1, _lookAtAction),
                new FrameAction(this, 2, new MoveAction(this, Vector2.up)),
                new SequencedAction(this, 0f, new FiniteAction[]
                {
                    new FrameAction(this, 1, new DashAction(this, true)),
                    new FrameAction(this, 1, new DashAction(this, false)),
                })
            }, (action) => action.Restart());
        }

        public PlayerMovementInputs GetInputs(float deltaTime)
        {
            /*LookAt(Vector3.right * 1000f);

            return new()
            {
                move = Vector2.right,
                wishJumping = player.movementModule.motor.GroundingStatus.IsStableOnGround,
                wishDashing = player.movementModule.CanDash,
                wishGroundSlam = player.movementModule.CanGroundSlam,
            };*/

            if (player.State == PlayerState.InLobby)
                return GetLobbyInputs(deltaTime);

            if (player.State == PlayerState.InMatch)
                return GetMatchInputs(deltaTime);

            return new();
        }

        private PlayerMovementInputs GetLobbyInputs(float deltaTime)
        {
            if (!_portal)
                _portal = GameObject.FindGameObjectWithTag("Portal");

            if (!_portal || !_portal.activeInHierarchy) return new();

            LookAt(_portal.transform.position);
            return new()
            {
                move = new(0f, 1f),
            };
        }

        private AIAction _matchAction;
        private LookAtAction _lookAtAction;

        private PlayerMovementInputs GetMatchInputs(float deltaTime)
        {
            if (player.itemModule.item)
            {
                PlayerCore closestPlayer = null;
                var predictedPos = Vector3.zero;
                var closestDistance = 2000f;
                foreach (var (_, otherPlayer) in MapLoader.loadedMap.players)
                {
                    if (otherPlayer.deathModule.Dead || player.teamReference.Unwrap().CompareTeam(otherPlayer.teamReference.Unwrap())) continue;

                    var predict = otherPlayer.hitEntity.Collider.bounds.center + otherPlayer.movementModule.LocalTransientVelocity * deltaTime;
                    if (Physics.Linecast(player.verticalOrientation.position, predict, LayerMask.GetMask("Enviroment")))
                        continue;

                    var distance = Vector3.Distance(predict, transform.position);
                    if (distance < closestDistance)
                    {
                        closestPlayer = otherPlayer;
                        predictedPos = predict;
                        closestDistance = distance;
                    }
                }

                if (closestPlayer)
                {
                    LookAt(predictedPos);
                    player.itemModule.TryUseItem(false);
                }
            }

            var boxes = FindObjectsByType<ItemBox>(FindObjectsSortMode.None);
            (float distance, Vector3 pos)? closestBox = null;
            foreach (var box in boxes)
            {
                var boxPos = box.transform.position - Vector3.up * transform.position.y;
                boxPos.y = Mathf.Pow(Mathf.Abs(boxPos.y), 1.5f) * Mathf.Sign(boxPos.y);

                var distance = Vector3.Distance(player.hitEntity.Collider.bounds.center, boxPos);
                if (!closestBox.HasValue || distance < closestBox.Value.distance)
                {
                    closestBox = (distance, boxPos);
                }
            }

            if (!closestBox.HasValue) return new();

            _lookAtAction.position = closestBox.Value.pos;
            var inputs = new PlayerMovementInputs();
            _matchAction.Execute(ref inputs, deltaTime);
            return inputs;
        }

        private void LookAt(Vector3 position)
        {
            var dir = (position - player.verticalOrientation.position).normalized;
            player.horizontalOrientation.localEulerAngles = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg * Vector3.up;
            player.verticalOrientation.localEulerAngles = Mathf.Asin(dir.y) * Mathf.Rad2Deg * Vector3.left;
        }
    }
}