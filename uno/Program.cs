using System;
using System.Collections.Generic;
using System.Linq;

// Enumerations
public enum Color
{
    Red,
    Blue,
    Green,
    Yellow
}

public enum CardType
{
    Number,
    Action,
    Wild
}

public enum ActionType
{
    Skip,
    Reverse,
    DrawTwo
}

public enum WildType
{
    Wild,
    WildDrawFour
}

public enum Number
{
    Zero,
    One,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine
}

// Interfaces
public interface ICard
{
    CardType GetCardType();
    void SetCardType(CardType type);
    Color? GetColor();
    void SetColor(Color? color);
    Number? GetNumber();
    void SetNumber(Number? number);
    ActionType? GetActionType();
    void SetActionType(ActionType? actionType);
    WildType? GetWildType();
    void SetWildType(WildType? wildType);
}

public interface IPlayer
{
    string GetName();
    void SetName(string name);
}

public interface IDeck
{
    List<ICard> GetCards();
    void SetCards(List<ICard> cards);
    ICard GetCardAt(int index);
    void SetCardAt(int index, ICard card);
}

public interface IDiscardPile
{
    List<ICard> GetCards();
    void SetCards(List<ICard> cards);
    ICard GetCardAt(int index);
    void SetCardAt(int index, ICard card);
}

// Implementation Classes
public class Card : ICard
{
    private CardType _type;
    private Color? _color;
    private Number? _number;
    private ActionType? _actionType;
    private WildType? _wildType;

    public CardType GetCardType() => _type;
    public void SetCardType(CardType type) => _type = type;
    
    public Color? GetColor() => _color;
    public void SetColor(Color? color) => _color = color;
    
    public Number? GetNumber() => _number;
    public void SetNumber(Number? number) => _number = number;
    
    public ActionType? GetActionType() => _actionType;
    public void SetActionType(ActionType? actionType) => _actionType = actionType;
    
    public WildType? GetWildType() => _wildType;
    public void SetWildType(WildType? wildType) => _wildType = wildType;

    public override string ToString()
    {
        if (GetCardType() == CardType.Number)
            return $"{GetColor()} {GetNumber()}";
        else if (GetCardType() == CardType.Action)
            return $"{GetColor()} {GetActionType()}";
        else
            return $"{GetWildType()}";
    }
}

public class Player : IPlayer
{
    private string _name;

    public string GetName() => _name;
    public void SetName(string name) => _name = name;
}

public class Deck : IDeck
{
    private List<ICard> _cards;

    public Deck()
    {
        _cards = new List<ICard>();
    }

    public List<ICard> GetCards() => _cards;
    public void SetCards(List<ICard> cards) => _cards = cards;
    
    public ICard GetCardAt(int index) => _cards[index];
    public void SetCardAt(int index, ICard card) => _cards[index] = card;
}

public class DiscardPile : IDiscardPile
{
    private List<ICard> _cards;

    public DiscardPile()
    {
        _cards = new List<ICard>();
    }

    public List<ICard> GetCards() => _cards;
    public void SetCards(List<ICard> cards) => _cards = cards;
    
    public ICard GetCardAt(int index) => _cards[index];
    public void SetCardAt(int index, ICard card) => _cards[index] = card;
}

public class GameController
{
    private List<IPlayer> _players;
    private Dictionary<IPlayer, List<ICard>> _playerHands;
    private IDeck _deck;
    private IDiscardPile _discardPile;
    private int _currentPlayerIndex;
    private bool _isClockwise;
    private Color? _currentWildColor;

    public Action<IPlayer> OnPlayerTurnChanged;
    public Action<IPlayer, ICard> OnCardPlayed;
    public Action<IPlayer> OnUnoViolation;
    public Action<IPlayer> OnGameEnded;
    
    // Delegates for UI interaction
    public Func<IPlayer, ICard, List<ICard>, ICard> CardChooser;
    public Func<Color> WildColorChooser;
    public Func<IPlayer, bool> UnoCallChecker; // Check if player wants to call UNO

    public GameController()
    {
        _players = new List<IPlayer>();
        _playerHands = new Dictionary<IPlayer, List<ICard>>();
        _deck = new Deck();
        _discardPile = new DiscardPile();
        _currentPlayerIndex = 0;
        _isClockwise = true;
        _currentWildColor = null;
    }

    // Game Flow Methods
    public void StartGame()
    {
        InitializeDeck();
        ShuffleDeck();
        DealCardsToPlayers();
        
        // Place first card on discard pile
        var firstCard = DrawCardFromDeck();
        AddCardToDiscardPile(firstCard);
    }

    public void AddPlayer(IPlayer player)
    {
        _players.Add(player);
        _playerHands[player] = new List<ICard>();
    }

    public void GameLoop()
    {
        while (!IsGameOver())
        {
            var currentPlayer = GetCurrentPlayer();
            OnPlayerTurnChanged?.Invoke(currentPlayer);
            
            var topCard = GetTopDiscardCard();
            var playableCards = GetPlayableCardsFromPlayer(currentPlayer, topCard);
            
            if (playableCards.Count > 0)
            {
                // Let UI handle the card choice
                var chosenCard = ChooseCard(currentPlayer, topCard, playableCards);
                if (chosenCard != null)
                {
                    PlayCard(currentPlayer, chosenCard);
                    
                    // Check for UNO call after playing card
                    if (GetPlayerHandSize(currentPlayer) == 1)
                    {
                        bool calledUno = UnoCallChecker?.Invoke(currentPlayer) ?? false;
                        if (!calledUno)
                        {
                            Console.WriteLine($"{currentPlayer.GetName()} forgot to call UNO! Drawing 2 penalty cards.");
                            PenalizePlayer(currentPlayer);
                            OnUnoViolation?.Invoke(currentPlayer);
                        }
                        else
                        {
                            Console.WriteLine($"{currentPlayer.GetName()} called UNO!");
                        }
                    }
                }
            }
            else
            {
                DrawCardFromDeck(currentPlayer);
                Console.WriteLine($"{currentPlayer.GetName()} drew a card.");
                
                // Check if drawn card is playable
                var newPlayableCards = GetPlayableCardsFromPlayer(currentPlayer, topCard);
                if (newPlayableCards.Count > 0)
                {
                    Console.WriteLine("You can now play a card!");
                    var chosenCard = ChooseCard(currentPlayer, topCard, newPlayableCards);
                    if (chosenCard != null)
                    {
                        PlayCard(currentPlayer, chosenCard);
                        
                        // Check for UNO call after playing drawn card
                        if (GetPlayerHandSize(currentPlayer) == 1)
                        {
                            bool calledUno = UnoCallChecker?.Invoke(currentPlayer) ?? false;
                            if (!calledUno)
                            {
                                Console.WriteLine($"{currentPlayer.GetName()} forgot to call UNO! Drawing 2 penalty cards.");
                                PenalizePlayer(currentPlayer);
                                OnUnoViolation?.Invoke(currentPlayer);
                            }
                            else
                            {
                                Console.WriteLine($"{currentPlayer.GetName()} called UNO!");
                            }
                        }
                    }
                }
            }
            
            // Check if other players want to challenge UNO calls
            CheckAllPlayersForUnoViolations();
            
            NextPlayer();
        }
        
        var winner = GetWinner();
        OnGameEnded?.Invoke(winner);
    }

    public void NextPlayer()
    {
        if (_isClockwise)
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
        else
            _currentPlayerIndex = (_currentPlayerIndex - 1 + _players.Count) % _players.Count;
    }

    public bool IsGameOver()
    {
        return _players.Any(player => GetPlayerHandSize(player) == 0);
    }

    public IPlayer GetWinner()
    {
        return _players.FirstOrDefault(player => GetPlayerHandSize(player) == 0);
    }

    // Player Hand Management Methods
    public void AddCardToPlayer(IPlayer player, ICard card)
    {
        _playerHands[player].Add(card);
    }

    public bool RemoveCardFromPlayer(IPlayer player, ICard card)
    {
        return _playerHands[player].Remove(card);
    }

    public int GetPlayerHandSize(IPlayer player)
    {
        return _playerHands[player].Count;
    }

    public bool PlayerHasCard(IPlayer player, ICard card)
    {
        return _playerHands[player].Contains(card);
    }

    public List<ICard> GetPlayerHand(IPlayer player)
    {
        return _playerHands[player];
    }

    public void ClearPlayerHand(IPlayer player)
    {
        _playerHands[player].Clear();
    }

    // Card Management Methods
    public bool PlayCard(IPlayer player, ICard card)
    {
        if (CanPlayCard(card, GetTopDiscardCard()))
        {
            RemoveCardFromPlayer(player, card);
            AddCardToDiscardPile(card);
            ExecuteCardEffect(card);
            OnCardPlayed?.Invoke(player, card);
            return true;
        }
        return false;
    }

    public ICard DrawCardFromDeck(IPlayer player)
    {
        var card = DrawCardFromDeck();
        if (card != null)
            AddCardToPlayer(player, card);
        return card;
    }

    private ICard DrawCardFromDeck()
    {
        if (IsDeckEmpty())
            RecycleDiscardPile();
            
        var cards = _deck.GetCards();
        if (cards.Count == 0) return null;
        
        var drawnCard = cards[cards.Count - 1];
        cards.RemoveAt(cards.Count - 1);
        _deck.SetCards(cards);
        return drawnCard;
    }

    public void AddCardToDeck(ICard card)
    {
        var cards = _deck.GetCards();
        cards.Add(card);
        _deck.SetCards(cards);
    }

    public int GetDeckCardCount()
    {
        return _deck.GetCards().Count;
    }

    public bool IsDeckEmpty()
    {
        return GetDeckCardCount() == 0;
    }

    public void ShuffleDeck()
    {
        var cards = _deck.GetCards();
        var random = new Random();
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            var temp = cards[i];
            cards[i] = cards[j];
            cards[j] = temp;
        }
        _deck.SetCards(cards);
    }

    public void AddCardToDiscardPile(ICard card)
    {
        var cards = _discardPile.GetCards();
        cards.Add(card);
        _discardPile.SetCards(cards);
    }

    public ICard GetTopDiscardCard()
    {
        var cards = _discardPile.GetCards();
        return cards.Count > 0 ? cards[cards.Count - 1] : null;
    }

    public int GetDiscardPileCardCount()
    {
        return _discardPile.GetCards().Count;
    }

    public bool IsDiscardPileEmpty()
    {
        return GetDiscardPileCardCount() == 0;
    }

    public List<ICard> GetPlayableCardsFromPlayer(IPlayer player, ICard topCard)
    {
        var hand = GetPlayerHand(player);
        return hand.Where(card => CanPlayCard(card, topCard)).ToList();
    }

    public ICard ChooseCard(IPlayer player, ICard topCard, List<ICard> playableCards)
    {
        // Use delegate if available, otherwise default behavior
        return CardChooser?.Invoke(player, topCard, playableCards) ?? playableCards.First();
    }

    public void ExecuteCardEffect(ICard card)
    {
        switch (card.GetCardType())
        {
            case CardType.Action:
                switch (card.GetActionType())
                {
                    case ActionType.Skip:
                        NextPlayer();
                        break;
                    case ActionType.Reverse:
                        ReverseDirection();
                        break;
                    case ActionType.DrawTwo:
                        NextPlayer();
                        DrawCardFromDeck(GetCurrentPlayer());
                        DrawCardFromDeck(GetCurrentPlayer());
                        break;
                }
                break;
            case CardType.Wild:
                _currentWildColor = ChooseWildColor();
                if (card.GetWildType() == WildType.WildDrawFour)
                {
                    NextPlayer();
                    for (int i = 0; i < 4; i++)
                        DrawCardFromDeck(GetCurrentPlayer());
                }
                break;
        }
    }

    // Rule Enforcement Methods
    public bool CallUno(IPlayer player)
    {
        return GetPlayerHandSize(player) == 1;
    }

    public bool CheckUnoViolation(IPlayer player)
    {
        return GetPlayerHandSize(player) == 1;
    }

    public void PenalizePlayer(IPlayer player)
    {
        DrawCardFromDeck(player);
        DrawCardFromDeck(player);
    }
    
    private void CheckAllPlayersForUnoViolations()
    {
        foreach (var player in _players)
        {
            if (CheckUnoViolation(player))
            {
                // Player has 1 card but hasn't been processed for UNO call
                // This could be expanded for more complex UNO challenge mechanics
            }
        }
    }

    public bool ValidateCard(ICard card)
    {
        return card != null;
    }

    public bool CanPlayCard(ICard card, ICard topCard)
    {
        if (topCard == null) return true;
        
        if (card.GetCardType() == CardType.Wild) return true;
        
        if (_currentWildColor.HasValue)
            return card.GetColor() == _currentWildColor.Value;
            
        return card.GetColor() == topCard.GetColor() ||
               (card.GetNumber().HasValue && card.GetNumber() == topCard.GetNumber()) ||
               (card.GetActionType().HasValue && card.GetActionType() == topCard.GetActionType());
    }

    // Setup/Utility Methods
    public void DealCardsToPlayers()
    {
        foreach (var player in _players)
        {
            for (int i = 0; i < 7; i++)
            {
                DrawCardFromDeck(player);
            }
        }
    }

    public void ReverseDirection()
    {
        _isClockwise = !_isClockwise;
    }

    public Color ChooseWildColor()
    {
        // Use delegate if available, otherwise default behavior
        return WildColorChooser?.Invoke() ?? Color.Red;
    }

    public void RecycleDiscardPile()
    {
        var cards = _discardPile.GetCards();
        if (cards.Count <= 1) return;
        
        var topCard = cards[cards.Count - 1];
        cards.RemoveAt(cards.Count - 1);
        
        _deck.SetCards(cards);
        _discardPile.SetCards(new List<ICard> { topCard });
        ShuffleDeck();
    }

    // Query Methods
    public IPlayer GetCurrentPlayer()
    {
        return _players[_currentPlayerIndex];
    }

    public List<Color> GetValidColors()
    {
        return new List<Color> { Color.Red, Color.Blue, Color.Green, Color.Yellow };
    }

    public IPlayer GetPlayerByName(string name)
    {
        return _players.FirstOrDefault(p => p.GetName() == name);
    }

    public List<int> GetPlayerHandSizes()
    {
        return _players.Select(player => GetPlayerHandSize(player)).ToList();
    }

    public List<IPlayer> GetAllPlayers()
    {
        return _players;
    }

    private void InitializeDeck()
    {
        var cards = new List<ICard>();

        // Number cards (0-9 for each color)
        foreach (Color color in Enum.GetValues<Color>())
        {
            foreach (Number number in Enum.GetValues<Number>())
            {
                var card = new Card();
                card.SetCardType(CardType.Number);
                card.SetColor(color);
                card.SetNumber(number);
                cards.Add(card);
                
                // Add second copy of non-zero numbers
                if (number != Number.Zero)
                {
                    var card2 = new Card();
                    card2.SetCardType(CardType.Number);
                    card2.SetColor(color);
                    card2.SetNumber(number);
                    cards.Add(card2);
                }
            }

            // Action cards (2 of each per color)
            foreach (ActionType action in Enum.GetValues<ActionType>())
            {
                for (int i = 0; i < 2; i++)
                {
                    var card = new Card();
                    card.SetCardType(CardType.Action);
                    card.SetColor(color);
                    card.SetActionType(action);
                    cards.Add(card);
                }
            }
        }

        // Wild cards (4 of each type)
        foreach (WildType wildType in Enum.GetValues<WildType>())
        {
            for (int i = 0; i < 4; i++)
            {
                var card = new Card();
                card.SetCardType(CardType.Wild);
                card.SetWildType(wildType);
                cards.Add(card);
            }
        }

        _deck.SetCards(cards);
    }
}

// UI Class
public class ConsoleUI
{
    private GameController _gameController;

    public ConsoleUI()
    {
        _gameController = new GameController();
        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        _gameController.OnPlayerTurnChanged += (player) => 
        {
            Console.WriteLine($"\n--- {player.GetName()}'s Turn ---");
            DisplayGameState();
        };

        _gameController.OnCardPlayed += (player, card) => 
        {
            Console.WriteLine($"\n{player.GetName()} played: {card}");
        };

        _gameController.OnUnoViolation += (player) => 
        {
            Console.WriteLine($"⚠️ {player.GetName()} was penalized for UNO violation!");
        };

        _gameController.OnGameEnded += (winner) => 
        {
            Console.WriteLine($"\n🎉 GAME OVER! 🎉");
            Console.WriteLine($"{winner.GetName()} WINS!");
            
            // Show final scores
            Console.WriteLine("\nFinal hand sizes:");
            foreach (var player in _gameController.GetAllPlayers())
            {
                Console.WriteLine($"{player.GetName()}: {_gameController.GetPlayerHandSize(player)} cards");
            }
        };
    }

    public void StartGame()
    {
        Console.WriteLine("🎴 Welcome to UNO! 🎴");
        Console.WriteLine("===================");
        
        // Add players
        Console.Write("Enter number of players (2-4): ");
        int numPlayers;
        while (!int.TryParse(Console.ReadLine(), out numPlayers) || numPlayers < 2 || numPlayers > 4)
        {
            Console.Write("Please enter a number between 2-4: ");
        }
        
        for (int i = 0; i < numPlayers; i++)
        {
            Console.Write($"Enter name for Player {i + 1}: ");
            string name = Console.ReadLine();
            
            var player = new Player();
            player.SetName(name);
            _gameController.AddPlayer(player);
        }

        _gameController.StartGame();
        
        // Set up UI interaction delegates
        _gameController.CardChooser = ChooseCardFromUI;
        _gameController.WildColorChooser = ChooseWildColorFromUI;
        _gameController.UnoCallChecker = CheckUnoCallFromUI;
        
        _gameController.GameLoop();
    }

    private ICard ChooseCardFromUI(IPlayer player, ICard topCard, List<ICard> playableCards)
    {
        Console.WriteLine($"\n{player.GetName()}, choose a card to play:");
        
        for (int i = 0; i < playableCards.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {playableCards[i]}");
        }
        Console.WriteLine($"{playableCards.Count + 1}. Skip turn (if you drew a card)");
        
        Console.Write("Enter your choice: ");
        int choice;
        while (!int.TryParse(Console.ReadLine(), out choice) || choice < 1 || choice > playableCards.Count + 1)
        {
            Console.Write($"Please enter a number between 1-{playableCards.Count + 1}: ");
        }
        
        if (choice == playableCards.Count + 1)
            return null; // Skip turn
            
        return playableCards[choice - 1];
    }
    
    private Color ChooseWildColorFromUI()
    {
        Console.WriteLine("\nChoose a color for the wild card:");
        Console.WriteLine("1. Red");
        Console.WriteLine("2. Blue");
        Console.WriteLine("3. Green");
        Console.WriteLine("4. Yellow");
        
        Console.Write("Enter your choice (1-4): ");
        int choice;
        while (!int.TryParse(Console.ReadLine(), out choice) || choice < 1 || choice > 4)
        {
            Console.Write("Please enter a number between 1-4: ");
        }
        
        return choice switch
        {
            1 => Color.Red,
            2 => Color.Blue,
            3 => Color.Green,
            4 => Color.Yellow,
            _ => Color.Red
        };
    }
    
    private bool CheckUnoCallFromUI(IPlayer player)
    {
        Console.WriteLine($"\n🚨 {player.GetName()}, you have 1 card left!");
        Console.WriteLine("Do you want to call UNO?");
        Console.WriteLine("1. Yes - Call UNO!");
        Console.WriteLine("2. No - Skip (you'll be penalized!)");
        
        Console.Write("Enter your choice (1-2): ");
        int choice;
        while (!int.TryParse(Console.ReadLine(), out choice) || choice < 1 || choice > 2)
        {
            Console.Write("Please enter 1 or 2: ");
        }
        
        return choice == 1;
    }

    private void DisplayGameState()
    {
        var currentPlayer = _gameController.GetCurrentPlayer();
        var topCard = _gameController.GetTopDiscardCard();
        var playerHand = _gameController.GetPlayerHand(currentPlayer);

        Console.WriteLine($"\nTop card on pile: {topCard}");
        
        // Show other players' hand sizes
        Console.WriteLine("\nOther players:");
        foreach (var player in _gameController.GetAllPlayers())
        {
            if (player != currentPlayer)
            {
                Console.WriteLine($"  {player.GetName()}: {_gameController.GetPlayerHandSize(player)} cards");
            }
        }
        
        Console.WriteLine($"\nYour hand ({playerHand.Count} cards):");
        for (int i = 0; i < playerHand.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. {playerHand[i]}");
        }

        var playableCards = _gameController.GetPlayableCardsFromPlayer(currentPlayer, topCard);
        
        if (playableCards.Count == 0)
        {
            Console.WriteLine("\n❌ No playable cards! You must draw a card.");
            Console.WriteLine("Press Enter to draw...");
            Console.ReadLine();
        }
        else
        {
            Console.WriteLine($"\n✅ You have {playableCards.Count} playable card(s)!");
        }
    }
}

// Main Program
public class Program
{
    public static void Main(string[] args)
    {
        var ui = new ConsoleUI();
        ui.StartGame();
    }
}