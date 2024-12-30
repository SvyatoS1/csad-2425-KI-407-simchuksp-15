#include <ArduinoUnit.h>

// Game constants and variables (matching server.ino)
const char EMPTY = ' ';
const char X_SYMBOL = 'X';
const char O_SYMBOL = 'O';

const int HOT_SEAT_MODE = 0;
const int AI_EASY_MODE = 1;
const int AI_HARD_MODE = 2;
const int AI_VS_AI_MODE = 3;

char board[3][3];
int currentMode = HOT_SEAT_MODE;
bool isXTurn = true;
bool aiVsAiGameInProgress = false;
int winsX = 0;
int winsO = 0;
int ties = 0;

// Required struct definition
struct Move {
    int row;
    int col;
};

// Function prototypes
void resetBoard();
bool isValidMove(int row, int col);
bool makeMove(int row, int col);
char checkWinner();
bool isBoardFull();
Move getRandomMove();
bool isWinningMove(int row, int col, char symbol);
Move getBestMove();
void processCommand(String command);
void updateStats(char winner);

// Implementation of required functions
void resetBoard() {
    for (int i = 0; i < 3; i++) {
        for (int j = 0; j < 3; j++) {
            board[i][j] = EMPTY;
        }
    }
    isXTurn = true;
}

bool isValidMove(int row, int col) {
    return row >= 0 && row < 3 && col >= 0 && col < 3 && board[row][col] == EMPTY;
}

bool makeMove(int row, int col) {
    if (!isValidMove(row, col)) return false;
    board[row][col] = isXTurn ? X_SYMBOL : O_SYMBOL;
    isXTurn = !isXTurn;
    return true;
}

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

bool isBoardFull() {
    for (int i = 0; i < 3; i++) {
        for (int j = 0; j < 3; j++) {
            if (board[i][j] == EMPTY) return false;
        }
    }
    return true;
}

Move getRandomMove() {
    Move move;
    do {
        move.row = random(3);
        move.col = random(3);
    } while (!isValidMove(move.row, move.col));
    return move;
}

bool isWinningMove(int row, int col, char symbol) {
    if (!isValidMove(row, col)) return false;
    board[row][col] = symbol;
    bool wins = checkWinner() == symbol;
    board[row][col] = EMPTY;
    return wins;
}

Move getBestMove() {
    char currentSymbol = isXTurn ? X_SYMBOL : O_SYMBOL;
    Move move;
    
    // Check for winning move
    for (int i = 0; i < 3; i++) {
        for (int j = 0; j < 3; j++) {
            if (isValidMove(i, j) && isWinningMove(i, j, currentSymbol)) {
                move.row = i;
                move.col = j;
                return move;
            }
        }
    }
    
    // Check for blocking move
    char opponent = currentSymbol == X_SYMBOL ? O_SYMBOL : X_SYMBOL;
    for (int i = 0; i < 3; i++) {
        for (int j = 0; j < 3; j++) {
            if (isValidMove(i, j) && isWinningMove(i, j, opponent)) {
                move.row = i;
                move.col = j;
                return move;
            }
        }
    }
    
    // Take center if available
    if (isValidMove(1, 1)) {
        move.row = 1;
        move.col = 1;
        return move;
    }
    
    return getRandomMove();
}

void updateStats(char winner) {
    if (winner == X_SYMBOL) winsX++;
    else if (winner == O_SYMBOL) winsO++;
    else ties++;
}

// Test cases
test(test_board_initialization) {
    resetBoard();
    for (int i = 0; i < 3; i++) {
        for (int j = 0; j < 3; j++) {
            assertEqual(board[i][j], EMPTY);
        }
    }
    assertTrue(isXTurn);
}

test(test_valid_moves) {
    resetBoard();
    assertTrue(isValidMove(0, 0));
    assertTrue(isValidMove(2, 2));
    
    makeMove(0, 0);
    assertFalse(isValidMove(0, 0));
    assertFalse(isValidMove(-1, 0));
    assertFalse(isValidMove(3, 0));
}

test(test_make_move) {
    resetBoard();
    assertTrue(makeMove(0, 0));
    assertEqual(board[0][0], X_SYMBOL);
    assertFalse(isXTurn);
    
    assertTrue(makeMove(1, 1));
    assertEqual(board[1][1], O_SYMBOL);
    assertTrue(isXTurn);
}

test(test_check_winner_row1) {
    resetBoard();
    board[0][0] = X_SYMBOL;
    board[0][1] = X_SYMBOL;
    board[0][2] = X_SYMBOL;
    assertEqual(checkWinner(), X_SYMBOL);
}

test(test_check_winner_row2) {
    resetBoard();
    board[1][0] = O_SYMBOL;
    board[1][1] = O_SYMBOL;
    board[1][2] = O_SYMBOL;
    assertEqual(checkWinner(), O_SYMBOL);
}

test(test_check_winner_row3) {
    resetBoard();
    board[2][0] = X_SYMBOL;
    board[2][1] = X_SYMBOL;
    board[2][2] = X_SYMBOL;
    assertEqual(checkWinner(), X_SYMBOL);
}

test(test_check_winner_col1) {
    resetBoard();
    board[0][0] = O_SYMBOL;
    board[1][0] = O_SYMBOL;
    board[2][0] = O_SYMBOL;
    assertEqual(checkWinner(), O_SYMBOL);
}

test(test_check_winner_col2) {
    resetBoard();
    board[0][1] = X_SYMBOL;
    board[1][1] = X_SYMBOL;
    board[2][1] = X_SYMBOL;
    assertEqual(checkWinner(), X_SYMBOL);
}

test(test_check_winner_col3) {
    resetBoard();
    board[0][2] = O_SYMBOL;
    board[1][2] = O_SYMBOL;
    board[2][2] = O_SYMBOL;
    assertEqual(checkWinner(), O_SYMBOL);
}

test(test_check_winner_diagonal1) {
    resetBoard();
    board[0][0] = X_SYMBOL;
    board[1][1] = X_SYMBOL;
    board[2][2] = X_SYMBOL;
    assertEqual(checkWinner(), X_SYMBOL);
}

test(test_check_winner_diagonal2) {
    resetBoard();
    board[0][2] = O_SYMBOL;
    board[1][1] = O_SYMBOL;
    board[2][0] = O_SYMBOL;
    assertEqual(checkWinner(), O_SYMBOL);
}

test(test_no_winner) {
    resetBoard();
    board[0][0] = X_SYMBOL;
    board[0][1] = O_SYMBOL;
    board[0][2] = X_SYMBOL;
    board[1][0] = X_SYMBOL;
    board[1][1] = O_SYMBOL;
    assertEqual(checkWinner(), EMPTY);
}

test(test_draw_game) {
    resetBoard();
    board[0][0] = X_SYMBOL; board[0][1] = O_SYMBOL; board[0][2] = X_SYMBOL;
    board[1][0] = X_SYMBOL; board[1][1] = O_SYMBOL; board[1][2] = X_SYMBOL;
    board[2][0] = O_SYMBOL; board[2][1] = X_SYMBOL; board[2][2] = O_SYMBOL;
    assertEqual(checkWinner(), EMPTY);
    assertTrue(isBoardFull());
}

test(test_empty_board) {
    resetBoard();
    assertFalse(isBoardFull());
    assertEqual(checkWinner(), EMPTY);
}

test(test_partial_board) {
    resetBoard();
    makeMove(0, 0);
    makeMove(1, 1);
    assertFalse(isBoardFull());
    assertEqual(checkWinner(), EMPTY);
}

test(test_game_stats) {
    resetBoard();
    winsX = 0; winsO = 0; ties = 0;
    
    // X wins
    updateStats(X_SYMBOL);
    assertEqual(winsX, 1);
    assertEqual(winsO, 0);
    assertEqual(ties, 0);
    
    // O wins
    updateStats(O_SYMBOL);
    assertEqual(winsX, 1);
    assertEqual(winsO, 1);
    assertEqual(ties, 0);
    
    // Tie
    updateStats(EMPTY);
    assertEqual(winsX, 1);
    assertEqual(winsO, 1);
    assertEqual(ties, 1);
}

test(test_random_move_availability) {
    resetBoard();
    Move move = getRandomMove();
    assertTrue(isValidMove(move.row, move.col));
}

test(test_winning_move_detection) {
    resetBoard();
    board[0][0] = X_SYMBOL;
    board[0][1] = X_SYMBOL;
    assertTrue(isWinningMove(0, 2, X_SYMBOL));
}

test(test_blocking_move_detection) {
    resetBoard();
    board[0][0] = O_SYMBOL;
    board[0][1] = O_SYMBOL;
    assertTrue(isWinningMove(0, 2, O_SYMBOL));
}

test(test_best_move_win) {
    resetBoard();
    board[0][0] = X_SYMBOL;
    board[0][1] = X_SYMBOL;
    isXTurn = true;
    Move move = getBestMove();
    assertEqual(move.row, 0);
    assertEqual(move.col, 2);
}

test(test_best_move_block) {
    resetBoard();
    board[0][0] = O_SYMBOL;
    board[0][1] = O_SYMBOL;
    isXTurn = true;
    Move move = getBestMove();
    assertEqual(move.row, 0);
    assertEqual(move.col, 2);
}

test(test_best_move_center) {
    resetBoard();
    Move move = getBestMove();
    assertEqual(move.row, 1);
    assertEqual(move.col, 1);
}

test(test_best_move_random) {
    resetBoard();
    board[1][1] = X_SYMBOL;  // Center taken
    Move move = getBestMove();
    assertTrue(isValidMove(move.row, move.col));
}

test(test_game_mode_switching) {
    currentMode = HOT_SEAT_MODE;
    assertEqual(currentMode, HOT_SEAT_MODE);
    
    currentMode = AI_EASY_MODE;
    assertEqual(currentMode, AI_EASY_MODE);
    
    currentMode = AI_HARD_MODE;
    assertEqual(currentMode, AI_HARD_MODE);
    
    currentMode = AI_VS_AI_MODE;
    assertEqual(currentMode, AI_VS_AI_MODE);
}

void setup() {
    Serial.begin(9600);
    while(!Serial) {} // Wait for serial connection
    randomSeed(analogRead(0)); // Initialize random seed
    Serial.println("Starting tests...");
}

void loop() {
    Test::run();
}