using Game.Core.Damages;
using Game.Core.Events;
using Game.Player.Online;
using Game.Player.Online.Events;
using Mirror;
using UnityEngine;

namespace Game.Damages
{
    [RequireComponent(typeof(TeamReference))]
    public class OutlineTeamReference : MonoBehaviour
    {
        private static int _friendlyLayer;
        private static int _hostileLayer;
        private static int _transparentOutlinesLayer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            _friendlyLayer = RenderingLayerMask.NameToRenderingLayer("Friendly");
            _hostileLayer = RenderingLayerMask.NameToRenderingLayer("Hostile");
            _transparentOutlinesLayer = RenderingLayerMask.NameToRenderingLayer("TransparentOutlines");
        }

        public TeamReference teamReference;
        public Renderer[] renderers;
        public bool transparent;

        private RenderingLayerMask[] _baseMasks;

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            teamReference = GetComponent<TeamReference>();
        }

        private void Start()
        {
            if (!NetworkClient.active) return;

            _baseMasks = new RenderingLayerMask[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                _baseMasks[i] = renderers[i].renderingLayerMask;
            }

            EventBus<OnLocalPlayerStarted>.Listen((_) => UpdateOutlines());
            teamReference.onTeamChanged.AddListener((_, _new) => UpdateOutlines());

            UpdateOutlines();
        }

        private void UpdateOutlines()
        {
            if (!OnlinePlayer.localPlayer) return;
            SetOutlines(OnlinePlayer.localPlayer.player.teamReference.team.CompareTeam(teamReference.team));
        }

        private void SetOutlines(bool friendly)
        {
            var layer = friendly ? _friendlyLayer : _hostileLayer;
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                renderer.renderingLayerMask = _baseMasks[i] | (uint)(1 << layer);
                if (transparent) renderer.renderingLayerMask |= (uint)(1 << _transparentOutlinesLayer);
            }
        }
    }
}