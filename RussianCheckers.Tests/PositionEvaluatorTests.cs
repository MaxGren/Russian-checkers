
using RussianCheckers.AI.Evaluation;
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Models;
using Xunit;

namespace RussianCheckers.Tests
{
    public class PositionEvaluatorTests
    {
        private readonly PositionEvaluator _evaluator = new();

        [Fact]
        public void EmptyBoard_HasZeroScore()
        {
            var board = new Board();

            int score = _evaluator.Evaluate(
                board, PieceColor.White);

            Assert.Equal(0, score);
        }

        [Fact]
        public void ExtraPiece_GivesPositiveScore()
        {
            var board = new Board();

            board.SetPiece(5, 2,
                new Piece(PieceColor.White));

            int score = _evaluator.Evaluate(
                board, PieceColor.White);

            Assert.True(score > 0);
        }

        [Fact]
        public void EnemyPiece_GivesNegativeScore()
        {
            var board = new Board();

            board.SetPiece(2, 3,
                new Piece(PieceColor.Black));

            int score = _evaluator.Evaluate(
                board, PieceColor.White);

            Assert.True(score < 0);
        }

        [Fact]
        public void King_IsMoreValuableThanRegularPiece()
        {
            var board = new Board();

            board.SetPiece(3, 2,
                new Piece(PieceColor.White));

            int regularScore = _evaluator.Evaluate(
                board, PieceColor.White);

            board.SetPiece(3, 2,
                new Piece(PieceColor.White, true));

            int kingScore = _evaluator.Evaluate(
                board, PieceColor.White);

            Assert.True(kingScore > regularScore);
        }

        [Fact]
        public void Evaluation_IsOppositeForDifferentColors()
        {
            var board = new Board();

            board.SetPiece(5, 2,
                new Piece(PieceColor.White));

            int whiteScore = _evaluator.Evaluate(
                board, PieceColor.White);

            int blackScore = _evaluator.Evaluate(
                board, PieceColor.Black);

            Assert.Equal(-whiteScore, blackScore);
        }
    }
}
