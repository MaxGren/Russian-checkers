
using System;

namespace RussianCheckers.Data.Models
{
    public class GameHistoryItem
    {
        public int Id { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public string PlayerColor { get; set; } = "";

        public string Difficulty { get; set; } = "";

        public string Status { get; set; } = "";

        public int MoveCount { get; set; }

        public string ResultText => Status switch
        {
            "Won" => "Победа",
            "Lost" => "Поражение",
            "Draw" => "Ничья",
            "Abandoned" => "Прервана",
            "InProgress" => "Не завершена",
            _ => Status
        };

        public string ColorText => PlayerColor switch
        {
            "White" => "Белые",
            "Black" => "Чёрные",
            _ => PlayerColor
        };

        public string DifficultyText => Difficulty switch
        {
            "Easy" => "Лёгкая",
            "Medium" => "Средняя",
            "Hard" => "Сложная",
            _ => Difficulty
        };
    }
}
