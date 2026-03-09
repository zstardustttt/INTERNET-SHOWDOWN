using Game.Core.Damages;
using Mirror;
using UnityEngine;

namespace Game.Damages
{
    [RequireComponent(typeof(TeamReference))]
    public class OutlineTeamReference : MonoBehaviour
    {
        private static int _friendlyLayer;
        private static int _hostileLayer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            _friendlyLayer = RenderingLayerMask.NameToRenderingLayer("Friendly");
            _hostileLayer = RenderingLayerMask.NameToRenderingLayer("Hostile");
        }

        public TeamReference teamReference;
        public Renderer[] renderers;

        private RenderingLayerMask[] _baseMasks;
        private TeamReference _localTeamReference;

        private void OnValidate()
        {
            teamReference = GetComponent<TeamReference>();
        }

        private void Awake()
        {
            if (!NetworkClient.localPlayer) return;
            _localTeamReference = NetworkClient.localPlayer.GetComponent<TeamReference>();

            _baseMasks = new RenderingLayerMask[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                _baseMasks[i] = renderers[i].renderingLayerMask;
            }

            teamReference.onTeamChanged.AddListener((_, _new) => SetOutlines(_localTeamReference.team.CompareTeam(_new)));
        }

        private void SetOutlines(bool friendly)
        {
            var layer = friendly ? _friendlyLayer : _hostileLayer;
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].renderingLayerMask = _baseMasks[i] | (uint)(1 << layer);
            }
        }
    }
}