using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public enum CardType { Dragon, Crow }

[System.Serializable]
public class CardData
{
    public CardType type;
    public int value;
    public string cardName;
    public Sprite frontSprite;
    public Sprite backSprite;
    
    public string Description => type == CardType.Dragon 
        ? $"Dragon card with value {value}" 
        : $"Crow card with special ability {value}";
}

[RequireComponent(typeof(Image), typeof(Button), typeof(CanvasGroup))]
public class Card : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Image cardImage;
    [SerializeField] private Button cardButton;
    [SerializeField] private CanvasGroup canvasGroup;
    
    private CardData _data;
    private bool _isFaceUp;
    private bool _isInteractable = true;
    private Vector3 _originalScale;
    
    public CardData Data => _data;
    public bool IsFaceUp => _isFaceUp;
    
    private void Awake()
    {
        _originalScale = transform.localScale;
        cardButton.onClick.AddListener(OnClick);
    }

    public void Initialize(CardData cardData)
    {
        _data = cardData;
        SetFaceDown();
    }

    public void SetFaceUp()
    {
        if (_isFaceUp) return;
        Flip();
    }

    public void SetFaceDown()
    {
        if (!_isFaceUp) return;
        Flip();
    }

    public void Flip()
    {
        _isFaceUp = !_isFaceUp;
        UpdateAppearance();
    }

    public void MoveTo(Transform parent, Vector3 localPosition)
    {
        transform.SetParent(parent);
        transform.localPosition = localPosition;
    }

    public void SetInteractable(bool interactable)
    {
        _isInteractable = interactable;
        canvasGroup.blocksRaycasts = interactable;
        canvasGroup.alpha = interactable ? 1f : 0.7f;
    }

    private void UpdateAppearance()
    {
        cardImage.sprite = _isFaceUp ? _data.frontSprite : _data.backSprite;
        cardImage.preserveAspect = true;
    }

    private void OnClick()
    {
        if (_isInteractable && !_isFaceUp)
        {
            Flip();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isInteractable) transform.localScale = _originalScale * 1.05f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = _originalScale;
    }
}