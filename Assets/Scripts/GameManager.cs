using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public enum GameState { Setup, PlayerTurn, GameEnd }

public class GameManager : MonoBehaviour
{
    [Header("Deck Settings")]
    public Deck deck;
    public int cardsPerPlayer = 6;

    [Header("Player Areas")]
    public RectTransform[] playerHands = new RectTransform[4];
    public Vector2 cardSpacing = new Vector2(120f, 150f);
    public bool dealFaceDown = true;

    [Header("Game UI")]
    public Button deckButton;
    public Transform[] discardPiles;
    public Text currentPlayerText;
    public Button endTurnButton;
    
    [Header("Card Size Settings")]
    public Vector2 handCardSize = new Vector2(100f, 150f);
    public Vector2 discardCardSize = new Vector2(50f, 68f);
    public float cardDealDelay = 0.2f;
    
    // Array of Player script references
    private Player[] players;
    private List<Card>[] playerCards;
    private int currentPlayerTurn = 0;
    private GameState gameState = GameState.Setup;
    private Card[] discardPileTopCards;

    private void Start()
    {
        // Ensure we have the deck and player areas
        if (deck == null)
        {
            deck = FindObjectOfType<Deck>();
            if (deck == null)
            {
                Debug.LogError("No Deck found in the scene!");
                return;
            }
        }
        
        // Initialize player objects
        InitializePlayers();
        
        // Set up UI buttons
        if (deckButton != null)
        {
            deckButton.onClick.AddListener(OnDeckButtonClicked);
        }
        
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(NextTurn);
            endTurnButton.gameObject.SetActive(false);  // Hide until game starts
        }
        
        // Initialize discard pile tracking
        if (discardPiles != null && discardPiles.Length > 0)
        {
            discardPileTopCards = new Card[discardPiles.Length];
        }

        InitializeGame();
    }
    
    private void InitializePlayers()
    {
        players = new Player[playerHands.Length];
        
        for (int i = 0; i < playerHands.Length; i++)
        {
            // Check if player component exists, otherwise add it
            Player existingPlayer = playerHands[i].GetComponent<Player>();
            if (existingPlayer == null)
            {
                // Create new player component
                Player player = playerHands[i].gameObject.AddComponent<Player>();
                player.playerIndex = i;
                player.handContainer = playerHands[i];
                players[i] = player;
            }
            else
            {
                // Use existing player component
                existingPlayer.playerIndex = i;
                existingPlayer.handContainer = playerHands[i];
                players[i] = existingPlayer;
            }
        }
    }

    private void InitializeGame()
    {
        // Initialize player card lists
        playerCards = new List<Card>[playerHands.Length];
        for (int i = 0; i < playerHands.Length; i++)
        {
            playerCards[i] = new List<Card>();
            
            // Clear player's hand
            players[i].cardsInHand.Clear();
        }

        // Setup the deck
        deck.InitializeDeck();
        deck.Shuffle();
        
        // Clear any existing cards in player areas
        foreach (Transform hand in playerHands)
        {
            foreach (Transform child in hand)
            {
                if (child.GetComponent<Card>() != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        
        // Clear discard piles
        if (discardPiles != null)
        {
            foreach (Transform discardPile in discardPiles)
            {
                foreach (Transform child in discardPile)
                {
                    Destroy(child.gameObject);
                }
            }
            
            // Reset discard pile tracking
            for (int i = 0; i < discardPileTopCards.Length; i++)
            {
                discardPileTopCards[i] = null;
            }
        }
        
        // Deal initial cards to players
        StartCoroutine(DealInitialCards());
    }

    private IEnumerator DealInitialCards()
    {
        gameState = GameState.Setup;
        
        // Deal cards with animation delay
        for (int cardIndex = 0; cardIndex < cardsPerPlayer; cardIndex++)
        {
            for (int playerIndex = 0; playerIndex < players.Length; playerIndex++)
            {
                yield return new WaitForSeconds(cardDealDelay);
                DealCardToPlayer(playerIndex, cardIndex);
            }
        }
        
        // Initialize discard piles if needed
        if (discardPiles != null && discardPiles.Length >= 2)
        {
            for (int i = 0; i < 2; i++) // Initialize 2 discard piles
            {
                yield return new WaitForSeconds(cardDealDelay);
                var (discardCard, discardCardData) = deck.DrawCard(discardPiles[i]);
                
                if (discardCard != null && discardCardData != null)
                {
                    // Initialize the discard card (belongs to no player initially, use index -1)
                    discardCard.Initialize(cardData: discardCardData, gameManager: this, playerIndex: -1);
                    
                    discardCard.SetFaceUp();
                    SetCardSize(discardCard, discardCardSize);
                    discardPileTopCards[i] = discardCard;
                }
            }
        }
        
        // Set current player (random for first game)
        // currentPlayerTurn = Random.Range(0, players.Length);
        // Set current player to Player 1 (index 0)
        currentPlayerTurn = 0;
        UpdateCurrentPlayerUI();
        
        // Start the game
        gameState = GameState.PlayerTurn;
        if (endTurnButton != null)
        {
            endTurnButton.gameObject.SetActive(true);
        }
        
        Debug.Log($"Game initialized. Player {currentPlayerTurn + 1}'s turn.");
    }

    private void DealCardToPlayer(int playerIndex, int cardPosition)
    {
        if (playerIndex < 0 || playerIndex >= players.Length)
        {
            Debug.LogError($"Invalid player index: {playerIndex}");
            return;
        }

        Player player = players[playerIndex];
        
        // Get card position in grid (2 rows of 3)
        int row = cardPosition / 3;
        int col = cardPosition % 3;
        
        // Different layouts based on player position
        Vector3 position;
        
        // Bottom player (0), Top player (2)
        if (playerIndex == 0 || playerIndex == 2)
        {
            position = new Vector3(
                col * cardSpacing.x - cardSpacing.x, 
                -row * cardSpacing.y, 
                0
            );
        }
        // Left player (1), Right player (3)
        else
        {
            position = new Vector3(
                0,
                -col * cardSpacing.y + cardSpacing.y, 
                0
            );
        }

        // Draw and position card
        var (card, cardData) = deck.DrawCard(player.handContainer);
        
        if (card != null && cardData != null)
        {
            card.MoveTo(player.handContainer, position);
            
            // Set card size
            SetCardSize(card, handCardSize);
            
            // Set face down initially
            if (dealFaceDown)
            {
                card.SetFaceDown();
            }
            else
            {
                card.SetFaceUp();
            }
            
            // Add to player's card list
            player.cardsInHand.Add(card);
            playerCards[playerIndex].Add(card);
            
            // Initialize the card with GameManager and player index
            card.Initialize(cardData: cardData, gameManager: this, playerIndex: playerIndex);
        }
    }
    
    // Set card size based on location
    private void SetCardSize(Card card, Vector2 size)
    {
        if (card == null) return;
        
        // Set the card size directly
        card.fixedCardSize = size;
        
        // Force the card to update its display
        card.SetupCardDisplay();
        
        // Also directly set the RectTransform size for immediate visual update
        RectTransform rectTransform = card.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = size;
        }
    }
    
    private void OnDeckButtonClicked()
    {
        if (gameState != GameState.PlayerTurn)
            return;
            
        // Player draws a card from the deck
        var (drawnCard, drawnCardData) = deck.DrawCard(transform);
        
        if (drawnCard != null && drawnCardData != null)
        {
            // Initialize the drawn card (no player index here, maybe -1 or handle differently?)
            // For now, let's assume drawn cards don't need player index immediately.
            // We might need a different Initialize or Setup method for this case.
            // If drawn card needs interaction later, this needs refinement. 
            // Let's call Initialize with default playerIndex (-1) for now.
            drawnCard.Initialize(cardData: drawnCardData, gameManager: this, playerIndex: -1); 
            
            drawnCard.SetFaceUp();
            
            // Show the card to the player (animation could be added here)
            Debug.Log($"Player {currentPlayerTurn + 1} drew: {drawnCardData.cardName}");
            
            // For now, just discard to first discard pile
            if (discardPiles != null && discardPiles.Length > 0)
            {
                drawnCard.MoveTo(discardPiles[0], Vector3.zero);
                SetCardSize(drawnCard, discardCardSize);
                discardPileTopCards[0] = drawnCard;
            }
            else
            {
                Destroy(drawnCard.gameObject);
            }
        }
    }
    
    public void NextTurn()
    {
        if (gameState != GameState.PlayerTurn)
            return;
            
        currentPlayerTurn = (currentPlayerTurn + 1) % players.Length;
        UpdateCurrentPlayerUI();
        Debug.Log($"Turn ended. Next turn: Player {currentPlayerTurn + 1}");
        
        // Add logic here to re-enable interaction for the new current player's cards if needed
        // For now, we assume cards remain interactable unless specifically disabled.
    }
    
    private void UpdateCurrentPlayerUI()
    {
        if (currentPlayerText != null)
        {
            currentPlayerText.text = $"Player {currentPlayerTurn + 1}'s Turn";
        }
    }
    
    private void CheckGameEndCondition()
    {
        // Check if any player has all cards face up
        foreach (Player player in players)
        {
            bool allCardsRevealed = true;
            foreach (Card card in player.cardsInHand)
            {
                if (!card.IsFaceUp)
                {
                    allCardsRevealed = false;
                    break;
                }
            }
            
            if (allCardsRevealed && player.cardsInHand.Count >= cardsPerPlayer)
            {
                EndGame();
                return;
            }
        }
    }
    
    private void EndGame()
    {
        gameState = GameState.GameEnd;
        
        // Reveal all player cards
        foreach (Player player in players)
        {
            player.RevealAllCards();
        }
        
        // Calculate scores
        int[] scores = new int[players.Length];
        int minScore = int.MaxValue;
        int winnerIndex = -1;
        
        for (int i = 0; i < players.Length; i++)
        {
            scores[i] = players[i].CalculateScore();
            Debug.Log($"Player {i + 1} score: {scores[i]}");
            
            if (scores[i] < minScore)
            {
                minScore = scores[i];
                winnerIndex = i;
            }
        }
        
        // Display winner
        if (winnerIndex >= 0)
        {
            Debug.Log($"Player {winnerIndex + 1} wins with a score of {minScore}!");
            if (currentPlayerText != null)
            {
                currentPlayerText.text = $"Player {winnerIndex + 1} wins!";
            }
        }
        
        // Hide end turn button, could show restart button instead
        if (endTurnButton != null)
        {
            endTurnButton.gameObject.SetActive(false);
        }
    }
    
    // Reset game with a button click
    public void RestartGame()
    {
        InitializeGame();
    }

    // New method to handle card clicks and manage turns
    public void HandleCardClick(Card clickedCard)
    {
        if (gameState != GameState.PlayerTurn)
        {
            Debug.Log("Cannot interact with cards outside of PlayerTurn state.");
            return;
        }

        if (clickedCard.PlayerIndex != currentPlayerTurn)
        {
            Debug.Log($"It's not Player {clickedCard.PlayerIndex + 1}'s turn! Current turn: Player {currentPlayerTurn + 1}");
            return;
        }

        // It's the correct player's turn, and they clicked their own card
        if (!clickedCard.IsFaceUp)
        {
            clickedCard.Flip();
            Debug.Log($"Player {currentPlayerTurn + 1} flipped card: {clickedCard.Data.cardName}");
            
            // Potentially add game logic based on the flipped card here
            
            // End the current player's turn after flipping a card
            NextTurn(); 
        }
        else
        {
            // Optional: Handle clicking an already face-up card if needed
            Debug.Log("Card is already face up.");
        }
    }
}