
using System;
using System.Linq;
using System.Threading;
using RussianCheckers.AI.Algorithms;
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Models;
using RussianCheckers.Core.Services;
using Xunit;

namespace RussianCheckers.Tests
{
    public class MinimaxTests
    {
        private readonly Minimax _minimax = new();
        private readonly GameEngine _engine = new();

        [Fact]
        public void InitialPosition_ReturnsLegalMove()
        {
            var state = new GameState();

            Move? bestMove = _minimax.FindBestMove(
                state, PieceColor.White, 2);

            Assert.NotNull(bestMove);

            var legalMoves = _engine.GetLegalMoves(state);

            Assert.Contains(legalMoves, m =>
                m.Path.SequenceEqual(bestMove.Path));
        }

        [Fact]
        public void Bot_SelectsMandatoryCapture()
        {
            var state = new GameState
            {
                Board = new Board(),
                CurrentTurn = PieceColor.White
            };

            state.Board.SetPiece(5, 2,
                new Piece(PieceColor.White));

            state.Board.SetPiece(4, 3,
                new Piece(PieceColor.Black));

            Move? move = _minimax.FindBestMove(
                state, PieceColor.White, 2);

            Assert.NotNull(move);
            Assert.True(move.IsCapture);
        }

        [Fact]
        public void NoLegalMoves_ReturnsNull()
        {
            var state = new GameState
            {
                Board = new Board(),
                CurrentTurn = PieceColor.White
            };

            Move? move = _minimax.FindBestMove(
                state, PieceColor.White, 2);

            Assert.Null(move);
        }

        [Fact]
        public void WrongTurn_ThrowsException()
        {
            var state = new GameState();

            Assert.Throws<ArgumentException>(() =>
                _minimax.FindBestMove(
                    state, PieceColor.Black, 2));
        }

        [Fact]
        public void CancelledSearch_ThrowsException()
        {
            var state = new GameState();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                _minimax.FindBestMove(
                    state,
                    PieceColor.White,
                    3,
                    cts.Token));
        }

        [Fact]
        public void Search_DoesNotModifyOriginalBoard()
        {
            var state = new GameState();

            _minimax.FindBestMove(
                state, PieceColor.White, 2);

            Assert.NotNull(state.Board.GetPiece(5, 0));
            Assert.Null(state.Board.GetPiece(4, 1));
            Assert.Equal(PieceColor.White, state.CurrentTurn);
            Assert.Equal(0, state.MoveCount);
        }
    }
}
