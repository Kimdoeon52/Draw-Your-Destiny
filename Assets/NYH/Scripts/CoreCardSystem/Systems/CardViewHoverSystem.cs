using NYH.CoreCardSystem;
using UnityEngine;

/// <summary>
/// 마우스를 카드 위에 올렸을 때 큰 미리보기를 보여주는 시스템입니다.
/// </summary>
public class CardViewHoverSystem : Singleton<CardViewHoverSystem>
{
    [Header("Preview UI")]
    [SerializeField] private CardView cardViewHover;

    protected override void Awake()
    {
        base.Awake();
        if (cardViewHover != null)
        {
            cardViewHover.IsHoverPreview = true;
            cardViewHover.gameObject.SetActive(false);
        }
    }

    public void Show(Card card, Vector3 position)
    {
        if (cardViewHover == null)
        {
            return;
        }

        cardViewHover.gameObject.SetActive(true);
        cardViewHover.Setup(card);
        cardViewHover.transform.SetAsLastSibling();

        var graphics = cardViewHover.GetComponentsInChildren<UnityEngine.UI.Graphic>();
        foreach (var graphic in graphics)
        {
            graphic.raycastTarget = false;
        }

        RectTransform rect = cardViewHover.GetComponent<RectTransform>();
        float halfWidth = (rect.rect.width * rect.lossyScale.x) / 2f;
        float halfHeight = (rect.rect.height * rect.lossyScale.y) / 2f;

        Vector3 targetPos = position;
        float minX = halfWidth;
        float maxX = Screen.width - halfWidth;
        float minY = halfHeight;
        float maxY = Screen.height - halfHeight;

        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

        cardViewHover.transform.position = targetPos;
    }

    public void Hide()
    {
        if (cardViewHover != null)
        {
            cardViewHover.gameObject.SetActive(false);
        }
    }
}
