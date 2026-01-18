using TMPro;
using UnityEngine;

namespace EasyTextEffects.Effects
{
    [CreateAssetMenu(fileName = "Scale", menuName = "Easy Text Effects/4. Scale", order = 4)]
    public class Effect_Scale : TextEffectInstance
    {
        [Space(10)]
        [Header("Scale")]
        public float startScale = 0;
        public float endScale = 1;

        public override void ApplyEffect(TMP_TextInfo _textInfo, int _charIndex, int _startVertex = 0, int _endVertex = 3)
        {
            if (!CheckCanApplyEffect(_charIndex)) return;

            TMP_CharacterInfo charInfo = _textInfo.characterInfo[_charIndex];
            var materialIndex = charInfo.materialReferenceIndex;
            var verts = _textInfo.meshInfo[materialIndex].vertices;
            Vector3 center = CharCenter(charInfo, verts);

            var scale = Interpolate(startScale, endScale, _charIndex);

            for (var v = _startVertex; v <= _endVertex; v++)
            {
                var vertexIndex = charInfo.vertexIndex + v;
                Vector3 fromCenter = verts[vertexIndex] - center;
                verts[vertexIndex] = center + fromCenter * scale;
            }
        }
    }
}