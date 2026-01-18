namespace EasyTextEffects
{
    public enum TextEffectType
    {
        Tag,
        Global
    }

    public class TextEffectStatus
    {
        public string Tag;
        /// <summary>
        /// true if the effect has started, false if it's stopped or haven't started yet
        /// stays true even if the effect has completed
        /// works for every animation type
        /// </summary>
        public bool Started;

        /// <summary>
        /// true if the effect has completed or stopped
        /// works for OneTime and LoopFixedDuration animations
        /// </summary>
        public bool IsComplete;
    }
}