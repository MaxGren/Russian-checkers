
using RussianCheckers.Core.Enums;

namespace RussianCheckers.Core.Models
{
    public class Board
    {
        public const int Size = 8;

        private readonly Piece?[,] _cells = new Piece?[Size, Size];

        public Piece? GetPiece(int row, int column)
        {
            ValidatePosition(row, column);
            return _cells[row, column];
        }

        public void SetPiece(int row, int column, Piece? piece)
        {
            ValidatePosition(row, column);
            _cells[row, column] = piece;
        }

        public void Initialize()
        {
            // Очищаем доску
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    _cells[row, col] = null;
                }
            }

            // Расставляем шашки
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    bool isDark = (row + col) % 2 == 1;

                    if (!isDark)
                        continue;

                    if (row < 3)
                    {
                        _cells[row, col] =
                            new Piece(PieceColor.Black);
                    }
                    else if (row > 4)
                    {
                        _cells[row, col] =
                            new Piece(PieceColor.White);
                    }
                }
            }
        }

        public Board Clone()
        {
            var copy = new Board();

            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    copy._cells[row, col] =
                        _cells[row, col]?.Clone();
                }
            }

            return copy;
        }

        private static void ValidatePosition(int row, int column)
        {
            if (row < 0 || row >= Size ||
                column < 0 || column >= Size)
            {
                throw new ArgumentOutOfRangeException(
                    $"Недопустимая клетка: ({row}, {column})");
            }
        }
    }
}
