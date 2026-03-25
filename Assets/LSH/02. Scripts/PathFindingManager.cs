using System.Collections.Generic;
using UnityEngine;

public class PathFindingManager : MonoBehaviour
{
    public static PathFindingManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal)
    {
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            if (current == goal)
                break;

            foreach (var next in TileMapManager.Instance.GetNeighbors(current))
            {
                if (visited.Contains(next))
                    continue;

                visited.Add(next);
                queue.Enqueue(next);
                cameFrom[next] = current;
            }
        }

        if (!visited.Contains(goal))
            return null;

        List<Vector3Int> path = new List<Vector3Int>();
        Vector3Int currentPath = goal;

        while (currentPath != start)
        {
            path.Add(currentPath);
            currentPath = cameFrom[currentPath];
        }

        path.Reverse();
        return path;
    }
}
