using System;
using TicTacToeWPF;
using Xunit;

namespace TicTacToeTests
{
    public class GameStatusTests
    {
        [Fact]
        public void TestDefaultConstructor_ShouldInitializeDefaultValues()
        {
            // Arrange
            var gameStatus = new GameStatus();

            // Assert
            Assert.False(gameStatus.isGameOver()); // Default is false
            Assert.Null(gameStatus.getWinner()); // Default is null
            Assert.False(gameStatus.isTie()); // Default is false
        }

        [Fact]
        public void TestParameterizedConstructor_ShouldInitializeWithValues()
        {
            // Arrange
            var gameStatus = new GameStatus(true, "X", true);

            // Assert
            Assert.True(gameStatus.isGameOver()); // Game over should be true
            Assert.Equal("X", gameStatus.getWinner()); // Winner should be "X"
            Assert.True(gameStatus.isTie()); // Tie should be true
        }

        [Fact]
        public void TestSetGameOver_ShouldUpdateGameOverStatus()
        {
            // Arrange
            var gameStatus = new GameStatus();

            // Act
            gameStatus.setGameOver(true);

            // Assert
            Assert.True(gameStatus.isGameOver()); // Game over should be true
        }

        [Fact]
        public void TestSetWinner_ShouldUpdateWinner()
        {
            // Arrange
            var gameStatus = new GameStatus();

            // Act
            gameStatus.setWinner("O");

            // Assert
            Assert.Equal("O", gameStatus.getWinner()); // Winner should be "O"
        }

        [Fact]
        public void TestSetTie_ShouldUpdateTieStatus()
        {
            // Arrange
            var gameStatus = new GameStatus();

            // Act
            gameStatus.setTie(true);

            // Assert
            Assert.True(gameStatus.isTie()); // Tie should be true
        }

        [Fact]
        public void TestSetTie_ShouldNotChangeGameOver()
        {
            // Arrange
            var gameStatus = new GameStatus();

            // Act
            gameStatus.setGameOver(true);
            gameStatus.setTie(true);

            // Assert
            Assert.True(gameStatus.isGameOver()); // Game over should be true
            Assert.True(gameStatus.isTie()); // Tie should be true
        }

        [Fact]
        public void TestWinnerShouldBeNull_WhenGameNotOver()
        {
            // Arrange
            var gameStatus = new GameStatus();

            // Act
            gameStatus.setGameOver(false);
            string winner = gameStatus.getWinner();

            // Assert
            Assert.Null(winner); // Winner should be null when game is not over
        }

        [Fact]
        public void TestTieShouldBeFalse_WhenGameNotOver()
        {
            // Arrange
            var gameStatus = new GameStatus();

            // Act
            gameStatus.setGameOver(false);
            bool tieStatus = gameStatus.isTie();

            // Assert
            Assert.False(tieStatus); // Tie should be false when game is not over
        }
    }
}