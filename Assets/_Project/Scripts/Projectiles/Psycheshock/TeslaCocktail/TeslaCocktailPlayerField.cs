using Game.Core.Player;
using Mirror;

namespace Game.Projectiles.Psycheshock.TeslaCocktail
{
    public class TeslaCocktailPlayerField : NetworkBehaviour
    {
        public PlayerCore player;

        public override void OnStartServer()
        {
            if (player) player.deathModule.onDeath.AddListener(OnDeath);
        }

        private void Update()
        {
            if (!NetworkServer.active || !player) return;
            transform.position = player.transform.position + player.movementModule.motor.Capsule.center;
        }

        private void OnDeath()
        {
            player.deathModule.onDeath.RemoveListener(OnDeath);
            NetworkServer.Destroy(gameObject);
        }
    }
}