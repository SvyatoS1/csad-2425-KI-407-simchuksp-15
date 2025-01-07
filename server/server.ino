#include <Arduino.h>

/** @name Game Constants
 *  @{
 */
const char EMPTY = ' ';      ///< Represents an empty cell on the board
const char X_SYMBOL = 'X';   ///< Symbol for player X
const char O_SYMBOL = 'O';   ///< Symbol for player O
/** @} */

/** @name Game Modes
 *  @{
 */
const int HOT_SEAT_MODE = 0; ///< Two players on same device
const int AI_EASY_MODE = 1;  ///< Play against AI with random moves
const int AI_HARD_MODE = 2;  ///< Play against AI with strategic moves
const int AI_VS_AI_MODE = 3; ///< Watch two AIs play against each other
/** @} */

/** @name Game State Variables
 *  @{
 */
char board[3][3];               ///< Game board state
int currentMode = HOT_SEAT_MODE;///< Current game mode
bool isXTurn = true;           ///< Tracks current player's turn
bool aiVsAiGameInProgress = false; ///< AI vs AI game status
int winsX = 0;                 ///< Number of X wins
int winsO = 0;                 ///< Number of O wins
int ties = 0;                  ///< Number of ties
/** @} */

/** @name Command Codes
 *  @{
 */
const char CMD_MOVE = 'M';     ///< Move command: M,row,col
const char CMD_MODE = 'G';     ///< Game mode command: G,mode
const char CMD_RESET = 'R';    ///< Reset command: R
const char CMD_STATUS = 'S';   ///< Status request: S
const char CMD_AI_MOVE = 'A';  ///< Request AI move: A
const char CMD_AI_VS_AI = 'V'; ///< Start AI vs AI game: V
/** @} */

/**
 * @struct Move
 * @brief Represents a move on the game board
 */
struct Move {
    int row; ///< Row position (0-2)
    int col; ///< Column position (0-2)
};

/**
 * @brief Initialize serial communication and game board
 */
void setup() {
  Serial.begin(9600);
  resetBoard();
}

/**
 * @brief Reset the game board to initial state
 */
void resetBoard() {
  for (int i = 0; i < 3; i++) {
    for (int j = 0; j < 3; j++) {
      board[i][j] = EMPTY;
    }
  }
  isXTurn = true;
}

/**
 * @brief Check if a move is valid
 * @param row Row position (0-2)
 * @param col Column position (0-2)
 * @return true if move is valid, false otherwise
 */
bool isValidMove(int row, int col) {
  return row >= 0 && row < 3 && col >= 0 && col < 3 && board[row][col] == EMPTY;
}

/**
 * @brief Execute a move on the board
 * @param row Row position (0-2)
 * @param col Column position (0-2)
 * @return true if move was successful, false otherwise
 */
bool makeMove(int row, int col) {
  if (!isValidMove(row, col)) return false;
  
  board[row][col] = isXTurn ? X_SYMBOL : O_SYMBOL;
  isXTurn = !isXTurn;
  return true;
}

/**
 * @brief Check for a winner
 * @return Winner symbol (X/O) or EMPTY if no winner
 */
char checkWinner() {
  // Check rows
  for (int i = 0; i < 3; i++) {
    if (board[i][0] != EMPTY && board[i][0] == board[i][1] && board[i][0] == board[i][2]) {
      return board[i][0];
    }
  }
  
  // Check columns
  for (int i = 0; i < 3; i++) {
    if (board[0][i] != EMPTY && board[0][i] == board[1][i] && board[0][i] == board[2][i]) {
      return board[0][i];
    }
  }
  
  // Check diagonals
  if (board[0][0] != EMPTY && board[0][0] == board[1][1] && board[0][0] == board[2][2]) {
    return board[0][0];
  }
  if (board[0][2] != EMPTY && board[0][2] == board[1][1] && board[0][2] == board[2][0]) {
    return board[0][2];
  }
  
  return EMPTY;
}

/**
 * @brief Check if board is full
 * @return true if board is full, false otherwise
 */
bool isBoardFull() {
  for (int i = 0; i < 3; i++) {
    for (int j = 0; j < 3; j++) {
      if (board[i][j] == EMPTY) return false;
    }
  }
  return true;
}

/**
 * @brief Generate a random valid move
 * @return Move struct containing row and column
 */
Move getRandomMove() {
  Move move;
  do {
    move.row = random(3);
    move.col = random(3);
  } while (!isValidMove(move.row, move.col));
  return move;
}

/**
 * @brief Check if a move would result in a win
 * @param row Row position (0-2)
 * @param col Column position (0-2)
 * @param symbol Player symbol (X/O)
 * @return true if move would win game, false otherwise
 */
bool isWinningMove(int row, int col, char symbol) {
  // Try the move
  board[row][col] = symbol;
  bool isWinning = (checkWinner() == symbol);
  board[row][col] = EMPTY; // Undo the move
  return isWinning;
}

/**
 * @brief Calculate best strategic move for AI
 * @return Move struct containing row and column
 */
Move getBestMove() {
  Move move;
  char currentSymbol = isXTurn ? X_SYMBOL : O_SYMBOL;
  
  // First, check for winning move
  for (int i = 0; i < 3; i++) {
    for (int j = 0; j < 3; j++) {
      if (isValidMove(i, j) && isWinningMove(i, j, currentSymbol)) {
        move.row = i;
        move.col = j;
        return move;
      }
    }
  }
  
  // Then, block opponent's winning move
  char opponentSymbol = isXTurn ? O_SYMBOL : X_SYMBOL;
  for (int i = 0; i < 3; i++) {
    for (int j = 0; j < 3; j++) {
      if (isValidMove(i, j) && isWinningMove(i, j, opponentSymbol)) {
        move.row = i;
        move.col = j;
        return move;
      }
    }
  }
  
  // If no winning moves, try to take center
  if (isValidMove(1, 1)) {
    move.row = 1;
    move.col = 1;
    return move;
  }
  
  // If no strategic moves available, make a random move
  return getRandomMove();
}

/**
 * @brief Execute AI move and send to client
 */
void performAIMove() {
  Move move;
  // Use appropriate AI strategy based on current player
  if (currentMode == AI_EASY_MODE || 
      (currentMode == AI_VS_AI_MODE && isXTurn)) { // X player uses random moves
    move = getRandomMove();
  } else { // O player uses strategic moves or AI_HARD_MODE
    move = getBestMove();
  }
  
  makeMove(move.row, move.col);
  
  // Send AI move to client
  Serial.print("A,");
  Serial.print(move.row);
  Serial.print(",");
  Serial.println(move.col);
}

/**
 * @brief Update game statistics
 * @param winner Winner symbol (X/O) or EMPTY for tie
 */
void updateStats(char winner) {
  if (winner == X_SYMBOL) winsX++;
  else if (winner == O_SYMBOL) winsO++;
  else ties++;
}

/**
 * @brief Send current game status to client
 */
void sendGameStatus() {
  char winner = checkWinner();
  bool gameOver = (winner != EMPTY || isBoardFull());
  
  if (gameOver) {
    if (winner != EMPTY) {
      updateStats(winner);
      Serial.print("W,");
      Serial.println(winner);
    } else if (isBoardFull()) {
      updateStats(EMPTY);
      Serial.println("T"); // Tie
    }
  }
}

/**
 * @brief Process incoming commands from client
 * @param command Command string received via serial
 */
void processCommand(String command) {
  char cmd = command.charAt(0);
  command = command.substring(2); // Skip command char and comma
  
  switch(cmd) {
    case CMD_MOVE: {
      int row = command.charAt(0) - '0';
      int col = command.charAt(2) - '0';
      if (makeMove(row, col)) {
        sendGameStatus();
        
        // If it's AI's turn and game isn't over
        if ((currentMode == AI_EASY_MODE || currentMode == AI_HARD_MODE) && 
            checkWinner() == EMPTY && !isBoardFull()) {
          performAIMove();
          sendGameStatus();
        }
      }
      break;
    }
    
    case CMD_MODE:
      currentMode = command.toInt();
      aiVsAiGameInProgress = false; // Reset AI vs AI state when mode changes
      resetBoard();
      Serial.println("OK");
      break;
      
    case CMD_RESET:
      resetBoard();
      aiVsAiGameInProgress = false; // Reset AI vs AI state on reset
      winsX = 0;
      winsO = 0;
      ties = 0;
      Serial.println("OK");
      break;
      
    case CMD_STATUS:
      sendGameStatus();
      break;
      
    case CMD_AI_MOVE:
      if (currentMode == AI_VS_AI_MODE) {
        performAIMove();
        sendGameStatus();
      }
      break;
      
    case CMD_AI_VS_AI:
  if (currentMode == AI_VS_AI_MODE) {
    aiVsAiGameInProgress = true;
    resetBoard(); // Ensure clean board state
    
    // First move (X's turn)
    performAIMove();
    sendGameStatus();
    
    while (aiVsAiGameInProgress) {
      // Check if game should continue
      if (checkWinner() != EMPTY || isBoardFull()) {
        aiVsAiGameInProgress = false;
        break;
      }
      
      delay(1000); // Delay between moves
      
      // Only make next move if game is still in progress
      if (aiVsAiGameInProgress) {
        performAIMove();
        sendGameStatus();
      }
    }
  }
  break;
  }
}

/**
 * @brief Main program loop
 */
void loop() {
  if (Serial.available() > 0) {
    String command = Serial.readStringUntil('\n');
    command.trim();
    if (command.length() > 0) {
      processCommand(command);
    }
  }
}