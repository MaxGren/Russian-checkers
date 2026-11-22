
using RussianCheckers.Core.Enums;

namespace RussianCheckers.Data.Serialization
{
    public class PieceSaveData
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public PieceColor Color { get; set; }
        public bool IsKing { get; set; }
    }
}
