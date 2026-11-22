
using System;
using RussianCheckers.AI.Enums;
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Models;

namespace RussianCheckers.Data.Models
{
    public class GameRecord
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public PieceColor PlayerColor { get; set; }
        public BotDifficulty Difficulty { get; set; }

        public string Status { get; set; } = "InProgress";
        public string? Result { get; set; }

        public GameState State { get; set; } = new();

        public DateTime StartedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
    }
}
