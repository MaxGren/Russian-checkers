using System;
using System.Text;
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Models;

namespace RussianCheckers.Core.Services
{
    public static class PositionKeyGenerator
    {
        public static string Generate(GameState state)
        {
            var builder = new StringBuilder(65);

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Piece? piece =
                        state.Board.GetPiece(row, col);

                    char symbol = piece switch
                    {
                        null => '.',

                        {
                            Color: PieceColor.White,
                            IsKing: false
                        } => 'w',

                        {
                            Color: PieceColor.White,
                            IsKing: true
                        } => 'W',

                        {
                            Color: PieceColor.Black,
                            IsKing: false
                        } => 'b',

                        {
                            Color: PieceColor.Black,
                            IsKing: true
                        } => 'B',

                        _ => throw new InvalidOperationException(
                            "Неизвестный тип шашки.")
                    };

                    builder.Append(symbol);
                }
            }

            builder.Append(
                state.CurrentTurn == PieceColor.White
                    ? 'w'
                    : 'b');

            return builder.ToString();
        }
    }
}
