using UnityEngine;

namespace Game.Player.Effects
{
    [CreateAssetMenu(fileName = "PlayerEffectsConfig", menuName = "Player/Player Effects Config", order = 0)]
    public class PlayerEffectsConfig : ScriptableObject
    {
        [Header("Objects")]
        public GameObject jumpPrimaryAudioSource;
        public GameObject dashAudioSource;
        public GameObject groundSlamAudioSource;
        public GameObject landAudioSource;
        public GameObject wallSlideAudioSource;
        public GameObject skidAudioSource;
        public GameObject respawnAudioSource;

        [Header("Wall Slide")]
        public float wallSlideVolume;
        public float wallSlideVolumeSmoothingSpeed;
        public float wallSlideVolumeIncreaseRate;

        [Header("Skids")]
        public int velocityRecordSize;
        public float skidThreshold;
    }
}