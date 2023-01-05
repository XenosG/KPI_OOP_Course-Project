using System;
using System.Text;
using System.Linq;
using Newtonsoft.Json;

// This enum represents the possible outcomes of a game.
public enum Results
{
    Win,
    Lose,
    Draw,
    Undetermined
}

// This class represents a basic game.
// Abstract modifier is not used, because serialization/deserialization is not possible with abstract classes.
public class Game
{
    // A static field to keep track of the index for each game.
    public static uint constIndex { get; set; }

    // Fields to store the first and second player's GameAccount objects.
    // Marked with the JsonIgnore attribute to prevent circular references during serialization/deserialization.
    [JsonIgnore]
    public GameAccount FirstPlayer { get; }
    [JsonIgnore]
    public GameAccount SecondPlayer { get; }

    // Fields to store the names of the first and second players.
    // Marked with the JsonProperty attribute to enable serialization/deserialization.
    [JsonProperty]
    public string FirstPlayerName { get; set; }
    [JsonProperty]
    public string SecondPlayerName { get; set; }

    // Fields to store the rating cost and index of the game.
    // Marked with the JsonProperty attribute.
    [JsonProperty]
    public uint RatingCost { get; set; }
    [JsonProperty]
    public uint Index { get; set; }

    // Fields to store the game's name and result .
    // Marked with the JsonProperty attribute.
    [JsonProperty]
    public string GameName { get; set; }
    [JsonProperty]
    public Results Result { get; set; }

    // Constructor to initialize the fields with the provided values.
    public Game(GameAccount firstPlayer, GameAccount secondPlayer, uint cost, string gameName)
    {
        FirstPlayer = firstPlayer;
        FirstPlayerName = firstPlayer.UserName;
        SecondPlayer = secondPlayer;
        SecondPlayerName = secondPlayer.UserName;
        RatingCost = cost;
        Index = constIndex++;
        GameName = gameName;
        Result = Results.Undetermined;
    }

    // Default constructor to enable deserialization.
    public Game() { }
}

// This class represents a game account.
public class GameAccount
{
    // Field to store the rating of the user.
    private uint rating = 5;

    // Fields to store user's name, games history and games count.
    // Marked with the JsonProperty attribute.
    [JsonProperty]
    public string UserName { get; set; }
    [JsonProperty]
    public List<Game> GameHistory { get; set; }
    [JsonProperty]
    public uint GamesCount { get; set; }

    // Get/set for the rating field including the check for it not to be negative.
    // Marked with the JsonProperty attribute.
    [JsonProperty]
    public uint CurrentRating
    {
        get => rating;
        set
        {
            int temp = (int)value;
            rating = temp < 0 ? 0 : value;
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
            RecordGame(game);
            game.SecondPlayer.RecordGame(game);
        }
        else if (game.SecondPlayer.UserName.Equals(this.UserName))
        {
            RecordGame(game);
            game.FirstPlayer.RecordGame(game);
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
    private void RecordGame(Game game)
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
        if (this.GameHistory.Count != 0) Console.WriteLine(UserGameHistoryToString());
        Console.WriteLine($"{UserName}'s rating: {CurrentRating}\n");
    }

    // Method to make results in table represent themselves relatively to the user.
    private string UserGameHistoryToString(){
        List<Game> temp = new List<Game>(this.GameHistory);
        foreach(Game game in temp) game.Result = game.Result == Results.Draw || this.UserName.Equals(game.FirstPlayerName) ? game.Result : game.Result == Results.Win ? Results.Lose : Results.Win;
        return GameHistoryToString(temp);
    }

    // Method to convert game history list to a readable table view and return that as a string.
    public static string GameHistoryToString(List<Game> history)
    {
        // Find the maximum lengths of the game, player, and result strings.
        int maxGameNameLength = history.Max(game => game.GameName.Length);
        int maxFirstPlayerNameLength = history.Max(game => game.FirstPlayerName.Length);
        int maxSecondPlayerNameLength = history.Max(game => game.SecondPlayerName.Length);
        int maxResultLength = history.Max(game => game.Result.ToString().Length);
        int maxIndexLength = history.Max(game => game.Index.ToString().Length);
        int maxWagerLength = history.Max(game => game.RatingCost.ToString().Length);

        // Create a StringBuilder to hold the table view string.
        StringBuilder sb = new StringBuilder();

        // Add the table headers.
        sb.Append("\nIndex".PadRight(maxIndexLength + 8) + "| Game Name".PadRight(maxGameNameLength + 10)
        + "| First Player".PadRight(maxFirstPlayerNameLength + 15) + "| Second Player".PadRight(maxSecondPlayerNameLength + 16)
        + "| Result".PadRight(8) + " | Wager\n");

        sb.Append("------".PadRight(maxIndexLength + 7, '-') + "|".PadRight(maxGameNameLength + 10, '-')
        + "|".PadRight(maxFirstPlayerNameLength + 15, '-') + "|".PadRight(maxSecondPlayerNameLength + 16, '-')
        + "|".PadRight(8, '-') + "-|------\n");

        // Iterate over the games in the list and add them to the StringBuilder.
        foreach (Game game in history)
            sb.Append($"{game.Index.ToString().PadRight(maxIndexLength + 6)} | {game.GameName.PadRight(maxGameNameLength + 7)} | {game.FirstPlayerName.PadRight(maxFirstPlayerNameLength + 12)} | {game.SecondPlayerName.PadRight(maxSecondPlayerNameLength + 13)} | {game.Result.ToString().PadRight(6)} | {game.RatingCost}\n");

        // Get the final string.
        return sb.ToString();
    }
}

// This class that represents a game of Tic Tac Toe.
// Inherits from Game class.
class TicTacToe : Game
{
    // This struct represents user's cursor (its position).
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
    private string[,] staticField;

    // Constructor to initialize the fields with the provided values.
    public TicTacToe(GameAccount firstPlayer, GameAccount secondPlayer, uint cost) : base(firstPlayer, secondPlayer, cost, "Tic Tac Toe")
    {
        cursor = new Cursor();
        currentPlayerTurn = PlayerTurn.FirstPlayer;
        field = new string[3, 3];
        staticField = new string[3, 3];
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                field[i, j] = Cells["Empty"];
                staticField[i, j] = Cells["Empty"];
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
        TextCursor();

        // While cycle which ends only after the game is finished.
        while (true)
        {
            // Console is cleared to stay readable and appealing.
            Console.Clear();

            // The game checks whether the conditions for victory have been met by any player.
            WinConditions(Cells["PlayerX"]);
            WinConditions(Cells["PlayerO"]);

            // The game field is printed to console.
            FieldPrint();

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
                    PlayerSetter();
                    break;

                default:
                    break;
            }

            // Cursor is moved to the next position if needed.
            TextCursor();
        }

        Console.WriteLine();

        // The console is turned back white, because the game has ended.
        Console.ForegroundColor = ConsoleColor.White;
    }

    // This method prints the game field to the console.
    private void FieldPrint()
    {
        Console.WriteLine();

        // Iterate through the field and draw each cell.
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (staticField[i, j] != Cells["Empty"])
                    field[i, j] = staticField[i, j];

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
    private void WinConditions(string player)
    {
        // Iterate through the game field and check whether the victory conditions have been met or not.
        for (int i = 0; i < 3; i++)
        {
            // vertical
            if (staticField[0, i] == player && staticField[1, i] == player && staticField[2, i] == player)
                isWon = true;

            // horizontal
            if (staticField[i, 0] == player && staticField[i, 1] == player && staticField[i, 2] == player)
                isWon = true;

        }

        // diagonals
        if (staticField[0, 0] == player && staticField[1, 1] == player && staticField[2, 2] == player)
            isWon = true;

        if (staticField[2, 0] == player && staticField[1, 1] == player && staticField[0, 2] == player)
            isWon = true;


        int temp = 0;
        foreach (string cell in staticField)
            if (!cell.Equals(Cells["Empty"])) temp++;

        if (temp == 9) isDraw = true;

    }

    // This method draws the cursor.
    private void TextCursor()
    {
        // Set the current cursor position cell to "Selected".
        field[cursor.YCord, cursor.XCord] = Cells["Selected"];

        // Check if cursor has moved from its previous position.
        if (xTemp != cursor.XCord || yTemp != cursor.YCord)
            // If it has, set the previous position cell to "Empty".
            field[yTemp, xTemp] = Cells["Empty"];
    }

    // This method acts as a player's turn.
    private void PlayerSetter()
    {
        // If the cell the cursor is currently in is empty,
        if (staticField[cursor.YCord, cursor.XCord] == Cells["Empty"])
        {
            // set the cell to the symbol of the current player
            field[cursor.YCord, cursor.XCord] = currentPlayerTurn == PlayerTurn.FirstPlayer ? Cells["PlayerX"] : Cells["PlayerO"];
            staticField[cursor.YCord, cursor.XCord] = currentPlayerTurn == PlayerTurn.FirstPlayer ? Cells["PlayerX"] : Cells["PlayerO"];

            // and change the current player.
            currentPlayerTurn = currentPlayerTurn == PlayerTurn.FirstPlayer ? PlayerTurn.SecondPlayer : PlayerTurn.FirstPlayer;
        }
    }
}


class Program
{

    static void Main(string[] args)
    {
        // Lists of game accounts and game history.
        // Imported from json files.
        List<GameAccount> gameAccounts;
        List<Game> gameHistory;
        gameAccounts = GetListFromJson<GameAccount>("accounts.json");
        gameHistory = GetListFromJson<Game>("gameHistory.json");

        // The index is set to continue the previous counting.
        Game.constIndex = gameHistory.Count == 0 ? 0 : (uint)gameHistory.Count;


        // Endless cycle which implements a console-based user interface.
        while (true)
        {
            Console.Clear();

            // Display menu options.
            Console.WriteLine("Menu Options:");
            Console.WriteLine("1. Play Tic Tac Toe");
            Console.WriteLine("2. View Game History");
            Console.WriteLine("3. View Player's ratings");
            Console.WriteLine("4. Quit");

            // Get user input.
            Console.Write("Enter an option: ");
            string input = Console.ReadLine();

            // Parse user input and execute corresponding action.
            switch (input)
            {
                case "1":
                    // Endless while cycles are needed to prevent the user from making wrong inputs.
                    Console.Clear();

                    // Temporary variable to store user's input.
                    string temp;

                    Console.Write("First player: ");
                    while (true)
                    {
                        temp = Console.ReadLine();
                        if (!temp.Equals("")) break;
                    }

                    // If the game account with said name exists, use it.
                    // If not, create a new one and add it to the list.
                    GameAccount first = gameAccounts.FirstOrDefault(g => g.UserName.Equals(temp));
                    if (first == null)
                    {
                        first = new GameAccount(temp);
                        gameAccounts.Add(first);
                    }

                    Console.Write("Second player: ");
                    while (true)
                    {
                        temp = Console.ReadLine();
                        if (!temp.Equals("") && !temp.Equals(first.UserName)) break;
                    }

                    // If the game account with said name exists, use it.
                    // If not, create a new one and add it to the list.
                    GameAccount second = gameAccounts.FirstOrDefault(g => g.UserName.Equals(temp));
                    if (second == null)
                    {
                        second = new GameAccount(temp);
                        gameAccounts.Add(second);
                    }

                    Console.Write("Enter rating wager (>=0): ");
                    while (true)
                    {
                        temp = Console.ReadLine();
                        if (!temp.Equals("") && Convert.ToInt32(temp) >= 0) break;
                    }

                    // Create a new instance of a tic-tac-toe game and start it.
                    TicTacToe game = new TicTacToe(first, second, Convert.ToUInt32(temp));
                    game.Play();

                    // When the game is finished, add it to the game history
                    // and store new values for the game history and game accounts in json files.
                    gameHistory = GetAllUniqueGames(gameAccounts);
                    SaveListToJson<Game>(gameHistory, "gameHistory.json");
                    SaveListToJson<GameAccount>(gameAccounts, "accounts.json");

                    Console.ReadLine();
                    break;

                case "2":
                    // If there are no games recorded, do nothing.
                    if (gameHistory.Count == 0) break;
                    Console.Clear();

                    // View Game History.
                    Console.WriteLine(GameAccount.GameHistoryToString(gameHistory));

                    Console.ReadLine();
                    break;

                case "3":
                    // If there are no game accounts, do nothing.
                    if (gameAccounts.Count == 0) break;
                    Console.Clear();

                    // View player's ratings.
                    foreach (GameAccount g in from acc in gameAccounts orderby acc.CurrentRating descending select acc)
                        Console.WriteLine($"{g.UserName.PadRight(gameAccounts.Max(acc => acc.UserName.Length))}'s rating: {g.CurrentRating}");

                    Console.ReadLine();
                    break;

                case "4":
                    // Quit.
                    Environment.Exit(0);
                    break;

                default:
                    Console.WriteLine("Invalid input. Please try again.");
                    break;
            }
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

        // Return the list of all unique games.
        return allGames;
    }

    // This method is used to put a list into a json file.
    public static void SaveListToJson<T>(List<T> list, string filePath)
    {
        // Serialize the list to a JSON string, avoiding infinite cycles
        // and write the JSON string to the specified file.
        File.WriteAllText(filePath, JsonConvert.SerializeObject(list, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
    }

    // This method is used to get a list from a json file.
    // Deserializes the list from a JSON file and returns it.
    public static List<T> GetListFromJson<T>(string filePath) => JsonConvert.DeserializeObject<List<T>>(File.ReadAllText(filePath));

}