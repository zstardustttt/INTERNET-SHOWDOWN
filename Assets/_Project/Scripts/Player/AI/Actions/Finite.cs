using System;
using Game.Core.Player.Movement;
using UnityEngine;

namespace Game.Player.AI.Actions
{
    public abstract class FiniteAction : AIAction
    {
        public Action<FiniteAction> OnComplete;
        public bool Completed { get; private set; }

        public FiniteAction(AIPlayer player, Action<FiniteAction> onComplete = null) : base(player)
        {
            OnComplete += onComplete;
        }

        public override void Execute(ref PlayerMovementInputs inputs, float deltaTime)
        {
            if (Completed) return;
            FiniteExecute(ref inputs, deltaTime);
        }

        protected abstract void FiniteExecute(ref PlayerMovementInputs inputs, float deltaTime);

        public void Complete()
        {
            if (Completed) return;

            Completed = true;
            OnComplete?.Invoke(this);
        }

        public void Restart()
        {
            Completed = false;
            OnRestart();
        }

        public abstract void OnRestart();
    }

    public class FrameAction : FiniteAction
    {
        public AIAction action;
        public int lifespan;

        private int _frameCounter;

        public FrameAction(AIPlayer player, int lifespan, AIAction action, Action<FiniteAction> onComplete = null) : base(player, onComplete)
        {
            this.action = action;
            this.lifespan = lifespan;
        }

        protected override void FiniteExecute(ref PlayerMovementInputs inputs, float deltaTime)
        {
            action.Execute(ref inputs, deltaTime);
            _frameCounter++;

            if (_frameCounter >= lifespan) Complete();
        }

        public override void OnRestart()
        {
            _frameCounter = 0;
        }
    }

    public class TimeAction : FiniteAction
    {
        public AIAction action;
        public float lifespan;

        private float _timer;

        public TimeAction(AIPlayer player, float lifespan, AIAction action, Action<FiniteAction> onComplete = null) : base(player, onComplete)
        {
            this.action = action;
            this.lifespan = lifespan;
        }

        protected override void FiniteExecute(ref PlayerMovementInputs inputs, float deltaTime)
        {
            action.Execute(ref inputs, deltaTime);

            _timer += deltaTime;
            if (_timer >= lifespan) Complete();
        }

        public override void OnRestart()
        {
            _timer = 0f;
        }
    }

    public class SequencedAction : FiniteAction
    {
        public FiniteAction[] actions;
        public float interval;

        private int _actionIndex;
        private float _intervalTimer;

        public SequencedAction(AIPlayer player, float interval, FiniteAction[] actions, Action<FiniteAction> onComplete = null) : base(player, onComplete)
        {
            this.actions = actions;
            this.interval = interval;

            if (actions.Length == 0)
            {
                Complete();
                return;
            }

            actions[0].OnComplete += OnActionComplete;
        }

        protected override void FiniteExecute(ref PlayerMovementInputs inputs, float deltaTime)
        {
            if (_intervalTimer > 0f)
            {
                _intervalTimer -= deltaTime;
                return;
            }

            actions[_actionIndex].Execute(ref inputs, deltaTime);
        }

        public override void OnRestart()
        {
            _actionIndex = 0;
            _intervalTimer = 0f;

            if (actions.Length == 0)
            {
                Complete();
                return;
            }

            foreach (var action in actions)
            {
                action.Restart();
            }

            actions[0].OnComplete += OnActionComplete;
        }

        private void OnActionComplete(FiniteAction action)
        {
            action.OnComplete -= OnActionComplete;

            _actionIndex++;
            if (_actionIndex >= actions.Length)
            {
                Complete();
                return;
            }

            actions[_actionIndex].OnComplete += OnActionComplete;
            _intervalTimer = interval;
        }
    }

    public class RepeatedAction : FiniteAction
    {
        public FiniteAction action;
        public int count;
        public float interval;

        private int _counter;
        private float _intervalTimer;

        public RepeatedAction(AIPlayer player, int count, float interval, FiniteAction action, Action<FiniteAction> onComplete = null) : base(player, onComplete)
        {
            this.action = action;
            this.count = count;
            this.interval = interval;

            if (count == 0)
            {
                Complete();
                return;
            }

            action.OnComplete += OnActionComplete;
        }

        protected override void FiniteExecute(ref PlayerMovementInputs inputs, float deltaTime)
        {
            if (_intervalTimer > 0f)
            {
                _intervalTimer -= deltaTime;
                return;
            }

            action.Execute(ref inputs, deltaTime);
        }

        public override void OnRestart()
        {
            _counter = 0;
            _intervalTimer = 0f;

            action.Restart();
            action.OnComplete += OnActionComplete;
        }

        private void OnActionComplete(FiniteAction _)
        {
            _counter++;
            if (_counter >= count)
            {
                action.OnComplete -= OnActionComplete;
                Complete();
                return;
            }

            _intervalTimer = interval;
            action.Restart();
        }
    }

    public class ParallelAction : FiniteAction
    {
        public FiniteAction[] actions;

        private int _completedActionsCount;

        public ParallelAction(AIPlayer player, FiniteAction[] actions, Action<FiniteAction> onComplete = null) : base(player, onComplete)
        {
            this.actions = actions;

            foreach (var action in actions)
            {
                action.OnComplete += OnActionComplete;
            }
        }

        private void OnActionComplete(FiniteAction action)
        {
            action.OnComplete -= OnActionComplete;
            _completedActionsCount++;

            if (_completedActionsCount == actions.Length)
                Complete();
        }

        protected override void FiniteExecute(ref PlayerMovementInputs inputs, float deltaTime)
        {
            foreach (var action in actions)
            {
                action.Execute(ref inputs, deltaTime);
            }
        }

        public override void OnRestart()
        {
            _completedActionsCount = 0;

            foreach (var action in actions)
            {
                action.Restart();
                action.OnComplete += OnActionComplete;
            }
        }
    }

    public class WhileAction : FiniteAction
    {
        public AIAction action;
        public Func<bool> predicate;

        public WhileAction(AIPlayer player, Func<bool> predicate, Action<FiniteAction> onComplete = null) : base(player, onComplete)
        {
            this.predicate = predicate;
        }

        public override void OnRestart() { }

        protected override void FiniteExecute(ref PlayerMovementInputs inputs, float deltaTime)
        {
            if (predicate())
            {
                action.Execute(ref inputs, deltaTime);
                return;
            }

            Complete();
        }
    }
}