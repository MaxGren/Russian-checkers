
using Xunit;
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Models;
using RussianCheckers.Core.Services;

namespace RussianCheckers.Tests
{
    public class DrawRuleCheckerTests
    {
        private readonly DrawRuleChecker _checker = new();

        private static GameState EmptyState()
        {
            var state = new GameState();
            state.Board = new Board();
            return state;
        }

        [Fact]
        public void InitialBoard_Has24Pieces()
        {
            var state = new GameState();

            var stats = _checker.GetPieceStatistics(state);

            Assert.Equal(12, stats.WhiteMen);
            Assert.Equal(12, stats.BlackMen);

            Assert.Equal(0, stats.WhiteKings);
            Assert.Equal(0, stats.BlackKings);

            Assert.Equal(24, stats.TotalPieces);
        }

        [Fact]
        public void PieceStatistics_CountsKingsCorrectly()
        {
            var state = EmptyState();

            state.Board.SetPiece(
                5, 0,
                new Piece(PieceColor.White, true));

            state.Board.SetPiece(
                4, 1,
                new Piece(PieceColor.White, false));

            state.Board.SetPiece(
                2, 3,
                new Piece(PieceColor.Black, true));

            var stats = _checker.GetPieceStatistics(state);

            Assert.Equal(1, stats.WhiteKings);
            Assert.Equal(1, stats.WhiteMen);
            Assert.Equal(1, stats.BlackKings);
            Assert.Equal(3, stats.TotalPieces);
        }

        [Fact]
        public void BothPlayersHaveKings_DetectsCorrectly()
        {
            var state = EmptyState();

            state.Board.SetPiece(
                5, 0,
                new Piece(PieceColor.White, true));

            state.Board.SetPiece(
                2, 3,
                new Piece(PieceColor.Black, true));

            Assert.True(
                _checker.BothPlayersHaveKings(state));
        }

        [Fact]
        public void ThreeKingsAgainstOne_IsDetected()
        {
            var state = EmptyState();

            state.Board.SetPiece(
                5, 0,
                new Piece(PieceColor.White, true));

            state.Board.SetPiece(
                5, 2,
                new Piece(PieceColor.White, true));

            state.Board.SetPiece(
                5, 4,
                new Piece(PieceColor.White, true));

            state.Board.SetPiece(
                2, 3,
                new Piece(PieceColor.Black, true));

            Assert.True(
                _checker.IsThreeKingsAgainstOne(state));
        }

        [Fact]
        public void ThreeKingsAgainstOne_NotDetectedWithTwoEnemyKings()
        {
            var state = EmptyState();

            state.Board.SetPiece(
                5, 0,
                new Piece(PieceColor.White, true));

            state.Board.SetPiece(
                5, 2,
                new Piece(PieceColor.White, true));

            state.Board.SetPiece(
                5, 4,
                new Piece(PieceColor.White, true));

            state.Board.SetPiece(
                2, 3,
                new Piece(PieceColor.Black, true));

            state.Board.SetPiece(
                2, 5,
                new Piece(PieceColor.Black, true));

            Assert.False(
                _checker.IsThreeKingsAgainstOne(state));
        }


        [Fact]
        public void TwoKings_HaveFiveMoveMaterialLimit()
        {
            var state = EmptyState();

            state.Board.SetPiece(5, 0,
                new Piece(PieceColor.White, true));

            state.Board.SetPiece(2, 7,
                new Piece(PieceColor.Black, true));

            Assert.Equal(
                5,
                _checker.GetMaterialBalanceMoveLimit(state));
        }

        [Fact]
        public void FourPieces_HaveThirtyMoveMaterialLimit()
        {
            var state = EmptyState();

            state.Board.SetPiece(5, 0,
                new Piece(PieceColor.White, true));

            state.Board.SetPiece(4, 3,
                new Piece(PieceColor.White));

            state.Board.SetPiece(2, 7,
                new Piece(PieceColor.Black, true));

            state.Board.SetPiece(1, 6,
                new Piece(PieceColor.Black));

            Assert.Equal(
                30,
                _checker.GetMaterialBalanceMoveLimit(state));

            Assert.Equal(
                15,
                _checker.GetKingOnlyMoveLimit(state));
        }

        [Fact]
        public void SixPieces_HaveSixtyAndTwentyFiveMoveLimits()
        {
            var state = EmptyState();

            state.Board.SetPiece(5, 0,
                new Piece(PieceColor.White, true));

            state.Board.SetPiece(4, 3,
                new Piece(PieceColor.White));

            state.Board.SetPiece(6, 1,
                new Piece(PieceColor.White));

            state.Board.SetPiece(2, 7,
                new Piece(PieceColor.Black, true));

            state.Board.SetPiece(1, 6,
                new Piece(PieceColor.Black));

            state.Board.SetPiece(3, 4,
                new Piece(PieceColor.Black));

            Assert.Equal(
                60,
                _checker.GetMaterialBalanceMoveLimit(state));

            Assert.Equal(
                25,
                _checker.GetKingOnlyMoveLimit(state));
        }

        [Fact]
        public void WithoutKings_MaterialLimitDoesNotApply()
        {
            var state = EmptyState();

            state.Board.SetPiece(5, 0,
                new Piece(PieceColor.White));

            state.Board.SetPiece(2, 7,
                new Piece(PieceColor.Black));

            Assert.Null(
                _checker.GetMaterialBalanceMoveLimit(state));
        }

        [Fact]
        public void KingOnBigRoad_IsDetected()
        {
            var state = EmptyState();

            // На диагонали a1-h8
            state.Board.SetPiece(5, 2,
                new Piece(PieceColor.White, true));

            // Вне большой дороги
            state.Board.SetPiece(2, 7,
                new Piece(PieceColor.Black, true));

            Assert.True(
                _checker.HasKingOnBigRoad(
                    state, PieceColor.White));

            Assert.False(
                _checker.HasKingOnBigRoad(
                    state, PieceColor.Black));
        }


        [Fact]
        public void FourPieceEnding_ReachesDrawAfterThirtyMoves()
        {
            var state = EmptyState();

            state.Board.SetPiece(5, 0,
                new Piece(PieceColor.White, true));

            state.Board.SetPiece(4, 3,
                new Piece(PieceColor.White));

            state.Board.SetPiece(2, 7,
                new Piece(PieceColor.Black, true));

            state.Board.SetPiece(1, 6,
                new Piece(PieceColor.Black));

            state.DrawState.MaterialEndingLimit = 30;
            state.DrawState.WhiteMovesSinceMaterialChange = 30;
            state.DrawState.BlackMovesSinceMaterialChange = 30;

            Assert.NotNull(
                _checker.GetReachedDrawReason(state));
        }

        [Fact]
        public void FourPieceEnding_DoesNotDrawTooEarly()
        {
            var state = EmptyState();

            state.Board.SetPiece(5, 0,
                new Piece(PieceColor.White, true));

            state.Board.SetPiece(4, 3,
                new Piece(PieceColor.White));

            state.Board.SetPiece(2, 7,
                new Piece(PieceColor.Black, true));

            state.Board.SetPiece(1, 6,
                new Piece(PieceColor.Black));

            state.DrawState.MaterialEndingLimit = 30;
            state.DrawState.WhiteMovesSinceMaterialChange = 30;
            state.DrawState.BlackMovesSinceMaterialChange = 29;

            Assert.Null(
                _checker.GetReachedDrawReason(state));
        }

        [Fact]
        public void KingOnlyEnding_ReachesDrawAfterFifteenFullMoves()
        {
            var state = EmptyState();

            state.Board.SetPiece(5, 0,
                new Piece(PieceColor.White, true));

            state.Board.SetPiece(4, 3,
                new Piece(PieceColor.White));

            state.Board.SetPiece(2, 7,
                new Piece(PieceColor.Black, true));

            state.Board.SetPiece(1, 6,
                new Piece(PieceColor.Black));

            state.DrawState.KingOnlyEndingLimit = 15;
            state.DrawState.KingOnlyPlies = 30;

            Assert.NotNull(
                _checker.GetReachedDrawReason(state));
        }


    }
}
