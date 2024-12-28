using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.IO.Ports;
using System.Runtime.Versioning;

namespace TicTacToeWPF
{
    /// <summary>
    /// Main window class for the Tic-Tac-Toe WPF application that handles game logic and serial communication
    /// with an external server.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Serial port instance used for communication with the game server.
        /// </summary>
        public SerialPort serialPort;

        /// <summary>
        /// Indicates current player's turn. True represents X's turn, false represents O's turn.
        /// </summary>
        public static bool TURN = true;

        /// <summary>
        /// Flag indicating whether an AI vs AI game is currently in progress.
        /// </summary>
        private bool isAiVsAiGameActive = false;

        /// <summary>
        /// Current game mode. Defaults to hot seat mode (see Constants.HOT_SEAT_MODE).
        /// </summary>
        public static int MODE = Constants.HOT_SEAT_MODE;

        /// <summary>
        /// List containing all game board buttons for easy access and manipulation.
        /// </summary>
        public List<Button> gameButtons;

        /// <summary>
        /// Initializes the main window, sets up COM port connection, and initializes the game board.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            InitializeGameButtons(); // Initialize buttons first

            var comPortSelectionWindow = new ComPortSelectionWindow();
            bool? dialogResult = comPortSelectionWindow.ShowDialog();

            if (dialogResult == true)
            {
                string selectedPort = comPortSelectionWindow.SelectedPort;
                SetupCOM(selectedPort);
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Initializes the game board buttons by adding them to the gameButtons list.
        /// This method is called during window initialization.
        /// </summary>
        public void InitializeGameButtons()
        {
            gameButtons = new List<Button>();

            // Wait for window to load completely
            this.Loaded += (s, e) =>
            {
                gameButtons.Clear(); // Clear any existing buttons
                gameButtons.AddRange(new Button[] {
                    A1, A2, A3,
                    B1, B2, B3,
                    C1, C2, C3
                });
            };
        }

        /// <summary>
        /// Sets up the serial port communication with the game server.
        /// </summary>
        /// <param name="portName">The name of the COM port to connect to</param>
        public void SetupCOM(string portName)
        {
            try
            {
                serialPort = new SerialPort(portName, 9600);
                serialPort.DataReceived += SerialPort_DataReceived;
                serialPort.Open();

                // Send initial game mode to server only after ensuring buttons are initialized
                this.Loaded += (s, e) =>
                {
                    SendCommand($"G,{MODE}");
                };

                MessageBox.Show($"Connected to {portName} successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to {portName}: {ex.Message}");
            }

            this.Closed += MainWindow_Closed;
        }

        /// <summary>
        /// Sends a command to the game server through the serial port.
        /// </summary>
        /// <param name="command">The command string to send</param>
        public void SendCommand(string command)
        {
            try
            {
                if (serialPort?.IsOpen == true)
                {
                    serialPort.WriteLine(command);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending command: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes responses received from the game server.
        /// Handles different command types including AI moves, winner declarations, and ties.
        /// </summary>
        /// <param name="response">The response string received from the server</param>
        public void ProcessServerResponse(string response)
        {
            string[] parts = response.Split(',');
            string command = parts[0];

            switch (command)
            {
                case "A": // AI Move
                    if (parts.Length == 3 && int.TryParse(parts[1], out int row) && int.TryParse(parts[2], out int col))
                    {
                        Button button = gameButtons[row * 3 + col];
                        button.Content = TURN ? Constants.X_SYMBOL : Constants.O_SYMBOL;
                        button.IsEnabled = false;
                        TURN = !TURN;
                    }
                    break;

                case "W": // Winner
                    if (parts.Length == 2)
                    {
                        string winner = parts[1];
                        if (winner == Constants.X_SYMBOL)
                        {
                            int currentWins = Convert.ToInt32(winsX.Content);
                            winsX.Content = (currentWins + 1).ToString();
                        }
                        else if (winner == Constants.O_SYMBOL)
                        {
                            int currentWins = Convert.ToInt32(winsO.Content);
                            winsO.Content = (currentWins + 1).ToString();
                        }
                        DisableAllButtons();
                        MessageBox.Show($"Player {winner} wins!");
                        isAiVsAiGameActive = false;
                        EnableAiVsAiControls();
                    }
                    break;

                case "T": // Tie
                    int currentTies = Convert.ToInt32(ties.Content);
                    ties.Content = (currentTies + 1).ToString();
                    DisableAllButtons();
                    MessageBox.Show("Game ended in a tie!");
                    isAiVsAiGameActive = false;
                    EnableAiVsAiControls();
                    break;

                case "OK":
                    // Command acknowledgment, no action needed
                    break;
            }
        }

        /// <summary>
        /// Event handler for game board button clicks.
        /// Processes player moves and sends them to the server.
        /// </summary>
        /// <param name="sender">The button that was clicked</param>
        /// <param name="e">Event arguments</param>
        public void gameAction_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)sender;
            int index = gameButtons.IndexOf(clickedButton);
            int row = index / 3;
            int col = index % 3;

            clickedButton.Content = TURN ? Constants.X_SYMBOL : Constants.O_SYMBOL;
            clickedButton.IsEnabled = false;
            TURN = !TURN;

            // Send move to server
            SendCommand($"M,{row},{col}");
        }

        /// <summary>
        /// Event handler for the restart button.
        /// Resets the game board while maintaining scores.
        /// </summary>
        /// <param name="sender">The button that was clicked</param>
        /// <param name="e">Event arguments</param>
        private void onRestartButton_Click(object sender, EventArgs e)
        {
            isAiVsAiGameActive = false;
            SendCommand("R");
            EnableAllButtons();
            ClearAllButtons();
            TURN = true;
            EnableAiVsAiControls();
        }

        /// <summary>
        /// Event handler for the game mode selection.
        /// Updates the game mode and sends the change to the server.
        /// </summary>
        /// <param name="sender">The combo box that was changed</param>
        /// <param name="e">Event arguments</param>
        private void gameModeComboBox_Click(object sender, SelectionChangedEventArgs e)
        {
            if (gameModeComboBox.SelectedIndex >= 0)
            {
                isAiVsAiGameActive = false;
                MODE = gameModeComboBox.SelectedIndex;
                SendCommand($"G,{MODE}");

                if (startButton != null && gameModeComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    startButton.IsEnabled = selectedItem.Content.ToString() == "AI vs AI";
                }

                EnableAllButtons();
                ClearAllButtons();
                TURN = true;
            }
        }

        /// <summary>
        /// Event handler for the start button in AI vs AI mode.
        /// Initiates an AI vs AI game session.
        /// </summary>
        /// <param name="sender">The button that was clicked</param>
        /// <param name="e">Event arguments</param>
        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            if (MODE == Constants.AI_VS_AI_MODE && !isAiVsAiGameActive)
            {
                isAiVsAiGameActive = true;
                DisableAiVsAiControls();
                ClearAllButtons();
                DisableAllButtons();
                SendCommand("R"); // Reset the game
                SendCommand("V"); // Start AI vs AI game
            }
        }

        /// <summary>
        /// Event handler for the new game button.
        /// Resets the game board and all scores.
        /// </summary>
        /// <param name="sender">The button that was clicked</param>
        /// <param name="e">Event arguments</param>
        private void newButton_Click(object sender, RoutedEventArgs e)
        {
            SendCommand("R");
            EnableAllButtons();
            ClearAllButtons();
            TURN = true;
            winsX.Content = "0";
            winsO.Content = "0";
            ties.Content = "0";
        }

        /// <summary>
        /// Event handler for the save button.
        /// Saves the current game state to a file.
        /// </summary>
        /// <param name="sender">The button that was clicked</param>
        /// <param name="e">Event arguments</param>
        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[Game]");
            sb.AppendLine("Turn=" + (TURN ? "X" : "O"));
            sb.AppendLine("Mode=" + MODE);
            sb.AppendLine("[Board]");
            foreach (Button button in gameButtons)
            {
                sb.AppendLine(button.Name + "=" + button.Content);
            }
            sb.AppendLine("[Stats]");
            sb.AppendLine("WinsX=" + winsX.Content);
            sb.AppendLine("WinsO=" + winsO.Content);
            sb.AppendLine("Ties=" + ties.Content);

            File.WriteAllText("gameState.ini", sb.ToString());
            MessageBox.Show("Game state saved!");
        }

        /// <summary>
        /// Event handler for the load button.
        /// Loads a previously saved game state from a file.
        /// </summary>
        /// <param name="sender">The button that was clicked</param>
        /// <param name="e">Event arguments</param>
        private void loadButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists("gameState.ini"))
            {
                var parser = new IniParser.FileIniDataParser();
                var data = parser.ReadFile("gameState.ini");

                TURN = data["Game"]["Turn"] == "X";
                MODE = int.Parse(data["Game"]["Mode"]);
                SendCommand($"G,{MODE}");

                foreach (Button button in gameButtons)
                {
                    string content = data["Board"][button.Name];
                    button.Content = content;
                    button.IsEnabled = string.IsNullOrEmpty(content);
                }

                winsX.Content = data["Stats"]["WinsX"];
                winsO.Content = data["Stats"]["WinsO"];
                ties.Content = data["Stats"]["Ties"];

                MessageBox.Show("Game state loaded!");
            }
            else
            {
                MessageBox.Show("No save file found!");
            }
        }

        /// <summary>
        /// Enables all buttons on the game board if they exist.
        /// </summary>
        private void EnableAllButtons()
        {
            if (gameButtons == null) return;
            foreach (Button button in gameButtons)
            {
                if (button != null)
                {
                    button.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Disables all buttons on the game board if they exist.
        /// </summary>
        private void DisableAllButtons()
        {
            if (gameButtons == null) return;
            foreach (Button button in gameButtons)
            {
                if (button != null)
                {
                    button.IsEnabled = false;
                }
            }
        }

        /// <summary>
        /// Clears the content of all buttons on the game board if they exist.
        /// </summary>
        private void ClearAllButtons()
        {
            if (gameButtons == null) return;
            foreach (Button button in gameButtons)
            {
                if (button != null)
                {
                    button.Content = "";
                }
            }
        }

        /// <summary>
        /// Event handler for serial port data reception.
        /// Processes received data and updates the UI through the dispatcher.
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">Event data</param>
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string message = serialPort.ReadLine().Trim();
                Dispatcher.Invoke(() => ProcessServerResponse(message));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Error receiving data: {ex.Message}"));
            }
        }

        /// <summary>
        /// Enables the AI vs AI control buttons when in AI vs AI mode.
        /// </summary>
        private void EnableAiVsAiControls()
        {
            if (MODE == Constants.AI_VS_AI_MODE)
            {
                startButton.IsEnabled = true;
                gameModeComboBox.IsEnabled = true;
            }
        }

        /// <summary>
        /// Disables the AI vs AI control buttons when in AI vs AI mode.
        /// </summary>
        private void DisableAiVsAiControls()
        {
            if (MODE == Constants.AI_VS_AI_MODE)
            {
                startButton.IsEnabled = false;
                gameModeComboBox.IsEnabled = false;
            }
        }

        /// <summary>
        /// Event handler for window closing.
        /// Ensures proper cleanup of serial port connection.
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">Event data</param>
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (serialPort?.IsOpen == true)
            {
                serialPort.Close();
            }
        }
    }
}