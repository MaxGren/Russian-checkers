using RussianCheckers.Core.Models;
using System.Collections.Generic;
using RussianCheckers.Core.Enums;

namespace RussianCheckers.Data.Serialization
{
    public class GameStateSaveData
    {
        public List<PieceSaveData> Pieces { get; set; } = new();

        public PieceColor CurrentTurn { get; set; }

        public List<string> PositionHistory { get; set; } = new();

        public DrawState DrawState { get; set; } = new();

        public int MoveCount { get; set; }

        public int ConsecutiveKingMoves { get; set; }
    }
}
