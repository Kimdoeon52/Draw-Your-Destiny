namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(menuName = "CardData/Battle Attack Pattern")]
    /*
     * AttackPatternData
     *
     * 역할:
     * - 커스텀 공격/회복 범위 모양을 ScriptableObject 자산으로 저장합니다.
     * - cells는 패턴 원점 기준 상대 좌표이며, resolver가 카드 방향에 맞춰 회전/배치합니다.
     */
    public class AttackPatternData : ScriptableObject
    {
        [SerializeField] private string patternName;
        [SerializeField] private Vector2Int editorGridSize = new(10, 10);
        [SerializeField] private bool rotateToFacing = true;
        [SerializeField] private List<Vector2Int> cells = new();

        public string PatternName => patternName;
        public Vector2Int EditorGridSize => editorGridSize;
        public bool RotateToFacing => rotateToFacing;
        public IReadOnlyList<Vector2Int> Cells => cells;

        public void SetCells(IEnumerable<Vector2Int> source)
        {
            cells.Clear();
            if (source == null)
            {
                return;
            }

            foreach (var cell in source)
            {
                if (!cells.Contains(cell))
                {
                    cells.Add(cell);
                }
            }
        }
    }
}
