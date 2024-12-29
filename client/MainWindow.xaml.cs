using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.IO.Ports;

namespace TicTacToeWPF
{
    public partial class MainWindow : Window
    {
        public SerialPort serialPort;
        public static bool TURN = true;  // true = X / false = O
        private bool isAiVsAiGameActive = false;
        public static int MODE = Constants.HOT_SEAT_MODE;
        public List<Button> gameButtons;

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

        private void EnableAiVsAiControls()
        {
            if (MODE == Constants.AI_VS_AI_MODE)
            {
                startButton.IsEnabled = true;
                gameModeComboBox.IsEnabled = true;
            }
        }

        private void DisableAiVsAiControls()
        {
            if (MODE == Constants.AI_VS_AI_MODE)
            {
                startButton.IsEnabled = false;
                gameModeComboBox.IsEnabled = false;
            }
        }

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

        private void onRestartButton_Click(object sender, EventArgs e)
        {
            isAiVsAiGameActive = false;
            SendCommand("R");
            EnableAllButtons();
            ClearAllButtons();
            TURN = true;
            EnableAiVsAiControls();
        }

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

        // Save/Load functionality remains unchanged as it's client-side only
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

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (serialPort?.IsOpen == true)
            {
                serialPort.Close();
            }
        }
    }
}