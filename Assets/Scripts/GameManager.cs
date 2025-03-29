using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Deck Settings")]
    public Deck deck;
    public int cardsPerPlayer = 6;

    [Header("Player Areas")]
    public Transform[] playerHands = new Transform[4];
    public Vector2 cardSpacing = new Vector2(120f, 150f);
    public bool dealFaceDown = true;

    private void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        deck.InitializeDeck();
        deck.Shuffle();
        DealInitialCards();
    }

    private void DealInitialCards()
    {
        for (int playerIndex = 0; playerIndex < playerHands.Length; playerIndex++)
        {
            DealToPlayer(playerHands[playerIndex]);
        }
    }

    private void DealToPlayer(Transform hand)
    {
        for (int i = 0; i < cardsPerPlayer; i++)
        {
            // Get card position in grid (2 rows of 3)
            int row = i < 3 ? 0 : 1;
            int col = i % 3;
            Vector3 position = new Vector3(
                col * cardSpacing.x, 
                -row * cardSpacing.y, 
                0
            );

            // Draw and position card
            Card card = deck.DrawCard(hand);
            card.MoveTo(hand, position);
            
            // Set face up/down based on setting
            if (dealFaceDown)
            {
                card.SetFaceDown();
            }
            else
            {
                card.SetFaceUp();
            }
        }
    }
}