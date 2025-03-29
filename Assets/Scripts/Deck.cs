using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
    public class DragonCardType
    {
        public int value; // Actual value (-2 to 8)
        public Sprite frontSprite;
        public string displayName; // Custom name for each value
    }

public class Deck : MonoBehaviour
{
    [Header("Card Assets")]
    public Card cardPrefab;
    public Sprite[] dragonFrontSprites;
    public Sprite[] crowFrontSprites;
    public Sprite cardBackSprite;
    [Header("Dragon Configurations")]
    public DragonCardType[] dragonCards = new DragonCardType[]
    {
        new DragonCardType { value = -2, displayName = "Ancient Dragon" },
        new DragonCardType { value = 0, displayName = "Dragonling" },
        new DragonCardType { value = 1, displayName = "Fire Drake" },
        new DragonCardType { value = 2, displayName = "Frost Wyrm" },
        new DragonCardType { value = 3, displayName = "Volcanic Dragon" },
        new DragonCardType { value = 4, displayName = "Thunder Drake" },
        new DragonCardType { value = 5, displayName = "Shadow Dragon" },
        new DragonCardType { value = 6, displayName = "Golden Wyvern" },
        new DragonCardType { value = 7, displayName = "Emerald Serpent" },
        new DragonCardType { value = 8, displayName = "Celestial Dragon" }
    };


    private List<CardData> _cards = new List<CardData>();

    private void Start()
    {
        InitializeDeck();
        Shuffle();
    }

     public void InitializeDeck()
    {
        _cards.Clear();
        
        // Initialize all dragon cards (including -2)
        InitializeDragons();
        
        // Initialize all crow cards (3 types)
        InitializeCrows();
    }

    private void InitializeDragons()
    {
        // First validate we have enough sprites
        if (dragonFrontSprites.Length < dragonCards.Length)
        {
            Debug.LogError($"Need {dragonCards.Length} dragon sprites, got {dragonFrontSprites.Length}");
            return;
        }

        for (int i = 0; i < dragonCards.Length; i++)
        {
            var dragonType = dragonCards[i];
            
            // Create 4 copies of each dragon card
            for (int j = 0; j < 4; j++)
            {
                _cards.Add(new CardData
                {
                    type = CardType.Dragon,
                    value = dragonType.value,
                    cardName = dragonType.displayName,
                    frontSprite = dragonFrontSprites[i],
                    backSprite = cardBackSprite
                });
            }
        }
    }

    private void InitializeCrows()
    {
        // Crow types with their special abilities
        string[] crowNames = { "Scout Crow", "Thief Crow", "Mirror Crow" };
        
        if (crowFrontSprites.Length < crowNames.Length)
        {
            Debug.LogError($"Need {crowNames.Length} crow sprites, got {crowFrontSprites.Length}");
            return;
        }

        for (int type = 0; type < crowNames.Length; type++)
        {
            // Create 4 copies of each crow card
            for (int i = 0; i < 4; i++)
            {
                _cards.Add(new CardData
                {
                    type = CardType.Crow,
                    value = type + 9, // Values 9, 10, 11
                    cardName = crowNames[type],
                    frontSprite = crowFrontSprites[type],
                    backSprite = cardBackSprite
                });
            }
        }
    }

    public void Shuffle()
    {
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
    }

    public Card DrawCard(Transform parent)
    {
        if (_cards.Count == 0)
        {
            Debug.LogWarning("Deck is empty!");
            return null;
        }

        CardData data = _cards[0];
        _cards.RemoveAt(0);
        
        Card card = Instantiate(cardPrefab, parent);
        card.Initialize(data);
        return card;
    }

    public int RemainingCards => _cards.Count;
}