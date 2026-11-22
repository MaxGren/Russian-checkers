
using System;
using System.Collections.Generic;
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Models;

namespace RussianCheckers.Core.Services
{
    public class MoveGenerator
    {
        private static readonly (int dr, int dc)[] Directions =
        {
            (-1, -1), (-1, 1),
            (1, -1), (1, 1)
        };

        public List<Move> GetLegalMoves(
            Board board, PieceColor turn)
        {
            var captures = new List<Move>();

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Piece? piece = board.GetPiece(row, col);

                    if (piece == null || piece.Color != turn)
                        continue;

                    var start = new BoardPosition(row, col);
                    FindCaptures(board, start, new Move(start),
                        captures, new HashSet<BoardPosition>());
                }
            }

            // При наличии взятия обычные ходы запрещены
            if (captures.Count > 0)
                return captures;

            var moves = new List<Move>();

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Piece? piece = board.GetPiece(row, col);

                    if (piece == null || piece.Color != turn)
                        continue;

                    var from = new BoardPosition(row, col);

                    if (piece.IsKing)
                        AddKingMoves(board, from, moves);
                    else
                        AddRegularMoves(board, from, piece, moves);
                }
            }

            return moves;
        }

        private void AddRegularMoves(
            Board board, BoardPosition from,
            Piece piece, List<Move> moves)
        {
            int forward = piece.Color == PieceColor.White
                ? -1 : 1;

            foreach (int dc in new[] { -1, 1 })
            {
                var to = new BoardPosition(
                    from.Row + forward,
                    from.Column + dc);

                if (!to.IsValid ||
                    board.GetPiece(to.Row, to.Column) != null)
                    continue;

                var move = new Move(from);
                move.Path.Add(to);
                moves.Add(move);
            }
        }

        private void AddKingMoves(
            Board board, BoardPosition from,
            List<Move> moves)
        {
            foreach (var (dr, dc) in Directions)
            {
                int row = from.Row + dr;
                int col = from.Column + dc;

                while (IsInside(row, col) &&
                       board.GetPiece(row, col) == null)
                {
                    var move = new Move(from);
                    move.Path.Add(new BoardPosition(row, col));
                    moves.Add(move);

                    row += dr;
                    col += dc;
                }
            }
        }

        private void FindCaptures(
            Board board,
            BoardPosition position,
            Move currentMove,
            List<Move> result,
            HashSet<BoardPosition> captured)
        {
            Piece? piece = board.GetPiece(
                position.Row, position.Column);

            if (piece == null)
                return;

            bool foundContinuation = false;

            foreach (var (dr, dc) in Directions)
            {
                if (piece.IsKing)
                {
                    int row = position.Row + dr;
                    int col = position.Column + dc;

                    // Ищем первую встреченную фигуру
                    while (IsInside(row, col) &&
                           board.GetPiece(row, col) == null)
                    {
                        row += dr;
                        col += dc;
                    }

                    if (!IsInside(row, col))
                        continue;

                    var enemyPos = new BoardPosition(row, col);
                    Piece? enemy = board.GetPiece(row, col);

                    if (enemy == null ||
                        enemy.Color == piece.Color ||
                        captured.Contains(enemyPos))
                        continue;

                    row += dr;
                    col += dc;

                    // Дамка может остановиться на любой
                    // свободной клетке за побитой фигурой
                    while (IsInside(row, col) &&
                           board.GetPiece(row, col) == null)
                    {
                        var to = new BoardPosition(row, col);

                        ExploreCapture(board, position, to,
                            enemyPos, currentMove, result,
                            captured);

                        foundContinuation = true;

                        row += dr;
                        col += dc;
                    }
                }
                else
                {
                    var enemyPos = new BoardPosition(
                        position.Row + dr,
                        position.Column + dc);

                    var to = new BoardPosition(
                        position.Row + dr * 2,
                        position.Column + dc * 2);

                    if (!enemyPos.IsValid || !to.IsValid)
                        continue;

                    Piece? enemy = board.GetPiece(
                        enemyPos.Row, enemyPos.Column);

                    if (enemy == null ||
                        enemy.Color == piece.Color ||
                        captured.Contains(enemyPos) ||
                        board.GetPiece(to.Row, to.Column) != null)
                        continue;

                    ExploreCapture(board, position, to,
                        enemyPos, currentMove, result, captured);

                    foundContinuation = true;
                }
            }

            if (!foundContinuation && currentMove.IsCapture)
                result.Add(currentMove);
        }

        private void ExploreCapture(
            Board board,
            BoardPosition from,
            BoardPosition to,
            BoardPosition enemyPos,
            Move currentMove,
            List<Move> result,
            HashSet<BoardPosition> captured)
        {
            Board nextBoard = board.Clone();

            Piece movingPiece = nextBoard.GetPiece(
                from.Row, from.Column)!;

            nextBoard.SetPiece(from.Row, from.Column, null);

            // Побитую шашку учитываем как препятствие
            // до завершения всей цепочки взятий
            // и запрещаем брать её повторно
            captured = new HashSet<BoardPosition>(captured)
            {
                enemyPos
            };

            nextBoard.SetPiece(to.Row, to.Column, movingPiece);

            // Превращение во время взятия
            if (!movingPiece.IsKing &&
                ((movingPiece.Color == PieceColor.White &&
                  to.Row == 0) ||
                 (movingPiece.Color == PieceColor.Black &&
                  to.Row == 7)))
            {
                movingPiece.IsKing = true;
            }

            Move nextMove = currentMove.Clone();
            nextMove.Path.Add(to);
            nextMove.CapturedPositions.Add(enemyPos);

            FindCaptures(nextBoard, to, nextMove,
                result, captured);
        }

        private static bool IsInside(int row, int col)
        {
            return row >= 0 && row < 8 &&
                   col >= 0 && col < 8;
        }
    }
}
