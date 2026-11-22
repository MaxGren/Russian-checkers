
using System.Collections.Generic;
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Services;

namespace RussianCheckers.Core.Models
{
    public class GameState
    {
        public Board Board { get; set; }

        public PieceColor CurrentTurn { get; set; }

        public int MoveCount { get; set; }

        public int ConsecutiveKingMoves { get; set; }

        public DrawState DrawState { get; set; } = new();

        // История позиций для проверки повторений
        public List<string> PositionHistory { get; set; } = new();

        public GameState()
        {
            Board = new Board();
            Board.Initialize();

            CurrentTurn = PieceColor.White;
            MoveCount = 0;

            // Запоминаем начальную позицию
            PositionHistory.Add(
                PositionKeyGenerator.Generate(this));
        }

        public GameState Clone()
        {
            return new GameState
            {
                Board = Board.Clone(),
                CurrentTurn = CurrentTurn,
                MoveCount = MoveCount,
                ConsecutiveKingMoves = ConsecutiveKingMoves,
                DrawState = DrawState.Clone(),

                // Копируем список, а не его ссылку
                PositionHistory = new List<string>(
                    PositionHistory)
            };
        }
    }
}
