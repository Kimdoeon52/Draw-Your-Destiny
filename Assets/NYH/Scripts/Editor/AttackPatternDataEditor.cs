using System.Collections.Generic;
using NYH.BattleCardSystem;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AttackPatternData))]
public class AttackPatternDataEditor : Editor
{
    private const float CellSize = 28f;
    private const float CellGap = 2f;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty patternNameProp = serializedObject.FindProperty("patternName");
        SerializedProperty gridSizeProp = serializedObject.FindProperty("editorGridSize");
        SerializedProperty rotateToFacingProp = serializedObject.FindProperty("rotateToFacing");

        EditorGUILayout.PropertyField(patternNameProp);
        EditorGUILayout.PropertyField(gridSizeProp);
        EditorGUILayout.PropertyField(rotateToFacingProp);

        Vector2Int gridSize = gridSizeProp.vector2IntValue;
        gridSize.x = Mathf.Max(1, gridSize.x);
        gridSize.y = Mathf.Max(1, gridSize.y);
        gridSizeProp.vector2IntValue = gridSize;

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Pattern Grid", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("가운데 칸이 기준점입니다. 클릭해서 공격 범위를 켜고 끌 수 있습니다.", MessageType.Info);

        DrawPatternGrid((AttackPatternData)target, gridSize);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPatternGrid(AttackPatternData patternData, Vector2Int gridSize)
    {
        HashSet<Vector2Int> cells = new(patternData.Cells);
        int originX = gridSize.x / 2;
        int originY = gridSize.y / 2;

        float width = gridSize.x * (CellSize + CellGap);
        float height = gridSize.y * (CellSize + CellGap);
        Rect rect = GUILayoutUtility.GetRect(width, height);

        for (int y = gridSize.y - 1; y >= 0; y--)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                Rect cellRect = new(
                    rect.x + x * (CellSize + CellGap),
                    rect.y + (gridSize.y - 1 - y) * (CellSize + CellGap),
                    CellSize,
                    CellSize);

                Vector2Int offset = new(x - originX, y - originY);
                bool isOrigin = offset == Vector2Int.zero;
                bool isActive = cells.Contains(offset);

                Color previous = GUI.backgroundColor;
                GUI.backgroundColor = isOrigin ? new Color(0.85f, 0.85f, 0.95f) : (isActive ? new Color(0.95f, 0.55f, 0.2f) : new Color(0.2f, 0.2f, 0.2f));

                string label = isOrigin ? "O" : string.Empty;
                if (GUI.Button(cellRect, label))
                {
                    if (!isOrigin)
                    {
                        if (isActive) cells.Remove(offset);
                        else cells.Add(offset);

                        Undo.RecordObject(patternData, "Toggle Attack Pattern Cell");
                        patternData.SetCells(cells);
                        EditorUtility.SetDirty(patternData);
                    }
                }

                GUI.backgroundColor = previous;
            }
        }
    }
}
