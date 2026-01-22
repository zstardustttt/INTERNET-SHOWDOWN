using TMPro;
using UnityEngine;

namespace Game.Projectiles.ShockGerenade
{
    public class ShockGerenadeLocalVisual : MonoBehaviour
    {
        public Transform visual;
        public float toggleTextInterval;
        public TMP_Text displayText;

        private float _toggleTextTimer;

        private void Update()
        {
            _toggleTextTimer += Time.deltaTime;
            if (_toggleTextTimer >= toggleTextInterval)
            {
                displayText.color = new
                (
                    displayText.color.r,
                    displayText.color.g,
                    displayText.color.b,
                    1f - displayText.color.a
                );
                _toggleTextTimer = 0f;
            }
        }
    }
}