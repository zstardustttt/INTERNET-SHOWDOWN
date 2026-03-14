using Game.Core.Player.Movement;

namespace Game.Player.AI.Actions
{
    public abstract class AIAction
    {
        public AIPlayer player;

        public AIAction(AIPlayer player)
        {
            this.player = player;
        }

        public abstract void Execute(ref PlayerMovementInputs inputs, float deltaTime);
    }
}