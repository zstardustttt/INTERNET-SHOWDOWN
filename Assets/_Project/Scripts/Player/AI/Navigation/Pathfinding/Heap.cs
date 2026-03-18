using System.Runtime.CompilerServices;

namespace Game.Player.AI.Navigation.Pathfinding.Heap
{
    public class PathfindingHeap
    {
        public PathfindingNodeData[] items;
        public int count;

        public PathfindingHeap(int capacity)
        {
            items = new PathfindingNodeData[capacity];
        }

        public void Clear(PathfindingNodeData first)
        {
            count = 1;
            items[0] = first;
            first.heapIndex = 0;
        }

        public void Push(PathfindingNodeData item)
        {
            item.heapIndex = count;
            items[count] = item;
            SortUp(item);
            count++;
        }

        public void SortUp(PathfindingNodeData item)
        {
            var parentIndex = (item.heapIndex - 1) / 2;

            while (true)
            {
                var parentItem = items[parentIndex];
                if (item.CompareTo(parentItem) >= 0)
                    return;

                Swap(item, parentItem);
                parentIndex = (item.heapIndex - 1) / 2;
            }
        }

        public PathfindingNodeData Pop()
        {
            var first = items[0];
            count--;
            items[0] = items[count];
            items[0].heapIndex = 0;
            SortDown(items[0]);
            return first;
        }

        public void SortDown(PathfindingNodeData item)
        {
            while (true)
            {
                var childIndexLeft = item.heapIndex * 2 + 1;
                var childIndexRight = item.heapIndex * 2 + 2;

                int swapIndex;
                if (childIndexLeft < count)
                {
                    if (childIndexRight < count && items[childIndexLeft].CompareTo(items[childIndexRight]) > 0)
                        swapIndex = childIndexRight;
                    else swapIndex = childIndexLeft;

                    if (item.CompareTo(items[swapIndex]) <= 0) return;
                    Swap(item, items[swapIndex]);
                }
                else return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Swap(PathfindingNodeData first, PathfindingNodeData second)
        {
            items[first.heapIndex] = second;
            items[second.heapIndex] = first;
            (second.heapIndex, first.heapIndex) = (first.heapIndex, second.heapIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(PathfindingNodeData item)
        {
            return item.heapIndex != -1 && item.heapIndex < count;
        }
    }
}