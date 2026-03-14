using Game.Core.Player.Movement;
using UnityEngine;

namespace Game.Player.AI.Actions
{
    public class MoveAction : AIAction
    {
        public Vector2 direction;

        public MoveAction(AIPlayer player, Vector2 direction) : base(player) { this.direction = direction; }

        public override void Execute(ref PlayerMovementInputs inputs, float deltaTime)
        {
            inputs.move = direction;
        }
    }

    public class JumpAction : AIAction
    {
        public bool value;

        public JumpAction(AIPlayer player, bool value) : base(player)
        {
            this.value = value;
        }

        public override void Execute(ref PlayerMovementInputs inputs, float deltaTime)
        {
            inputs.wishJumping = value;
        }
    }

    public class DashAction : AIAction
    {
        public bool value;

        public DashAction(AIPlayer player, bool value) : base(player)
        {
            this.value = value;
        }

        public override void Execute(ref PlayerMovementInputs inputs, float deltaTime)
        {
            inputs.wishDashing = value;
        }
    }

    public class GroundSlamAction : AIAction
    {
        public bool value;

        public GroundSlamAction(AIPlayer player, bool value) : base(player)
        {
            this.value = value;
        }

        public override void Execute(ref PlayerMovementInputs inputs, float deltaTime)
        {
            inputs.wishGroundSlam = value;
        }
    }

    public class LookAtAction : AIAction
    {
        public Vector3 position;
        public LookAtAction(AIPlayer player, Vector3 position) : base(player) { this.position = position; }

        public override void Execute(ref PlayerMovementInputs inputs, float deltaTime)
        {
            var dir = (position - player.player.verticalOrientation.position).normalized;
            player.player.horizontalOrientation.localEulerAngles = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg * Vector3.up;
            player.player.verticalOrientation.localEulerAngles = Mathf.Asin(dir.y) * Mathf.Rad2Deg * Vector3.left;
        }
    }
}