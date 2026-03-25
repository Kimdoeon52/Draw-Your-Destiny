using System.Collections.Generic;
using UnityEngine;

public class PathFindingManager : MonoBehaviour
{
    public static PathFindingManager Instance;
    private void Awake()
    {
        Instance = this;
    }

    // start → goal 까지 갈 수 있는 경로를 찾는 함수
    // BFS (너비 우선 탐색) 방식
    public List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal)
    {
        // 다음에 탐색할 타일을 저장하는 큐
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        // "어떤 타일이 어디에서 왔는지" 기록
        // 나중에 경로를 역추적할 때 사용
        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        // 이미 방문한 타일 기록
        // 같은 타일을 여러 번 탐색하지 않기 위해 사용
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        // 시작 타일을 탐색 시작점으로 등록
        queue.Enqueue(start); // 큐에 시작 타일 추가
        visited.Add(start); // 방문 기록
        // 큐에 탐색할 타일이 있는 동안 반복
        while (queue.Count > 0)
        {
            // 큐에서 다음 탐색할 타일 꺼내기
            Vector3Int current = queue.Dequeue();
            // 목표 타일에 도달했으면 탐색 종료
            if (current == goal)
                break;

            // 현재 타일 주변 이웃 타일 검사
            // TileMapManager에서 이동 가능한 이웃 타일 가져오기
            foreach (var next in TileMapManager.Instance.GetNeighbors(current))
            {
                // 이미 방문한 타일이면 무시
                if (visited.Contains(next))
                    continue;
                // 방문 처리
                visited.Add(next);
                // 큐에 추가 (나중에 탐색할 타일)
                queue.Enqueue(next);
                // 이 타일이 어디에서 왔는지 기록
                // next는 current에서 왔다
                cameFrom[next] = current;
            }
        }

        // 목표 타일에 도달하지 못한 경우
        if (!visited.Contains(goal))
            return null; // 길이 없음

        // 경로 역추적 시작
        // goal → start 방향으로 거꾸로 따라가면서 경로 생성
        List<Vector3Int> path = new List<Vector3Int>();
        Vector3Int currentPath = goal;

        // start에 도달할 때까지 반복
        while (currentPath != start)
        {
            // 현재 타일을 경로에 추가
            path.Add(currentPath);
            // 이전 타일로 이동
            currentPath = cameFrom[currentPath];
        }

        // 현재 path는 goal → start 순서라서
        // start → goal 순서로 뒤집는다
        path.Reverse();
        // 완성된 경로 반환
        return path;
    }
}
