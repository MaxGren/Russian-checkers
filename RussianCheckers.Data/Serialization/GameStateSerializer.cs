
using System;
using System.Text.Json;
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Models;
using System.Collections.Generic;
using RussianCheckers.Core.Services;

namespace RussianCheckers.Data.Serialization
{
    public static class GameStateSerializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static string Serialize(GameState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            var data = new GameStateSaveData
            {
                CurrentTurn = state.CurrentTurn,
                MoveCount = state.MoveCount,
                ConsecutiveKingMoves = state.ConsecutiveKingMoves,
                PositionHistory = new List<string>(state.PositionHistory),
                DrawState = state.DrawState.Clone(),
            };

            for (int row = 0; row < Board.Size; row++)
            {
                for (int col = 0; col < Board.Size; col++)
                {
                    Piece? piece = state.Board.GetPiece(row, col);

                    if (piece == null)
                        continue;

                    data.Pieces.Add(new PieceSaveData
                    {
                        Row = row,
                        Column = col,
                        Color = piece.Color,
                        IsKing = piece.IsKing
                    });
                }
            }

            return JsonSerializer.Serialize(data, Options);
        }

        public static GameState Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException(
                    "Сохранённое состояние пустое.", nameof(json));

            GameStateSaveData data =
                JsonSerializer.Deserialize<GameStateSaveData>(
                    json, Options)
                ?? throw new InvalidOperationException(
                    "Не удалось восстановить состояние игры.");



            if (data.Pieces == null ||
                !Enum.IsDefined(data.CurrentTurn) ||
                data.MoveCount < 0 ||
                data.ConsecutiveKingMoves < 0 ||
                data.ConsecutiveKingMoves > data.MoveCount)
            {
                throw new InvalidOperationException(
                    "Сохранённое состояние повреждено.");
            }

            var board = new Board();

            foreach (PieceSaveData piece in data.Pieces)
            {
                if (piece == null ||
                    !Enum.IsDefined(piece.Color))
                {
                    throw new InvalidOperationException(
                        "Некорректные данные шашки.");
                }

                var position = new BoardPosition(
                    piece.Row, piece.Column);

                if (!position.IsValid ||
                    (piece.Row + piece.Column) % 2 != 1)
                {
                    throw new InvalidOperationException(
                        "Некорректные координаты шашки.");
                }

                if (board.GetPiece(piece.Row, piece.Column) != null)
                {
                    throw new InvalidOperationException(
                        "На одной клетке находятся две шашки.");
                }

                board.SetPiece(
                    piece.Row,
                    piece.Column,
                    new Piece(piece.Color, piece.IsKing));
            }


            var state = new GameState
            {
                Board = board,
                CurrentTurn = data.CurrentTurn,
                MoveCount = data.MoveCount,
                ConsecutiveKingMoves = data.ConsecutiveKingMoves,

                PositionHistory = data.PositionHistory != null
                    ? new List<string>(data.PositionHistory)
                    : new List<string>(),

                // Восстанавливаем счётчики ничьих
                DrawState = data.DrawState?.Clone() ?? new DrawState()
            };


            // Поддержка сохранений, созданных до обновления
            if (state.PositionHistory.Count == 0)
            {
                state.PositionHistory.Add(
                    PositionKeyGenerator.Generate(state));
            }

            return state;
        }
    }
}
