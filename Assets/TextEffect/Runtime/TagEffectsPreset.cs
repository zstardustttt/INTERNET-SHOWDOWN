using System.Collections.Generic;
using UnityEngine;

namespace EasyTextEffects
{
    [CreateAssetMenu(fileName = "TagEffectsPreset", menuName = "Easy Text Effects/Tag Effects Preset")]
    public class TagEffectsPreset : ScriptableObject
    {
        public List<TextEffectEntry> tagEffects = new List<TextEffectEntry>();
    }
}