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
    private GameManager _gameManager;
    
    [Header("Card Display Settings")]
    [SerializeField] private bool maintainAspectRatio = true;
    [SerializeField] private bool useUniformScale = true;
    public Vector2 fixedCardSize = new Vector2(100f, 150f);
    
    private CardData _data;
    private bool _isFaceUp;
    private bool _isInteractable = true;
    private Vector3 _originalScale;
    private int _playerIndex = -1;
    
    public CardData Data => _data;
    public bool IsFaceUp => _isFaceUp;
    public int PlayerIndex => _playerIndex;
    
    public void SetPlayerIndex(int newPlayerIndex)
    {
        _playerIndex = newPlayerIndex;
    }

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

    public void Initialize(CardData cardData, GameManager gameManager, int playerIndex)
    {
        _data = cardData;
        _gameManager = gameManager;
        _playerIndex = playerIndex;
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
        // Debug.Log($"Card {Data?.cardName} (Instance ID: {GetInstanceID()}) SetInteractable({interactable}): Called. Current alpha before change: {canvasGroup?.alpha}", gameObject);
        _isInteractable = interactable;
        if (cardButton != null) cardButton.interactable = interactable;
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = interactable;
            canvasGroup.alpha = 1f; // Always fully opaque
            // Debug.Log($"Card {Data?.cardName} (Instance ID: {GetInstanceID()}) SetInteractable({interactable}): Alpha set to {canvasGroup.alpha}", gameObject);
        }
        // else
        // {
        //     Debug.LogError($"Card {Data?.cardName} (Instance ID: {GetInstanceID()}) SetInteractable({interactable}): canvasGroup is NULL!", gameObject);
        // }
    }

    public void ConfigureForDiscardPile()
    {
        // Debug.Log($"Card {Data?.cardName} (Instance ID: {GetInstanceID()}) ConfigureForDiscardPile: Called. Current alpha before change: {canvasGroup?.alpha}", gameObject);
        _isInteractable = false;
        if (cardButton != null) cardButton.interactable = false;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 1f; // Keep fully opaque
            // Debug.Log($"Card {Data?.cardName} (Instance ID: {GetInstanceID()}) ConfigureForDiscardPile: Alpha set to {canvasGroup.alpha}", gameObject);
        }
        // else
        // {
        //     Debug.LogError($"Card {Data?.cardName} (Instance ID: {GetInstanceID()}) ConfigureForDiscardPile: canvasGroup is NULL!", gameObject);
        // }
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
        if (!_isInteractable || _gameManager == null)
        {
            if (_gameManager == null)
            {
                Debug.LogError("GameManager reference not set on Card.");
            }
            return;
        }

        // Check for swap condition first if this card is a hand card
        if (PlayerIndex != -1 && _gameManager.CanSwapWithRevealedCard(this))
        {
            _gameManager.PerformSwap(this);
        }
        // Check if this card IS the revealedDeckCard itself
        else if (_gameManager.IsRevealedDeckCard(this))
        {
             _gameManager.HandleClickedRevealedDeckCard(this);
        }
        // NEW: Check if this card is a clickable top card of a discard pile
        else if (PlayerIndex == -1 && _gameManager.IsTopDiscardCard(this, out int pileIndex)) 
        {
             _gameManager.HandleDiscardPileCardSelected(this, pileIndex);
        }
        // Otherwise, it's likely a click on a hand card not for a swap,
        // or an invalid click. GameManager.HandleCardClick will sort it out.
        else 
        {
            _gameManager.HandleCardClick(this);
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