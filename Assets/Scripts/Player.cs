using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public int playerIndex;
    public RectTransform handContainer;
    public List<Card> cardsInHand = new List<Card>();
    
    private GameManager gameManager;
    
    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("No GameManager found in scene!");
        }
    }

    public void AddCardToHand(Card card)
    {
        if (card != null)
        {
            cardsInHand.Add(card);
            card.transform.SetParent(handContainer, false);
            RepositionCards();
        }
    }

    public void RemoveCardFromHand(Card card)
    {
        if (cardsInHand.Contains(card))
        {
            cardsInHand.Remove(card);
            RepositionCards();
        }
    }

    private void RepositionCards()
    {
        // Position cards in the hand based on player's position
        // This is a simplified version; adjust as needed for your layout
        for (int i = 0; i < cardsInHand.Count; i++)
        {
            Card card = cardsInHand[i];
            if (card != null)
            {
                // Position in a grid (2 rows of 3)
                int row = i / 3;
                int col = i % 3;
                
                // Different layouts based on player position
                Vector3 position;
                
                // Bottom player (0) or Top player (2)
                if (playerIndex == 0 || playerIndex == 2)
                {
                    position = new Vector3(
                        col * 120f - 120f, 
                        -row * 150f, 
                        0
                    );
                }
                // Left player (1) or Right player (3)
                else
                {
                    position = new Vector3(
                        0,
                        -col * 150f + 150f, 
                        0
                    );
                }
                
                card.transform.localPosition = position;
            }
        }
    }

    public void RevealAllCards()
    {
        foreach (Card card in cardsInHand)
        {
            if (card != null && !card.IsFaceUp)
            {
                card.SetFaceUp();
            }
        }
    }

    public void UpdateHandLayout()
    {
        RepositionCards();
    }

    public int CalculateScore()
    {
        int score = 0;
        List<int> leftColumnValues = new List<int>();
        List<int> rightColumnValues = new List<int>();
        
        // Separate cards into left column (0, 1, 2) and right column (3, 4, 5)
        for (int i = 0; i < cardsInHand.Count; i++)
        {
            if (i < 3) // Left column
            {
                leftColumnValues.Add(cardsInHand[i].Data.value);
            }
            else // Right column
            {
                rightColumnValues.Add(cardsInHand[i].Data.value);
            }
        }
        
        // Check for pairs in columns (two identical cards in the same column)
        bool leftColumnHasPair = HasPair(leftColumnValues);
        bool rightColumnHasPair = HasPair(rightColumnValues);
        
        // Calculate score based on the game rules
        if (!leftColumnHasPair)
        {
            foreach (int value in leftColumnValues)
            {
                score += value;
            }
        }
        
        if (!rightColumnHasPair)
        {
            foreach (int value in rightColumnValues)
            {
                score += value;
            }
        }
        
        return score;
    }
    
    private bool HasPair(List<int> values)
    {
        for (int i = 0; i < values.Count; i++)
        {
            for (int j = i + 1; j < values.Count; j++)
            {
                if (values[i] == values[j])
                {
                    return true;
                }
            }
        }
        return false;
    }
} 