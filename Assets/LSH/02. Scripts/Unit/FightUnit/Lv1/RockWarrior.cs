using UnityEngine;
using Base;
public class RockWarrior : HumanBase, IFightable
{
    [Header("최대체력")]
    [SerializeField] private int maxHealth;
    [Header("현재체력")]
    [SerializeField] int currentHealth;
    [Header("공격력")]
    [SerializeField] int attackPower;

    protected override void OnEnable()
    {//무난한 일반 병사
        base.OnEnable();
        maxHealth = 150;
        attackPower = 20;
        moveSpeed = 3f;
        SetupHealth();
        unitTypeBase = UnitTypeBase.RockWarrior;
    }
    protected override void Update()
    {
        base.Update();
        if (currentHealth <= 0)
        {
            Dead();
        }
        if (Input.GetKeyDown(KeyCode.S)) //임시 공격받았음
        {
            TakeDamage(10);
        }
    }
    public void Attack(int targetID)
    {

    }

    public void SetupHealth()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log("공격 받음!");
    }
    protected override void Dead()
    {
        base.Dead();
    }
    ////======================================일단 임시 전투용 이동 실험 함수들=======================================

    //private void OnMouseDown()
    //{
    //    isSelected = true;
    //    if (isSelected)
    //    {
    //        Debug.Log("유닛 선택됨");
    //    }
    //    else
    //    {
    //        Debug.Log("유닛 선택 해제됨");
    //    }
    //}
    //public void Move(Vector3Int targetCell) //임시로 만들어둔 움직이는 함수 일단 클릭하고 옆 타일로 이동하게 만들었음
    //{
    //    if(!isSelected) return; //유닛이 선택되지 않았으면 이동하지 않음
    //    Vector3Int currentCell = TileMapManager.Instance.groundTilemap.WorldToCell(transform.position); //현재 위치 셀 좌표 넣기
    //    int dx = Mathf.Abs(targetCell.x - currentCell.x);
    //    int dy = Mathf.Abs(targetCell.y - currentCell.y);
    //    if (dx + dy != 1) //한 칸 만 이동할 수 있게
    //    {
    //        Debug.Log("한 칸만 이동 가능");
    //        return;
    //    }
    //    Vector3 targetWorld = TileMapManager.Instance.groundTilemap.GetCellCenterWorld(targetCell);//타일 중앙으로 이동하게
    //    transform.position = targetWorld;

    //    isSelected = false; // 이동 후 선택 해제
    //}

}
