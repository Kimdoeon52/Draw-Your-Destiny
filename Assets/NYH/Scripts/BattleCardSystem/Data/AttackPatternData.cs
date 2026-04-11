namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Data/Battle Attack Pattern")]
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
