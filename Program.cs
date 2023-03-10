using System;
using System.Text;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

// This enum represents the possible outcomes of a game.
enum Results
{
    Win,
    Lose,
    Draw,
    Undetermined
}

// This class represents a basic game.
// Abstract modifier is not used, because serialization/deserialization is not possible with abstract classes.
[Serializable]
abstract class Game
{
    // A static property to keep track of the index for each game.
    public static uint constIndex { get; set; }

    [NonSerialized]
    private GameAccount _firstPlayer;
    [NonSerialized]
    private GameAccount _secondPlayer;


    // Properties to store the first and second player's GameAccount objects.
    public GameAccount FirstPlayer { get => _firstPlayer; }
    public GameAccount SecondPlayer { get => _secondPlayer; }

    // Properties to store the names of the first and second players.
    public string FirstPlayerName { get; }
    public string SecondPlayerName { get; }

    // Properties to store the rating cost and index of the game.
    public uint RatingCost { get; }
    public uint Index { get; }

    // Properties to store the game's name and result .
    public string GameName { get; protected set; }
    public Results Result { get; set; }

    // Constructor to initialize the fields with the provided values.
    protected Game(GameAccount firstPlayer, GameAccount secondPlayer, uint cost, string gameName)
    {
        _firstPlayer = firstPlayer;
        FirstPlayerName = firstPlayer.UserName;
        _secondPlayer = secondPlayer;
        SecondPlayerName = secondPlayer.UserName;
        RatingCost = cost;
        Index = constIndex++;
        GameName = gameName;
        Result = Results.Undetermined;
    }

}

// This class represents a game account.
[Serializable]
class GameAccount
{
    // Field to store the rating of the user.
    private uint rating = 5;

    // Properties to store user's name, games history and games count.
    // Marked with the JsonProperty attribute.
    public string UserName { get; }
    public List<Game> GameHistory { get; }
    public uint GamesCount { get; private set; }

    // Property for the rating field including the check for it not to be negative.
    // Marked with the JsonProperty attribute.
    public virtual uint CurrentRating
    {
        get => rating;
        protected set
        {
            int temp = (int)value;
            rating = temp < 1 ? 1 : value;
        }
    }

    // Constructor to initialize the fields with the provided values.
    public GameAccount(string name)
    {
        UserName = name;
        GamesCount = 0;
        GameHistory = new List<Game>();
    }

    // Default constructor to enable deserialization.
    public GameAccount() { }

    // Method which is called whenever user completes a game.
    public void CompleteGame(Game game)
    {
        // If the user was in the game, record the game for him and his opponent.
        if (game.FirstPlayer.UserName.Equals(this.UserName))
        {
            recordGame(game);
            game.SecondPlayer.recordGame(game);
        }
        else if (game.SecondPlayer.UserName.Equals(this.UserName))
        {
            recordGame(game);
            game.FirstPlayer.recordGame(game);
        }

        // If the user was not in the game, stop the app.
        else
        {
            Console.WriteLine("Players cannot complete a game they did not participate in");
            Console.ReadLine();
            Environment.Exit(0);
        }
    }

    // Method to record games.
    private void recordGame(Game game)
    {
        // If the game is not already recorded,
        if (!GameHistory.Any(g => g.Index == game.Index))
        {
            // count the new rating for the user.
            if (game.FirstPlayer.UserName.Equals(this.UserName))
            {
                if (game.Result == Results.Win)
                    CurrentRating += game.RatingCost;
                else if (game.Result == Results.Lose)
                    CurrentRating -= game.RatingCost;
            }
            else if (game.SecondPlayer.UserName.Equals(this.UserName))
            {
                if (game.Result.Equals(Results.Win))
                    CurrentRating -= game.RatingCost;
                else if (game.Result.Equals(Results.Lose))
                    CurrentRating += game.RatingCost;
            }

            // Then add the game to the user's game history and +1 to the games count.
            this.GameHistory.Add(game);
            GamesCount++;
        }
    }

    // Method to write user's stats to the console.
    // That includes user's game history and rating.
    public void GetStats()
    {
        if (this.GameHistory.Count != 0) Console.WriteLine(GameHistoryToString(this.GameHistory));
        Console.WriteLine($"{UserName}'s rating: {CurrentRating}\n");
    }

    // Method to convert game history list to a readable table view and return that as a string.
    public static string GameHistoryToString(List<Game> history)
    {
        // Find the maximum lengths of the game, player, and result strings.
        int maxGameNameLength = Math.Max(9, history.Max(game => game.GameName.Length));

        int maxFirstPlayerNameLength = history.Max(game => game.FirstPlayerName.Length);
        int maxSecondPlayerNameLength = history.Max(game => game.SecondPlayerName.Length);
        int maxResultLength = Math.Max(maxFirstPlayerNameLength, maxSecondPlayerNameLength);
        int maxIndexLength = history.Max(game => game.Index.ToString().Length);
        int maxWagerLength = history.Max(game => game.RatingCost.ToString().Length);
        maxFirstPlayerNameLength = Math.Max(12, maxFirstPlayerNameLength);
        maxSecondPlayerNameLength = Math.Max(13, maxSecondPlayerNameLength);

        // Create a StringBuilder to hold the table view string.
        StringBuilder sb = new StringBuilder();

        // Add the table headers.
        sb.Append("\nIndex".PadRight(maxIndexLength + 8) + "| Game Name".PadRight(maxGameNameLength + 5)
        + "| First Player".PadRight(maxFirstPlayerNameLength + 4) + "| Second Player".PadRight(maxSecondPlayerNameLength + 4)
        + "| Result".PadRight(maxResultLength + 7) + " | Wager\n");

        sb.Append("------".PadRight(maxIndexLength + 7, '-') + "|".PadRight(maxGameNameLength + 5, '-')
        + "|".PadRight(maxFirstPlayerNameLength + 4, '-') + "|".PadRight(maxSecondPlayerNameLength + 4, '-')
        + "|".PadRight(maxResultLength + 7, '-') + "-|------\n");

        // Iterate over the games in the list and add them to the StringBuilder.
        foreach (Game game in history)
        {
            string winner = "";
            if (game.Result == Results.Win)
                winner = game.FirstPlayerName + " won";
            else if (game.Result == Results.Lose)
                winner = game.SecondPlayerName + " won";
            else
                winner = "Draw";
            sb.Append($"{game.Index.ToString().PadRight(maxIndexLength + 6)} | {game.GameName.PadRight(maxGameNameLength + 2)} | {game.FirstPlayerName.PadRight(maxFirstPlayerNameLength + 1)} | {game.SecondPlayerName.PadRight(maxSecondPlayerNameLength + 1)} | {winner.PadRight(maxResultLength + 5)} | {game.RatingCost}\n");
        }

        // Get the final string.
        return sb.ToString();
    }
}

// Premium version of a game account (less points on loses).
[Serializable]
class PremiumGameAccount : GameAccount
{
    // Field which represents the multiplier by which the negative rating is divided (2 by default).
    private uint multiplier;

    // Rating setter is changed to work with multiplier.
    public override uint CurrentRating
    {
        get => base.CurrentRating;
        protected set => base.CurrentRating = base.CurrentRating > value ? ((base.CurrentRating - value) / multiplier) + value : value;
    }

    // Constructor to initialize the fields with the provided values.
    public PremiumGameAccount(string name, uint multiplier = 2) : base(name) { this.multiplier = multiplier; }
}

// PremiumPlus version of a game account (more points on wins, less points on loses).
[Serializable]
class PremiumPlusGameAccount : GameAccount
{
    // Field which represents the multiplier by which the rating value is increased and the negative value is divided (2 by default).
    private uint multiplier;

    // Rating setter is changed to work with multiplier.
    public override uint CurrentRating
    {
        get => base.CurrentRating;
        protected set => base.CurrentRating = base.CurrentRating > value ? ((base.CurrentRating - value) / multiplier) + value : ((value - base.CurrentRating) * multiplier) + base.CurrentRating;
    }

    // Constructor to initialize the fields with the provided values.
    public PremiumPlusGameAccount(string name, uint multiplier = 2) : base(name) { this.multiplier = multiplier; }
}

// This class that represents a game of Tic Tac Toe.
// Inherits from Game class.
[Serializable]
class TicTacToe : Game
{
    // This struct represents user's cursor (its position).
    [Serializable]
    private struct Cursor
    {
        private int yCord, xCord;

        // Getters and setters for cursor coordinates.
        // Setters ensure that cursor doesnt go out of bounds.
        public int YCord
        {
            get => yCord;
            set => yCord = (value <= 2 && value >= 0) ? value : yCord;
        }

        public int XCord
        {
            get => xCord;
            set => xCord = (value <= 2 && value >= 0) ? value : xCord;
        }

        // Constructor is necessary for structs.
        public Cursor()
        {
            yCord = 0;
            xCord = 0;
        }
    }

    // This dictionary contains key-value pairs for cell types.
    // It is used to make the code more readable.
    private Dictionary<string, string> Cells = new Dictionary<string, string>(){
        {"PlayerO", "[O]"},
        {"PlayerX", "[X]"},
        {"Empty", "[ ]"},
        {"Selected", "[/]"}
    };

    // This enum represents the possible player turn values.
    private enum PlayerTurn
    {
        FirstPlayer,
        SecondPlayer
    }

    // Fields required for a tic-tac-toe game.
    private Cursor cursor = new Cursor();
    private PlayerTurn currentPlayerTurn;
    private bool isWon;
    private bool isDraw;
    private int xTemp;
    private int yTemp;

    // Field which represents the shown game field.
    private string[,] field;
    // Field which represents the actual game field.
    private string[,] actualField;

    // Constructor to initialize the fields with the provided values.
    public TicTacToe(GameAccount firstPlayer, GameAccount secondPlayer, uint cost) : base(firstPlayer, secondPlayer, cost, "Tic Tac Toe")
    {
        cursor = new Cursor();
        currentPlayerTurn = PlayerTurn.FirstPlayer;
        field = new string[3, 3];
        actualField = new string[3, 3];
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                field[i, j] = Cells["Empty"];
                actualField[i, j] = Cells["Empty"];
            }
        }
        Play();
    }

    // This method is called to play the game.
    public void Play()
    {
        // The console color is changed to indicate that the game has started.
        Console.ForegroundColor = ConsoleColor.Cyan;

        ConsoleKey key;

        // Initial cursor placement.
        drawCursor();

        // While cycle which ends only after the game is finished.
        while (true)
        {
            // Console is cleared to stay readable and appealing.
            Console.Clear();

            // The game checks whether the conditions for victory have been met by any player.
            winConditions(Cells["PlayerX"]);
            winConditions(Cells["PlayerO"]);

            // The game field is printed to console.
            fieldPrint();

            // If the game is finished the result value is applied, the game is completed by player(s)
            // and the cycle is stopped.
            if (isWon || isDraw)
            {
                this.Result = isDraw ? Results.Draw : currentPlayerTurn == PlayerTurn.FirstPlayer ? Results.Lose : Results.Win;
                FirstPlayer.CompleteGame(this);
                break;
            }

            // Read the key that user presses.
            key = Console.ReadKey(true).Key;

            // Store last known cursor location.
            xTemp = cursor.XCord;
            yTemp = cursor.YCord;

            // Depending on the user's input move the cursor or make a turn.
            switch (key)
            {
                case ConsoleKey.DownArrow:
                case ConsoleKey.S:
                    cursor.YCord++;
                    break;

                case ConsoleKey.UpArrow:
                case ConsoleKey.W:
                    cursor.YCord--;
                    break;

                case ConsoleKey.RightArrow:
                case ConsoleKey.D:
                    cursor.XCord++;
                    break;

                case ConsoleKey.LeftArrow:
                case ConsoleKey.A:
                    cursor.XCord--;
                    break;

                case ConsoleKey.Spacebar:
                case ConsoleKey.Enter:
                    turnMaker();
                    break;

                default:
                    break;
            }

            // Cursor is moved to the next position if needed.
            drawCursor();
        }

        Console.WriteLine();

        // The console is turned back white, because the game has ended.
        Console.ForegroundColor = ConsoleColor.White;
    }

    // This method prints the game field to the console.
    private void fieldPrint()
    {
        Console.WriteLine();

        // Iterate through the field and draw each cell.
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (actualField[i, j] != Cells["Empty"])
                    field[i, j] = actualField[i, j];

                // If the game is not finished, draw the cursor.
                if (!isWon && !isDraw)
                    field[cursor.YCord, cursor.XCord] = Cells["Selected"];

                Console.Write("{0}\t", field[i, j]);
            }

            Console.WriteLine("\n");
        }

        // Depending on the game status, write whose turn it is, who won or is it a draw.
        Console.Write(isDraw ? "It's a draw!" : !isWon ? currentPlayerTurn == PlayerTurn.FirstPlayer ? $"It's {FirstPlayer.UserName}'s turn now." : $"It's {SecondPlayer.UserName}'s turn now." : currentPlayerTurn == PlayerTurn.FirstPlayer ? $"{SecondPlayer.UserName} won!" : $"{FirstPlayer.UserName} won!");
    }

    // This method checks if the game was finished.
    private void winConditions(string player)
    {
        // Iterate through the game field and check whether the victory conditions have been met or not.
        for (int i = 0; i < 3; i++)
        {
            // vertical
            if (actualField[0, i] == player && actualField[1, i] == player && actualField[2, i] == player)
                isWon = true;

            // horizontal
            if (actualField[i, 0] == player && actualField[i, 1] == player && actualField[i, 2] == player)
                isWon = true;

        }

        // diagonals
        if (actualField[0, 0] == player && actualField[1, 1] == player && actualField[2, 2] == player)
            isWon = true;

        if (actualField[2, 0] == player && actualField[1, 1] == player && actualField[0, 2] == player)
            isWon = true;


        int temp = 0;
        foreach (string cell in actualField)
            if (!cell.Equals(Cells["Empty"])) temp++;

        if (temp == 9) isDraw = true;

    }

    // This method draws the cursor.
    private void drawCursor()
    {
        // Set the current cursor position cell to "Selected".
        field[cursor.YCord, cursor.XCord] = Cells["Selected"];

        // Check if cursor has moved from its previous position.
        if (xTemp != cursor.XCord || yTemp != cursor.YCord)
            // If it has, set the previous position cell to "Empty".
            field[yTemp, xTemp] = Cells["Empty"];
    }

    // This method acts as a player's turn.
    private void turnMaker()
    {
        // If the cell the cursor is currently in is empty,
        if (actualField[cursor.YCord, cursor.XCord] == Cells["Empty"])
        {
            // set the cell to the symbol of the current player
            field[cursor.YCord, cursor.XCord] = currentPlayerTurn == PlayerTurn.FirstPlayer ? Cells["PlayerX"] : Cells["PlayerO"];
            actualField[cursor.YCord, cursor.XCord] = currentPlayerTurn == PlayerTurn.FirstPlayer ? Cells["PlayerX"] : Cells["PlayerO"];

            // and change the current player.
            currentPlayerTurn = currentPlayerTurn == PlayerTurn.FirstPlayer ? PlayerTurn.SecondPlayer : PlayerTurn.FirstPlayer;
        }
    }
}

// Class for serialization/deserialization.
class BinIO
{
    // This method is used to serialize a list to a bin file.
    public static void SerializeListToBinFile<T>(string filePath, List<T> list)
    {
        // Check if the file already exists. If it does, delete it.
        if (File.Exists(filePath))
            File.Delete(filePath);

        // Create a new file and serialize the list to it.
        using (FileStream stream = File.OpenWrite(filePath))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, list);
        }
    }

    // This method is used to deserialize a list from a bin file.
    public static List<T> DeserializeListFromBinFile<T>(string filePath)
    {
        // Check if the file exists. If it doesn't, throw an error.
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Unable to find file at path: {filePath}");

        // Open the file and deserialize the list from it.
        using (FileStream stream = File.OpenRead(filePath))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            return (List<T>)formatter.Deserialize(stream);
        }
    }

    // This method is used to get the list of all unique games.
    public static List<Game> GetAllUniqueGames(List<GameAccount> gameAccounts)
    {
        // Create a list to hold all of the games.
        List<Game> allGames = new List<Game>();

        // Iterate over the game accounts and add their games to the list.
        foreach (GameAccount gameAccount in gameAccounts)
            foreach (Game game in gameAccount.GameHistory)
                // Only add the game if it doesn't already exist in the list.
                if (!allGames.Any(g => g.Index == game.Index))
                    allGames.Add(game);

        // Reorder list by index.
        var temp = from i in allGames orderby i.Index ascending select i;

        // Return the list of all unique games.
        return temp.ToList();
    }
}

// User interface.
class Menu
{

    public List<GameAccount> gameAccounts { get; protected set; }
    public List<Game> gameHistory { get; protected set; }


    public Menu(List<GameAccount> gameAccounts, List<Game> gameHistory)
    {
        this.gameAccounts = gameAccounts;
        this.gameHistory = gameHistory;
    }

    public void Activate()
    {
        // Endless cycle which implements a console-based user interface.
        while (true)
        {
            Console.Clear();

            // Display menu options.
            Console.WriteLine("[======Menu Options=====]\n");
            Console.WriteLine(" [1] Play Tic Tac Toe");
            Console.WriteLine(" [2] View Game History");
            Console.WriteLine(" [3] View Player's stats");
            Console.WriteLine(" [4] Clear data");
            Console.WriteLine(" [5] Quit");
            Console.WriteLine("\n[=======================]");

            // Get user input.
            Console.Write("\nEnter an option: ");
            Console.ForegroundColor = ConsoleColor.Green;
            string input = Console.ReadLine();
            Console.ForegroundColor = ConsoleColor.White;

            // Parse user input and execute corresponding action.
            switch (input)
            {
                case "1":
                    this.playGame();
                    break;

                case "2":
                    this.viewHistory();
                    break;

                case "3":
                    this.viewStats();
                    break;

                case "4":
                    this.clearData();
                    break;

                case "5":
                    // Quit.
                    Environment.Exit(0);
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid input. Please try again.");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.ReadKey();
                    break;
            }
        }
    }

    private void playGame()
    {
        // Endless while cycles are needed to prevent the user from making wrong inputs.
        Console.Clear();

        // Temporary variables to store user's input.
        string temp;
        int temp2;

        Console.Write("First player: ");
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            temp = Console.ReadLine();
            if (!temp.Equals("")) break;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid input. Please try again.");
        }
        Console.ForegroundColor = ConsoleColor.White;

        // If the game account with said name exists, use it.
        // If not, create a new one and add it to the list.
        GameAccount first = gameAccounts.FirstOrDefault(g => g.UserName.Equals(temp));
        if (first == null)
        {
            Console.Write("First player account type (1-3):\n[1]Basic\n[2]Premium\n[3]Premium+\n");
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                temp2 = Convert.ToInt32(Console.ReadLine());
                if (temp2 >= 1 && temp2 <= 3) break;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid input. Please try again.");
            }
            Console.ForegroundColor = ConsoleColor.White;
            first = temp2 == 1 ? new GameAccount(temp) : temp2 == 2 ? new PremiumGameAccount(temp) : new PremiumPlusGameAccount(temp);
            gameAccounts.Add(first);
        }

        Console.Write("Second player: ");
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            temp = Console.ReadLine();
            if (!temp.Equals("") && !temp.Equals(first.UserName)) break;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid input. Please try again.");
        }
        Console.ForegroundColor = ConsoleColor.White;

        // If the game account with said name exists, use it.
        // If not, create a new one and add it to the list.
        GameAccount second = gameAccounts.FirstOrDefault(g => g.UserName.Equals(temp));
        if (second == null)
        {
            Console.Write("Second player account type (1-3):\n[1]Basic\n[2]Premium\n[3]Premium+\n");
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                temp2 = Convert.ToInt32(Console.ReadLine());
                if (temp2 >= 1 && temp2 <= 3) break;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid input. Please try again.");
            }
            Console.ForegroundColor = ConsoleColor.White;
            second = temp2 == 1 ? new GameAccount(temp) : temp2 == 2 ? new PremiumGameAccount(temp) : new PremiumPlusGameAccount(temp);
            gameAccounts.Add(second);
        }

        Console.Write("Enter rating wager (>=0 && <=player's rating): ");
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            temp = Console.ReadLine();
            if (!temp.Equals("") && Convert.ToInt32(temp) >= 0 && Convert.ToInt32(temp) <= first.CurrentRating && Convert.ToInt32(temp) <= second.CurrentRating) break;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid input. Please try again.");
        }
        Console.ForegroundColor = ConsoleColor.White;

        // Create a new instance of a tic-tac-toe game and start it.
        TicTacToe game = new TicTacToe(first, second, Convert.ToUInt32(temp));
        game.Play();

        // When the game is finished, add it to the game history
        // and store new values for the game history and game accounts in json files.
        gameHistory = BinIO.GetAllUniqueGames(gameAccounts);
        BinIO.SerializeListToBinFile<Game>("gameHistory.bin", gameHistory);
        BinIO.SerializeListToBinFile<GameAccount>("accounts.bin", gameAccounts);

        Console.ReadLine();
    }

    private void viewHistory()
    {
        // If there are no games recorded, do nothing.
        if (gameHistory.Count == 0) return;
        Console.Clear();

        // View Game History.
        Console.WriteLine(GameAccount.GameHistoryToString(gameHistory));

        Console.ReadLine();
    }

    private void viewStats()
    {
        // If there are no game accounts, do nothing.
        if (gameAccounts.Count == 0) return;
        Console.Clear();

        // View player's ratings.
        foreach (GameAccount g in from acc in gameAccounts orderby acc.CurrentRating descending select acc)
            Console.WriteLine((g is PremiumPlusGameAccount ? "[Premium+]" : g is PremiumGameAccount ? "[Premium ]" : "[Basic   ]") + $" {g.UserName.PadRight(gameAccounts.Max(acc => acc.UserName.Length))}'s  rating: {g.CurrentRating}");

        Console.ReadLine();
    }

    private void clearData()
    {
        string input;
        Console.Write("Are you sure you want to clear data? (Y/N)\n");
        Console.ForegroundColor = ConsoleColor.Green;
        while (true)
        {
            input = Console.ReadLine();
            if (input == "Y" || input == "y" || input == "N" || input == "n") break;
        }
        Console.ForegroundColor = ConsoleColor.White;
        switch (input)
        {
            case "Y":
            case "y":
                gameAccounts = new List<GameAccount>();
                gameHistory = new List<Game>();
                BinIO.SerializeListToBinFile<Game>("gameHistory.bin", gameHistory);
                BinIO.SerializeListToBinFile<GameAccount>("accounts.bin", gameAccounts);
                break;
            case "N":
            case "n":
                break;
        }
    }
}

class Program
{

    static void Main(string[] args)
    {
        // Try-finally in case an erroe occures while the console color is changed.
        try
        {
            // Lists of game accounts and game history.
            // Imported from json files.
            List<GameAccount> gameAccounts = BinIO.DeserializeListFromBinFile<GameAccount>("accounts.bin");
            List<Game> gameHistory = BinIO.DeserializeListFromBinFile<Game>("gameHistory.bin");

            // The index is set to continue the previous counting.
            Game.constIndex = gameHistory.Count == 0 ? 0 : (uint)gameHistory.Count;

            // Initializing and activating game menu.
            Menu m = new Menu(gameAccounts, gameHistory);
            m.Activate();
        }
        finally
        {
            Console.ForegroundColor = ConsoleColor.White;
        }

    }

}
