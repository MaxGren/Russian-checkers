
using RussianCheckers.Core.Enums;

namespace RussianCheckers.Core.Models
{
    public class Piece
    {
        public PieceColor Color { get; set; }

        public bool IsKing { get; set; }

        public Piece(PieceColor color, bool isKing = false)
        {
            Color = color;
            IsKing = isKing;
        }

        public Piece Clone()
        {
            return new Piece(Color, IsKing);
        }
    }
}
