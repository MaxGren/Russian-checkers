using System;
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Models;

namespace RussianCheckers.Core.Services
{
    public class DrawRuleChecker
    {
        public PieceStatistics GetPieceStatistics(
            GameState state)
        {
            var statistics = new PieceStatistics();

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Piece? piece =
                        state.Board.GetPiece(row, col);

                    if (piece == null)
                        continue;

                    if (piece.Color == PieceColor.White)
                    {
                        if (piece.IsKing)
                            statistics.WhiteKings++;
                        else
                            statistics.WhiteMen++;
                    }
                    else
                    {
                        if (piece.IsKing)
                            statistics.BlackKings++;
                        else
                            statistics.BlackMen++;
                    }
                }
            }

            return statistics;
        }

        // Определяем, есть ли дамки у обеих сторон
        public bool BothPlayersHaveKings(GameState state)
        {
            var stats = GetPieceStatistics(state);

            return stats.WhiteKings > 0 &&
                   stats.BlackKings > 0;
        }

        // Проверяем количество фигур на доске
        public bool HasPieceCount(
            GameState state,
            int minimum,
            int maximum)
        {
            var stats = GetPieceStatistics(state);

            return stats.TotalPieces >= minimum &&
                   stats.TotalPieces <= maximum;
        }

        // Проверяем соотношение: 3 дамки против 1
        public bool IsThreeKingsAgainstOne(GameState state)
        {
            var stats = GetPieceStatistics(state);

            return (stats.WhiteKings >= 3 &&
                    stats.BlackKings == 1 &&
                    stats.BlackMen == 0)
                ||
                   (stats.BlackKings >= 3 &&
                    stats.WhiteKings == 1 &&
                    stats.WhiteMen == 0);
        }


        public int? GetMaterialBalanceMoveLimit(GameState state)
        {
            var stats = GetPieceStatistics(state);

            // Правило действует, когда у обеих сторон
            // есть хотя бы одна дамка
            if (!BothPlayersHaveKings(state))
                return null;

            return stats.TotalPieces switch
            {
                2 or 3 => 5,
                4 or 5 => 30,
                6 or 7 => 60,
                _ => null
            };
        }

        // Ограничение для последовательных ходов
        // только дамками без взятий и ходов простых шашек
        public int? GetKingOnlyMoveLimit(GameState state)
        {
            var stats = GetPieceStatistics(state);

            return stats.TotalPieces switch
            {
                4 or 5 => 15,
                6 or 7 => 25,
                _ => null
            };
        }

        // Проверяем нахождение дамки на большой дороге
        // Диагональ a1–h8: Row + Column == 7
        public bool HasKingOnBigRoad(
            GameState state,
            PieceColor color)
        {
            for (int row = 0; row < Board.Size; row++)
            {
                int col = Board.Size - 1 - row;

                Piece? piece = state.Board.GetPiece(row, col);

                if (piece != null &&
                    piece.Color == color &&
                    piece.IsKing)
                {
                    return true;
                }
            }

            return false;
        }


        public string? GetReachedDrawReason(GameState state)
        {
            DrawState draw = state.DrawState;

            // Проверяем окончания с 4–7 фигурами,
            // в которых у обеих сторон есть дамки.
            int? materialLimit =
                GetMaterialBalanceMoveLimit(state);

            if (materialLimit is 30 or 60 &&
                draw.MaterialEndingLimit == materialLimit &&
                draw.WhiteMovesSinceMaterialChange >= materialLimit &&
                draw.BlackMovesSinceMaterialChange >= materialLimit)
            {
                return $"Окончание без изменения соотношения сил " +
                       $"в течение {materialLimit} ходов каждой стороны.";
            }

            // Проверяем последовательные ходы только дамками.
            int? kingLimit = GetKingOnlyMoveLimit(state);

            if (kingLimit is 15 or 25 &&
                draw.KingOnlyEndingLimit == kingLimit &&
                draw.KingOnlyPlies >= kingLimit * 2)
            {
                return $"В течение {kingLimit} полных ходов " +
                       "стороны двигали только дамками без взятий.";
            }

            return null;
        }


    }

    public class PieceStatistics
    {
        public int WhiteMen { get; set; }
        public int WhiteKings { get; set; }

        public int BlackMen { get; set; }
        public int BlackKings { get; set; }

        public int TotalWhite =>
            WhiteMen + WhiteKings;

        public int TotalBlack =>
            BlackMen + BlackKings;

        public int TotalPieces =>
            TotalWhite + TotalBlack;
    }
}
