using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System;

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
    
    [Header("Card Display Settings")]
    [SerializeField] private bool maintainAspectRatio = true;
    [SerializeField] private bool useUniformScale = true;
    public Vector2 fixedCardSize = new Vector2(100f, 150f);
    
    private CardData _data;
    private bool _isFaceUp;
    private bool _isInteractable = true;
    private Vector3 _originalScale;
    
    public CardData Data => _data;
    public bool IsFaceUp => _isFaceUp;
    
    public event Action<Card> OnCardFlipped;
    
    private void Awake()
    {
        _originalScale = transform.localScale;
        
        // Initialize references if not already set in inspector
        if (cardImage == null) cardImage = GetComponent<Image>();
        if (cardButton == null) cardButton = GetComponent<Button>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        
        SetupCardDisplay();
        
        cardButton.onClick.AddListener(OnClick);
    }

    public void SetupCardDisplay()
    {
        // Set fixed size for the card's RectTransform
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = fixedCardSize;
        }
        
        // Configure the image
        if (cardImage != null)
        {
            cardImage.preserveAspect = maintainAspectRatio;
            
            // Set the image to fill the card while respecting aspect ratio if needed
            if (useUniformScale)
            {
                cardImage.SetNativeSize();
                float widthRatio = fixedCardSize.x / cardImage.preferredWidth;
                float heightRatio = fixedCardSize.y / cardImage.preferredHeight;
                float uniformScale = Mathf.Min(widthRatio, heightRatio);
                
                RectTransform imageRect = cardImage.GetComponent<RectTransform>();
                imageRect.sizeDelta = new Vector2(
                    cardImage.preferredWidth * uniformScale,
                    cardImage.preferredHeight * uniformScale
                );
                
                // Center the image
                imageRect.anchoredPosition = Vector2.zero;
            }
        }
    }

    public void Initialize(CardData cardData)
    {
        _data = cardData;
        SetFaceDown();
        
        // Apply consistent sizing when card is initialized
        if (cardImage != null)
        {
            UpdateAppearance();
            SetupCardDisplay();
        }
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
        OnCardFlipped?.Invoke(this);
    }

    public void MoveTo(Transform parent, Vector3 localPosition)
    {
        transform.SetParent(parent);
        transform.localPosition = localPosition;
    }

    public void SetInteractable(bool interactable)
    {
        _isInteractable = interactable;
        cardButton.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
        canvasGroup.alpha = interactable ? 1f : 0.7f;
    }

    private void UpdateAppearance()
    {
        cardImage.sprite = _isFaceUp ? _data.frontSprite : _data.backSprite;
        cardImage.preserveAspect = maintainAspectRatio;
        
        // Reapply uniform scaling whenever the sprite changes
        if (useUniformScale)
        {
            cardImage.SetNativeSize();
            float widthRatio = fixedCardSize.x / cardImage.preferredWidth;
            float heightRatio = fixedCardSize.y / cardImage.preferredHeight;
            float uniformScale = Mathf.Min(widthRatio, heightRatio);
            
            RectTransform imageRect = cardImage.GetComponent<RectTransform>();
            imageRect.sizeDelta = new Vector2(
                cardImage.preferredWidth * uniformScale,
                cardImage.preferredHeight * uniformScale
            );
            
            // Center the image
            imageRect.anchoredPosition = Vector2.zero;
        }
    }

    private void OnClick()
    {
        if (_isInteractable)
        {
            Flip();
            Debug.Log($"Card flipped: {_data.cardName}, Value: {_data.value}, Face up: {_isFaceUp}");
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