using Game.Player;
using Mirror;

namespace Game.Projectiles
{
    public class TeslaCocktailPlayerField : NetworkBehaviour
    {
        public PlayerBase player;
        private bool _addedCallback;

        private void Update()
        {
            if (!NetworkServer.active || !player) return;

            if (!_addedCallback)
            {
                player.onResetPlayer.AddListener(OnResetPlayer);
                _addedCallback = true;
            }

            transform.position = player.transform.position + player.motor.Capsule.center;
        }

        private void OnResetPlayer()
        {
            NetworkServer.Destroy(gameObject);
        }
    }
}