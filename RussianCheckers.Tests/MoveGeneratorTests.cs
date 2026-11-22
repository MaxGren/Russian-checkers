
using System.Linq;
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Models;
using RussianCheckers.Core.Services;
using Xunit;

namespace RussianCheckers.Tests
{
    public class MoveGeneratorTests
    {
        private readonly MoveGenerator _generator = new();

        [Fact]
        public void InitialPosition_WhiteHasSevenMoves()
        {
            var board = new Board();
            board.Initialize();

            var moves = _generator.GetLegalMoves(
                board, PieceColor.White);

            Assert.Equal(7, moves.Count);
        }

        [Fact]
        public void RegularPiece_CanMoveForward()
        {
            var board = new Board();

            board.SetPiece(5, 2,
                new Piece(PieceColor.White));

            var moves = _generator.GetLegalMoves(
                board, PieceColor.White);

            Assert.Equal(2, moves.Count);

            Assert.Contains(moves,
                m => m.To == new BoardPosition(4, 1));

            Assert.Contains(moves,
                m => m.To == new BoardPosition(4, 3));
        }

        [Fact]
        public void Capture_IsMandatory()
        {
            var board = new Board();

            board.SetPiece(5, 2,
                new Piece(PieceColor.White));

            board.SetPiece(4, 3,
                new Piece(PieceColor.Black));

            var moves = _generator.GetLegalMoves(
                board, PieceColor.White);

            Assert.Single(moves);
            Assert.True(moves[0].IsCapture);

            Assert.Equal(
                new BoardPosition(3, 4),
                moves[0].To);
        }

        [Fact]
        public void RegularPiece_CanCaptureBackward()
        {
            var board = new Board();

            board.SetPiece(3, 2,
                new Piece(PieceColor.White));

            board.SetPiece(4, 3,
                new Piece(PieceColor.Black));

            var moves = _generator.GetLegalMoves(
                board, PieceColor.White);

            Assert.Single(moves);
            Assert.Equal(
                new BoardPosition(5, 4),
                moves[0].To);
        }

        [Fact]
        public void MultipleCaptures_AreOneMove()
        {
            var board = new Board();

            board.SetPiece(5, 0,
                new Piece(PieceColor.White));

            board.SetPiece(4, 1,
                new Piece(PieceColor.Black));

            board.SetPiece(2, 3,
                new Piece(PieceColor.Black));

            var moves = _generator.GetLegalMoves(
                board, PieceColor.White);

            Assert.Single(moves);

            Assert.Equal(2,
                moves[0].CapturedPositions.Count);

            Assert.Equal(3, moves[0].Path.Count);

            Assert.Equal(
                new BoardPosition(1, 4),
                moves[0].To);
        }

        [Fact]
        public void King_CanMoveMultipleSquares()
        {
            var board = new Board();

            board.SetPiece(3, 2,
                new Piece(PieceColor.White, true));

            var moves = _generator.GetLegalMoves(
                board, PieceColor.White);

            Assert.Contains(moves,
                m => m.To == new BoardPosition(0, 5));

            Assert.Contains(moves,
                m => m.To == new BoardPosition(6, 5));
        }

        [Fact]
        public void PromotionDuringCapture_ContinuesAsKing()
        {
            var board = new Board();

            // Белая шашка перед превращением
            board.SetPiece(2, 1,
                new Piece(PieceColor.White));

            // Первое взятие приводит на последнюю горизонталь
            board.SetPiece(1, 2,
                new Piece(PieceColor.Black));

            // Вторую шашку можно взять только уже дамкой
            board.SetPiece(2, 5,
                new Piece(PieceColor.Black));

            var moves = _generator.GetLegalMoves(
                board, PieceColor.White);

            Assert.NotEmpty(moves);

            // Дамка может приземлиться на разные клетки
            Assert.Contains(moves,
                m => m.To == new BoardPosition(3, 6));

            Assert.Contains(moves,
                m => m.To == new BoardPosition(4, 7));

            // Все допустимые маршруты должны
            // включать оба взятия
            Assert.All(moves, m =>
                Assert.Equal(2, m.CapturedPositions.Count));
        }

        [Fact]
        public void KingCapture_HasMultipleLandingSquares()
        {
            var board = new Board();

            board.SetPiece(5, 0,
                new Piece(PieceColor.White, true));

            board.SetPiece(3, 2,
                new Piece(PieceColor.Black));

            var moves = _generator.GetLegalMoves(
                board, PieceColor.White);

            Assert.Equal(3, moves.Count);

            Assert.Contains(moves,
                m => m.To == new BoardPosition(2, 3));

            Assert.Contains(moves,
                m => m.To == new BoardPosition(1, 4));

            Assert.Contains(moves,
                m => m.To == new BoardPosition(0, 5));

            Assert.All(moves,
                m => Assert.True(m.IsCapture));
        }

        [Fact]
        public void KingCannotCaptureSamePieceTwice()
        {
            var board = new Board();

            board.SetPiece(5, 0,
                new Piece(PieceColor.White, true));

            board.SetPiece(4, 1,
                new Piece(PieceColor.Black));

            board.SetPiece(2, 3,
                new Piece(PieceColor.Black));

            var moves = _generator.GetLegalMoves(
                board, PieceColor.White);

            Assert.NotEmpty(moves);

            Assert.Contains(moves,
                m => m.CapturedPositions.Count == 2);

            // В маршруте нельзя дважды взять
            // одну и ту же шашку
            Assert.All(moves, m =>
                Assert.Equal(
                    m.CapturedPositions.Count,
                    m.CapturedPositions.Distinct().Count()));
        }

    }
}
