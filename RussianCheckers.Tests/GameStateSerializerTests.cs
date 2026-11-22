
using System;
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Models;
using RussianCheckers.Data.Serialization;
using Xunit;
using System.Text.Json;

namespace RussianCheckers.Tests
{
    public class GameStateSerializerTests
    {
        [Fact]
        public void InitialState_CanBeRestored()
        {
            var state = new GameState();

            string json = GameStateSerializer.Serialize(state);

            GameState restored =
                GameStateSerializer.Deserialize(json);

            Assert.Equal(
                PieceColor.White,
                restored.CurrentTurn);

            Assert.Equal(0, restored.MoveCount);

            int pieceCount = 0;

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (restored.Board.GetPiece(row, col) != null)
                        pieceCount++;
                }
            }

            Assert.Equal(24, pieceCount);
        }

        [Fact]
        public void King_IsRestoredCorrectly()
        {
            var state = new GameState
            {
                Board = new Board()
            };

            state.Board.SetPiece(
                3, 2,
                new Piece(PieceColor.White, true));

            string json = GameStateSerializer.Serialize(state);

            GameState restored =
                GameStateSerializer.Deserialize(json);

            Piece? piece = restored.Board.GetPiece(3, 2);

            Assert.NotNull(piece);
            Assert.Equal(PieceColor.White, piece.Color);
            Assert.True(piece.IsKing);
        }

        [Fact]
        public void TurnAndCounters_AreRestored()
        {
            var state = new GameState
            {
                CurrentTurn = PieceColor.Black,
                MoveCount = 25,
                ConsecutiveKingMoves = 8
            };

            string json = GameStateSerializer.Serialize(state);

            GameState restored =
                GameStateSerializer.Deserialize(json);

            Assert.Equal(
                PieceColor.Black,
                restored.CurrentTurn);

            Assert.Equal(25, restored.MoveCount);
            Assert.Equal(8, restored.ConsecutiveKingMoves);
        }

        [Fact]
        public void EmptyBoard_CanBeRestored()
        {
            var state = new GameState
            {
                Board = new Board()
            };

            string json = GameStateSerializer.Serialize(state);

            GameState restored =
                GameStateSerializer.Deserialize(json);

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Assert.Null(
                        restored.Board.GetPiece(row, col));
                }
            }
        }

        [Fact]
        public void InvalidJson_ThrowsException()
        {
            Assert.Throws<JsonException>(() =>
                GameStateSerializer.Deserialize("{invalid json}"));
        }
    }
}
