
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RussianCheckers.AI.Enums;
using RussianCheckers.AI.Services;
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Models;
using RussianCheckers.Core.Services;
using Xunit;

namespace RussianCheckers.Tests
{
    public class CheckersBotTests
    {
        [Theory]
        [InlineData(BotDifficulty.Easy, 1)]
        [InlineData(BotDifficulty.Medium, 2)]
        [InlineData(BotDifficulty.Hard, 4)]
        public void Difficulty_HasCorrectDepth(
            BotDifficulty difficulty, int expectedDepth)
        {
            var bot = new CheckersBot(
                PieceColor.White, difficulty);

            Assert.Equal(expectedDepth, bot.GetSearchDepth());
        }

        [Fact]
        public async Task Bot_ReturnsLegalMove()
        {
            var bot = new CheckersBot(
                PieceColor.White,
                BotDifficulty.Easy);

            var state = new GameState();

            Move? move = await bot.FindMoveAsync(state);

            Assert.NotNull(move);

            var engine = new GameEngine();
            var legalMoves = engine.GetLegalMoves(state);

            Assert.Contains(legalMoves, m =>
                m.Path.SequenceEqual(move.Path) &&
                m.CapturedPositions.SequenceEqual(
                    move.CapturedPositions));
        }

        [Fact]
        public async Task Bot_DoesNotModifyOriginalState()
        {
            var bot = new CheckersBot(
                PieceColor.White,
                BotDifficulty.Easy);

            var state = new GameState();

            await bot.FindMoveAsync(state);

            Assert.NotNull(state.Board.GetPiece(5, 0));
            Assert.Null(state.Board.GetPiece(4, 1));
            Assert.Equal(PieceColor.White, state.CurrentTurn);
            Assert.Equal(0, state.MoveCount);
        }

        [Fact]
        public async Task Bot_CanBeCancelled()
        {
            var bot = new CheckersBot(
                PieceColor.White,
                BotDifficulty.Hard);

            var state = new GameState();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await bot.FindMoveAsync(
                    state, cts.Token));
        }

        [Fact]
        public async Task Bot_CapturesWhenRequired()
        {
            var bot = new CheckersBot(
                PieceColor.White,
                BotDifficulty.Easy);

            var state = new GameState
            {
                Board = new Board(),
                CurrentTurn = PieceColor.White
            };

            state.Board.SetPiece(5, 2,
                new Piece(PieceColor.White));

            state.Board.SetPiece(4, 3,
                new Piece(PieceColor.Black));

            Move? move = await bot.FindMoveAsync(state);

            Assert.NotNull(move);
            Assert.True(move.IsCapture);
        }
    }
}
