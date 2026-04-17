using NYH.CoreCardSystem;
using UnityEngine;

/// <summary>
/// 마우스를 카드 위에 올렸을 때 큰 미리보기를 보여주는 시스템입니다.
/// </summary>
public class CardViewHoverSystem : Singleton<CardViewHoverSystem>
{
    [Header("Preview UI")]
    [SerializeField] private CardView civilizationCardViewHover;
    [SerializeField] private CardView battleCardViewHover;
    [SerializeField] private CardView cardViewHover;

    protected override void Awake()
    {
        base.Awake();
        PrepareHoverView(civilizationCardViewHover);
        PrepareHoverView(battleCardViewHover);
        if (cardViewHover != null
            && cardViewHover != civilizationCardViewHover
            && cardViewHover != battleCardViewHover)
        {
            PrepareHoverView(cardViewHover);
        }
    }

    public void Show(Card card, Vector3 position)
    {
        CardView activeHoverView = ResolveHoverView(card);
        if (activeHoverView == null)
        {
            return;
        }

        HideInactiveHoverViews(activeHoverView);

        activeHoverView.gameObject.SetActive(true);
        activeHoverView.Setup(card);
        activeHoverView.transform.SetAsLastSibling();

        var graphics = activeHoverView.GetComponentsInChildren<UnityEngine.UI.Graphic>();
        foreach (var graphic in graphics)
        {
            graphic.raycastTarget = false;
        }

        RectTransform rect = activeHoverView.GetComponent<RectTransform>();
        float halfWidth = (rect.rect.width * rect.lossyScale.x) / 2f;
        float halfHeight = (rect.rect.height * rect.lossyScale.y) / 2f;

        Vector3 targetPos = position;
        float minX = halfWidth;
        float maxX = Screen.width - halfWidth;
        float minY = halfHeight;
        float maxY = Screen.height - halfHeight;

        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

        activeHoverView.transform.position = targetPos;
    }

    public void Hide()
    {
        HideHoverView(civilizationCardViewHover);
        HideHoverView(battleCardViewHover);
        HideHoverView(cardViewHover);
    }

    private CardView ResolveHoverView(Card card)
    {
        CardVisualKind visualKind = card?.PresentationData != null
            ? card.PresentationData.VisualKind
            : CardVisualKind.Civilization;

        return visualKind switch
        {
            CardVisualKind.Battle => battleCardViewHover != null ? battleCardViewHover : cardViewHover,
            _ => civilizationCardViewHover != null ? civilizationCardViewHover : cardViewHover,
        };
    }

    private void HideInactiveHoverViews(CardView activeHoverView)
    {
        if (civilizationCardViewHover != null && civilizationCardViewHover != activeHoverView)
        {
            civilizationCardViewHover.gameObject.SetActive(false);
        }

        if (battleCardViewHover != null && battleCardViewHover != activeHoverView)
        {
            battleCardViewHover.gameObject.SetActive(false);
        }

        if (cardViewHover != null && cardViewHover != activeHoverView)
        {
            cardViewHover.gameObject.SetActive(false);
        }
    }

    private static void PrepareHoverView(CardView hoverView)
    {
        if (hoverView == null)
        {
            return;
        }

        hoverView.IsHoverPreview = true;
        hoverView.gameObject.SetActive(false);
    }

    private static void HideHoverView(CardView hoverView)
    {
        if (hoverView != null)
        {
            hoverView.gameObject.SetActive(false);
        }
    }
}
