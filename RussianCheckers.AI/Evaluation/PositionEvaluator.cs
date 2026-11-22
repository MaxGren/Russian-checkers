
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Models;

namespace RussianCheckers.AI.Evaluation
{
    public class PositionEvaluator
    {
        private const int RegularPieceValue = 100;
        private const int KingValue = 350;
        private const int AdvancementBonus = 5;
        private const int CenterBonus = 10;

        public int Evaluate(Board board, PieceColor botColor)
        {
            int score = 0;

            for (int row = 0; row < Board.Size; row++)
            {
                for (int col = 0; col < Board.Size; col++)
                {
                    Piece? piece = board.GetPiece(row, col);

                    if (piece == null)
                        continue;

                    int value = piece.IsKing
                        ? KingValue
                        : RegularPieceValue;

                    // Поощряем продвижение простой шашки
                    // к последней горизонтали
                    if (!piece.IsKing)
                    {
                        int advancement = piece.Color == PieceColor.White
                            ? 7 - row
                            : row;

                        value += advancement * AdvancementBonus;
                    }

                    // Небольшой бонус за контроль центра
                    if (row >= 2 && row <= 5 &&
                        col >= 2 && col <= 5)
                    {
                        value += CenterBonus;
                    }

                    if (piece.Color == botColor)
                        score += value;
                    else
                        score -= value;
                }
            }

            return score;
        }
    }
}
