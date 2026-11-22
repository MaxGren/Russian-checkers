
using System.Collections.Generic;

namespace RussianCheckers.Core.Models
{
    public class Move
    {
        public BoardPosition From { get; set; }

        public BoardPosition To => Path[^1];

        // Весь путь шашки, включая начальную клетку
        public List<BoardPosition> Path { get; set; } = new();

        // Координаты побитых фигур
        public List<BoardPosition> CapturedPositions { get; set; } = new();

        public bool IsCapture => CapturedPositions.Count > 0;

        public Move(BoardPosition from)
        {
            From = from;
            Path.Add(from);
        }

        public Move Clone()
        {
            return new Move(From)
            {
                Path = new List<BoardPosition>(Path),
                CapturedPositions =
                    new List<BoardPosition>(CapturedPositions)
            };
        }
    }
}
