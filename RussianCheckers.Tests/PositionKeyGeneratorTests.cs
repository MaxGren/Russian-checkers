
using Xunit;
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Models;
using RussianCheckers.Core.Services;

namespace RussianCheckers.Tests
{
    public class PositionKeyGeneratorTests
    {
        [Fact]
        public void SamePosition_HasSameKey()
        {
            var state = new GameState();
            var copy = state.Clone();

            Assert.Equal(
                PositionKeyGenerator.Generate(state),
                PositionKeyGenerator.Generate(copy));
        }

        [Fact]
        public void DifferentTurn_HasDifferentKey()
        {
            var state = new GameState();
            var copy = state.Clone();

            copy.CurrentTurn = PieceColor.Black;

            Assert.NotEqual(
                PositionKeyGenerator.Generate(state),
                PositionKeyGenerator.Generate(copy));
        }

        [Fact]
        public void KingAndRegularPiece_HaveDifferentKeys()
        {
            var state = new GameState();

            state.Board.SetPiece(
                5, 0,
                new Piece(PieceColor.White, false));

            var copy = state.Clone();

            copy.Board.SetPiece(
                5, 0,
                new Piece(PieceColor.White, true));

            Assert.NotEqual(
                PositionKeyGenerator.Generate(state),
                PositionKeyGenerator.Generate(copy));
        }
    }
}
