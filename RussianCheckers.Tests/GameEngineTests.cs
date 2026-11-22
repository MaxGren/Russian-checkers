
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Models;
using RussianCheckers.Core.Services;
using Xunit;

namespace RussianCheckers.Tests
{
    public class GameEngineTests
    {
        private readonly GameEngine _engine = new();

        [Fact]
        public void ValidMove_ChangesPiecePosition()
        {
            var state = new GameState();

            var move = _engine.GetLegalMoves(state)
                .Find(m =>
                    m.From == new BoardPosition(5, 0) &&
                    m.To == new BoardPosition(4, 1));

            Assert.NotNull(move);
            Assert.True(_engine.MakeMove(state, move));

            Assert.Null(state.Board.GetPiece(5, 0));

            Assert.NotNull(state.Board.GetPiece(4, 1));

            Assert.Equal(
                PieceColor.Black,
                state.CurrentTurn);
        }

        [Fact]
        public void Capture_RemovesEnemyPiece()
        {
            var state = new GameState();

            state.Board = new Board();

            state.Board.SetPiece(5, 2,
                new Piece(PieceColor.White));

            state.Board.SetPiece(4, 3,
                new Piece(PieceColor.Black));

            var move = _engine.GetLegalMoves(state)[0];

            Assert.True(_engine.MakeMove(state, move));

            Assert.Null(state.Board.GetPiece(4, 3));

            Assert.NotNull(state.Board.GetPiece(3, 4));
        }

        [Fact]
        public void InvalidMove_IsRejected()
        {
            var state = new GameState();

            var invalidMove = new Move(
                new BoardPosition(5, 0));

            invalidMove.Path.Add(
                new BoardPosition(2, 3));

            Assert.False(
                _engine.MakeMove(state, invalidMove));

            Assert.NotNull(
                state.Board.GetPiece(5, 0));

            Assert.Equal(
                PieceColor.White,
                state.CurrentTurn);
        }

        [Fact]
        public void ReachingLastRow_CreatesKing()
        {
            var state = new GameState();

            state.Board = new Board();

            state.Board.SetPiece(1, 2,
                new Piece(PieceColor.White));

            var move = _engine.GetLegalMoves(state)
                .Find(m =>
                    m.To == new BoardPosition(0, 1));

            Assert.NotNull(move);
            Assert.True(_engine.MakeMove(state, move));

            Piece? piece = state.Board.GetPiece(0, 1);

            Assert.NotNull(piece);
            Assert.True(piece.IsKing);
        }

        [Fact]
        public void NoLegalMoves_OpponentWins()
        {
            var state = new GameState();

            state.Board = new Board();

            state.Board.SetPiece(0, 1,
                new Piece(PieceColor.White));

            state.CurrentTurn = PieceColor.White;

            Assert.Equal(
                PieceColor.Black,
                _engine.GetWinner(state));
        }

        [Fact]
        public void MoveCount_IncreasesAfterMove()
        {
            var state = new GameState();

            var move = _engine.GetLegalMoves(state)[0];

            Assert.True(_engine.MakeMove(state, move));

            Assert.Equal(1, state.MoveCount);
        }

        [Fact]
        public void IncompleteCaptureSequence_IsRejected()
        {
            var state = new GameState
            {
                Board = new Board(),
                CurrentTurn = PieceColor.White
            };

            state.Board.SetPiece(5, 0,
                new Piece(PieceColor.White));

            state.Board.SetPiece(4, 1,
                new Piece(PieceColor.Black));

            state.Board.SetPiece(2, 3,
                new Piece(PieceColor.Black));

            // Игрок пытается остановиться
            // после первого взятия
            var incompleteMove = new Move(
                new BoardPosition(5, 0));

            incompleteMove.Path.Add(
                new BoardPosition(3, 2));

            incompleteMove.CapturedPositions.Add(
                new BoardPosition(4, 1));

            bool result = _engine.MakeMove(
                state, incompleteMove);

            // Такой ход запрещён
            Assert.False(result);

            // Доска не должна измениться
            Assert.NotNull(
                state.Board.GetPiece(5, 0));

            Assert.NotNull(
                state.Board.GetPiece(4, 1));

            Assert.NotNull(
                state.Board.GetPiece(2, 3));

            Assert.Null(
                state.Board.GetPiece(3, 2));

            Assert.Equal(0, state.MoveCount);

            Assert.Equal(
                PieceColor.White,
                state.CurrentTurn);
        }

    }
}
