
using Xunit;
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Models;
using RussianCheckers.Core.Services;

namespace RussianCheckers.Tests
{
    public class DrawCounterTests
    {
        private readonly GameEngine _engine = new();

        private static GameState CreateEmptyState()
        {
            return new GameState
            {
                Board = new Board(),
                CurrentTurn = PieceColor.White
            };
        }

        // Выполняет допустимый ход по начальной
        // и конечной клетке
        private void MakeLegalMove(
            GameState state,
            int fromRow,
            int fromCol,
            int toRow,
            int toCol)
        {
            var moves = _engine.GetLegalMoves(state);

            var move = moves.Find(m =>
                m.From == new BoardPosition(fromRow, fromCol) &&
                m.To == new BoardPosition(toRow, toCol));

            Assert.NotNull(move);
            Assert.True(_engine.MakeMove(state, move!));
        }

        [Fact]
        public void RegularMove_IncreasesNoCaptureCounter()
        {
            var state = new GameState();

            MakeLegalMove(state, 5, 0, 4, 1);

            Assert.Equal(1, state.DrawState.NoCapturePlies);
            Assert.Equal(0, state.DrawState.KingOnlyPlies);
        }

        [Fact]
        public void KingMove_IncreasesBothCounters()
        {
            var state = CreateEmptyState();

            state.Board.SetPiece(5, 0,
                new Piece(PieceColor.White, true));

            state.Board.SetPiece(2, 7,
                new Piece(PieceColor.Black, true));

            MakeLegalMove(state, 5, 0, 4, 1);

            Assert.Equal(1, state.DrawState.NoCapturePlies);
            Assert.Equal(1, state.DrawState.KingOnlyPlies);

            Assert.Equal(1,
                state.DrawState.WhiteMovesSinceMaterialChange);
        }

        [Fact]
        public void Capture_ResetsCounters()
        {
            var state = CreateEmptyState();

            state.Board.SetPiece(5, 0,
                new Piece(PieceColor.White));

            state.Board.SetPiece(4, 1,
                new Piece(PieceColor.Black));

            state.DrawState.NoCapturePlies = 8;
            state.DrawState.KingOnlyPlies = 5;

            MakeLegalMove(state, 5, 0, 3, 2);

            Assert.Equal(0, state.DrawState.NoCapturePlies);
            Assert.Equal(0, state.DrawState.KingOnlyPlies);

            Assert.Equal(0,
                state.DrawState.WhiteMovesSinceMaterialChange);

            Assert.Equal(0,
                state.DrawState.BlackMovesSinceMaterialChange);
        }

        [Fact]
        public void RegularPieceMove_ResetsKingCounter()
        {
            var state = new GameState();

            state.DrawState.KingOnlyPlies = 7;

            MakeLegalMove(state, 5, 0, 4, 1);

            Assert.Equal(0, state.DrawState.KingOnlyPlies);
            Assert.Equal(1, state.DrawState.NoCapturePlies);
        }

        [Fact]
        public void Promotion_ResetsMaterialCounters()
        {
            var state = CreateEmptyState();

            state.Board.SetPiece(1, 2,
                new Piece(PieceColor.White));

            state.Board.SetPiece(6, 7,
                new Piece(PieceColor.Black));

            state.DrawState.WhiteMovesSinceMaterialChange = 5;
            state.DrawState.BlackMovesSinceMaterialChange = 4;

            MakeLegalMove(state, 1, 2, 0, 1);

            Piece? piece = state.Board.GetPiece(0, 1);

            Assert.NotNull(piece);
            Assert.True(piece!.IsKing);

            Assert.Equal(0,
                state.DrawState.WhiteMovesSinceMaterialChange);

            Assert.Equal(0,
                state.DrawState.BlackMovesSinceMaterialChange);

            Assert.Equal(0, state.DrawState.KingOnlyPlies);
        }


        [Fact]
        public void MaterialEnding_IsDetectedAfterMove()
        {
            var state = CreateEmptyState();

            state.Board.SetPiece(5, 0,
                new Piece(PieceColor.White, true));

            state.Board.SetPiece(2, 7,
                new Piece(PieceColor.Black, true));

            MakeLegalMove(state, 5, 0, 4, 1);

            Assert.Equal(
                5,
                state.DrawState.MaterialEndingLimit);
        }

        [Fact]
        public void FourPieceEnding_HasCorrectLimits()
        {
            var state = CreateEmptyState();

            state.Board.SetPiece(5, 0,
                new Piece(PieceColor.White, true));

            state.Board.SetPiece(4, 3,
                new Piece(PieceColor.White));

            state.Board.SetPiece(2, 7,
                new Piece(PieceColor.Black, true));

            state.Board.SetPiece(1, 6,
                new Piece(PieceColor.Black));

            MakeLegalMove(state, 5, 0, 6, 1);

            Assert.Equal(
                30,
                state.DrawState.MaterialEndingLimit);

            Assert.Equal(
                15,
                state.DrawState.KingOnlyEndingLimit);
        }

    }
}
