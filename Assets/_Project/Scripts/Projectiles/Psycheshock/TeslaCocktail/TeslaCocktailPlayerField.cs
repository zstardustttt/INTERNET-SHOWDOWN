using Game.Player;
using Mirror;

namespace Game.Projectiles.Psycheshock.TeslaCocktail
{
    public class TeslaCocktailPlayerField : NetworkBehaviour
    {
        public PlayerBase player;

        public override void OnStartServer()
        {
            if (player) player.onDeath.AddListener(OnDeath);
        }

        private void Update()
        {
            if (!NetworkServer.active || !player) return;
            transform.position = player.transform.position + player.motor.Capsule.center;
        }

        private void OnDeath()
        {
            player.onDeath.RemoveListener(OnDeath);
            NetworkServer.Destroy(gameObject);
        }
    }
}