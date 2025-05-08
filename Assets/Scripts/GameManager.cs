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
    private Card revealedDeckCard = null;
    private Card cardAwaitingDiscardDecision = null;

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

        if (cardAwaitingDiscardDecision != null)
        {
            Debug.Log("A card is awaiting discard. Please select a discard pile.");
            return;
        }

        // If a card is already revealed, and player clicks deck again, replace the old revealed card.
        if (revealedDeckCard != null)
        {
            Debug.Log($"Replacing existing revealed card {revealedDeckCard.Data.cardName} on deck panel with a new draw.");
            Destroy(revealedDeckCard.gameObject); // Or return to deck logic
            revealedDeckCard = null;
        }
            
        Transform deckPanelTransform = (deckButton != null) ? deckButton.transform : transform;
        var (drawnCard, drawnCardData) = deck.DrawCard(deckPanelTransform);
        
        if (drawnCard != null && drawnCardData != null)
        {
            drawnCard.Initialize(cardData: drawnCardData, gameManager: this, playerIndex: -1); 
            drawnCard.SetFaceUp();
            drawnCard.transform.localPosition = Vector3.zero; 
            SetCardSize(drawnCard, handCardSize);

            this.revealedDeckCard = drawnCard;

            Debug.Log($"Player {currentPlayerTurn + 1} drew: {drawnCardData.cardName} onto DeckPanel. It is now {this.revealedDeckCard.Data.cardName}.");
        }
    }
    
    public void NextTurn()
    {
        if (gameState != GameState.PlayerTurn)
            return;
            
        currentPlayerTurn = (currentPlayerTurn + 1) % players.Length;
        UpdateCurrentPlayerUI();
        Debug.Log($"Turn ended. Next turn: Player {currentPlayerTurn + 1}");
    }
    
    private void UpdateCurrentPlayerUI()
    {
        if (currentPlayerText != null)
        {
            currentPlayerText.text = $"Player {currentPlayerTurn + 1} Turn";
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

        if (cardAwaitingDiscardDecision != null)
        {
            Debug.Log("A card is awaiting discard. Please select a discard pile before interacting with hand cards.");
            return;
        }

        if (revealedDeckCard != null)
        {
            Debug.Log("A card is revealed on the deck. Click one of your hand cards to swap, or the deck card might need specific action.");
        }

        if (clickedCard.PlayerIndex != currentPlayerTurn)
        {
            Debug.Log($"It's not Player {clickedCard.PlayerIndex + 1}'s turn, or card does not belong to current player! Current turn: Player {currentPlayerTurn + 1}");
            return;
        }

        if (!clickedCard.IsFaceUp)
        {
            clickedCard.Flip();
            Debug.Log($"Player {currentPlayerTurn + 1} flipped card: {clickedCard.Data.cardName}");
            NextTurn(); 
        }
        else
        {
            Debug.Log("Card is already face up.");
        }
    }

    public bool CanSwapWithRevealedCard(Card handCard)
    {
        if (revealedDeckCard == null || gameState != GameState.PlayerTurn || cardAwaitingDiscardDecision != null)
        {
            return false;
        }
        return handCard.PlayerIndex == currentPlayerTurn;
    }

    public void PerformSwap(Card playerHandCardToDeck)
    {
        if (!CanSwapWithRevealedCard(playerHandCardToDeck))
        {
            Debug.LogWarning("Swap conditions not met or invalid card for swap.");
            return;
        }

        Player currentPlayer = players[currentPlayerTurn];
        Card cardFromDeckToHand = this.revealedDeckCard;

        Transform deckPanelTransform = cardFromDeckToHand.transform.parent; 
        Transform playerHandContainer = currentPlayer.handContainer;

        // 1. Update Player's cardsInHand list
        int originalIndex = currentPlayer.cardsInHand.IndexOf(playerHandCardToDeck);

        if (originalIndex != -1)
        {
            currentPlayer.cardsInHand.RemoveAt(originalIndex);
            currentPlayer.cardsInHand.Insert(originalIndex, cardFromDeckToHand);
        }
        else
        {
            Debug.LogError($"Critical Error: Card {playerHandCardToDeck.Data.cardName} (Player: {playerHandCardToDeck.PlayerIndex}) was clicked for swap but not found in current player {currentPlayer.playerIndex}'s hand list. Aborting swap.");
            // NOTE: If we abort here, the revealedDeckCard (cardFromDeckToHand) is effectively lost from play
            // as it's not returned to deck or discard. This might need more robust error handling.
            // For now, the turn won't end, and the revealedDeckCard remains. The player might need to take another action or end turn.
            return; 
        }
        
        // 2. Configure and move card from Hand to Deck Panel (playerHandCardToDeck)
        playerHandCardToDeck.transform.SetParent(deckPanelTransform, false);
        playerHandCardToDeck.transform.SetAsLastSibling(); // Ensure it's drawn on top
        playerHandCardToDeck.SetPlayerIndex(-1); 
        SetCardSize(playerHandCardToDeck, handCardSize); 
        playerHandCardToDeck.SetFaceUp(); // Ensure the card moved to the deck panel is face up
        playerHandCardToDeck.transform.localPosition = Vector3.zero; // Set position *after* all other visual updates

        // 3. Configure and move card from Deck Panel to Hand (cardFromDeckToHand)
        cardFromDeckToHand.transform.SetParent(playerHandContainer, false);
        cardFromDeckToHand.SetPlayerIndex(currentPlayerTurn); 
        SetCardSize(cardFromDeckToHand, handCardSize); 
        // Position will be set by Player's UpdateHandLayout

        Debug.Log($"PerformSwap: About to call UpdateHandLayout for Player {currentPlayer.playerIndex}. Card at originalIndex ({originalIndex}) in their hand is now {currentPlayer.cardsInHand[originalIndex].Data.cardName}. Player hand count: {currentPlayer.cardsInHand.Count}");
        for(int k=0; k < currentPlayer.cardsInHand.Count; ++k) {
            if (currentPlayer.cardsInHand[k] != null && currentPlayer.cardsInHand[k].Data != null) {
                 Debug.Log($"PerformSwap: Player {currentPlayer.playerIndex} hand before reposition, index {k}: {currentPlayer.cardsInHand[k].Data.cardName} (InstanceID: {currentPlayer.cardsInHand[k].GetInstanceID()})");
            } else {
                 Debug.Log($"PerformSwap: Player {currentPlayer.playerIndex} hand before reposition, index {k}: NULL CARD DATA OR CARD");
            }
        }

        // 4. Update player's visual hand layout
        currentPlayer.UpdateHandLayout();

        // 5. Update the game state's `revealedDeckCard` reference
        // The card that moved TO the deck panel is now the one "revealed" there for the next potential action (or cleanup on turn end)
        this.revealedDeckCard = playerHandCardToDeck; 

        Debug.Log($"Player {currentPlayerTurn + 1} swapped. Card from hand {playerHandCardToDeck.Data.cardName} moved to deck. Card from deck {cardFromDeckToHand.Data.cardName} moved to hand. New revealed deck card is {this.revealedDeckCard.Data.cardName}");

        // 6. End turn
        // NextTurn() will also handle clearing the new revealedDeckCard if the *next* player doesn't interact with it.
        this.cardAwaitingDiscardDecision = playerHandCardToDeck;
        this.revealedDeckCard = null;
    }

    public void SelectDiscardPile(int pileIndex)
    {
        if (cardAwaitingDiscardDecision == null)
        {
            Debug.LogWarning("SelectDiscardPile called, but no card is awaiting discard decision.");
            return;
        }
        if (pileIndex < 0 || discardPiles == null || pileIndex >= discardPiles.Length)
        {
            Debug.LogError($"Invalid pileIndex {pileIndex} for SelectDiscardPile.");
            cardAwaitingDiscardDecision = null; // Clear the card to prevent further errors with it.
            NextTurn(); // Proceed to next turn to not stall the game.
            return;
        }

        Card cardToDiscard = this.cardAwaitingDiscardDecision;
        this.cardAwaitingDiscardDecision = null;

        Debug.Log($"Player {currentPlayerTurn + 1} is discarding {cardToDiscard.Data.cardName} to discard pile {pileIndex + 1}.");

        cardToDiscard.transform.SetParent(discardPiles[pileIndex], false);
        SetCardSize(cardToDiscard, discardCardSize); 
        cardToDiscard.transform.localPosition = Vector3.zero; 
        cardToDiscard.transform.SetAsLastSibling(); // Place it on top
        // cardToDiscard.SetPlayerIndex(-1); // Already set in PerformSwap

        if (discardPileTopCards != null && pileIndex < discardPileTopCards.Length) {
            discardPileTopCards[pileIndex] = cardToDiscard; // Update tracking if used
        }

        NextTurn();
    }
}