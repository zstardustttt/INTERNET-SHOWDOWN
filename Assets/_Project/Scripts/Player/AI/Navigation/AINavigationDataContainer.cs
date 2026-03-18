using System.Collections.Generic;
using UnityEngine;

namespace Game.Player.AI.Navigation
{
    public class AINavigationDataContainer : MonoBehaviour
    {
        public NavigationNode[] bakedNavigationNodes;
        public NavigationNodesChunk[] bakedNavigationNodesChunks;

        public Dictionary<Vector3Int, NavigationNodesChunk> NavigationNodesChunkMap { get; private set; }

        public void MapNavigationNodesChunks()
        {
            if (bakedNavigationNodesChunks == null)
            {
                Debug.LogError("There are no information about navigation nodes chunks!");
                return;
            }

            if (NavigationNodesChunkMap == null) NavigationNodesChunkMap = new();
            else NavigationNodesChunkMap.Clear();

            foreach (var chunk in bakedNavigationNodesChunks)
            {
                NavigationNodesChunkMap.Add(chunk.chunkCenter, chunk);
            }
        }
    }
}