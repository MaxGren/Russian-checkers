
namespace RussianCheckers.Core.Models
{
    public record BoardPosition(int Row, int Column)
    {
        public bool IsValid =>
            Row >= 0 && Row < Board.Size &&
            Column >= 0 && Column < Board.Size;
    }
}
