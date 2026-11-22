
using System;
using System.Collections.Generic;
using System.Linq;
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Models;

namespace RussianCheckers.Core.Services
{
    public class GameEngine
    {
        private readonly MoveGenerator _moveGenerator = new();

        public List<Move> GetLegalMoves(GameState state)
        {
            return _moveGenerator.GetLegalMoves(
                state.Board,
                state.CurrentTurn);
        }

        public bool MakeMove(GameState state, Move move)
        {
            // Получаем все допустимые полные ходы
            List<Move> legalMoves = GetLegalMoves(state);

            // Ищем совпадение по всему маршруту и взятиям
            Move? legalMove = legalMoves.FirstOrDefault(
                m => m.From == move.From &&
                     m.Path.SequenceEqual(move.Path) &&
                     m.CapturedPositions.SequenceEqual(
                         move.CapturedPositions));

            if (legalMove == null)
                return false;

            Board board = state.Board;

            Piece? piece = board.GetPiece(
                legalMove.From.Row,
                legalMove.From.Column);

            if (piece == null)
                return false;

            // Сохраняем данные до выполнения хода
            string materialBefore =
                GetMaterialSignature(board);

            bool movedKing = piece.IsKing;
            bool wasCapture = legalMove.IsCapture;
            PieceColor movingColor = state.CurrentTurn;

            // Для старых сохранений без истории позиций
            if (state.PositionHistory == null ||
                state.PositionHistory.Count == 0)
            {
                state.PositionHistory = new List<string>
                {
                    PositionKeyGenerator.Generate(state)
                };
            }

            // Для старых сохранений без DrawState
            state.DrawState ??= new DrawState();

            // Существующий упрощённый счётчик дамочных ходов
            if (piece.IsKing && !legalMove.IsCapture)
            {
                state.ConsecutiveKingMoves++;
            }
            else
            {
                state.ConsecutiveKingMoves = 0;
            }

            // Убираем шашку с начальной клетки
            board.SetPiece(
                legalMove.From.Row,
                legalMove.From.Column,
                null);

            // Удаляем все побитые шашки
            foreach (BoardPosition captured
                     in legalMove.CapturedPositions)
            {
                board.SetPiece(
                    captured.Row,
                    captured.Column,
                    null);
            }

            // Превращение в дамку.
            // Проверяем весь маршрут, поскольку превращение
            // могло произойти во время цепочки взятий.
            if (!piece.IsKing)
            {
                int promotionRow =
                    piece.Color == PieceColor.White ? 0 : 7;

                if (legalMove.Path.Any(
                    p => p.Row == promotionRow))
                {
                    piece.IsKing = true;
                }
            }

            // Размещаем шашку на конечной клетке
            board.SetPiece(
                legalMove.To.Row,
                legalMove.To.Column,
                piece);

            // Увеличиваем количество выполненных ходов
            state.MoveCount++;

            // Передаём очередь другой стороне
            state.CurrentTurn =
                state.CurrentTurn == PieceColor.White
                    ? PieceColor.Black
                    : PieceColor.White;

            // Обновляем счётчики ничейных окончаний
            UpdateDrawCounters(
                state,
                movingColor,
                movedKing,
                wasCapture,
                materialBefore);

            // Запоминаем позицию после завершённого хода
            state.PositionHistory.Add(
                PositionKeyGenerator.Generate(state));

            return true;
        }

        public PieceColor? GetWinner(GameState state)
        {
            // Если у стороны нет допустимых ходов,
            // она проиграла.
            List<Move> moves = GetLegalMoves(state);

            if (moves.Count > 0)
                return null;

            return state.CurrentTurn == PieceColor.White
                ? PieceColor.Black
                : PieceColor.White;
        }

        public bool IsThreefoldRepetition(GameState state)
        {
            string currentPosition =
                PositionKeyGenerator.Generate(state);

            int repetitions = state.PositionHistory.Count(
                position => position == currentPosition);

            return repetitions >= 3;
        }

        private static string GetMaterialSignature(Board board)
        {
            int whiteMen = 0;
            int whiteKings = 0;
            int blackMen = 0;
            int blackKings = 0;

            for (int row = 0; row < Board.Size; row++)
            {
                for (int col = 0; col < Board.Size; col++)
                {
                    Piece? piece = board.GetPiece(row, col);

                    if (piece == null)
                        continue;

                    if (piece.Color == PieceColor.White)
                    {
                        if (piece.IsKing)
                            whiteKings++;
                        else
                            whiteMen++;
                    }
                    else
                    {
                        if (piece.IsKing)
                            blackKings++;
                        else
                            blackMen++;
                    }
                }
            }

            return $"{whiteMen}:{whiteKings}:{blackMen}:{blackKings}";
        }


        private static void UpdateDrawCounters(
            GameState state,
            PieceColor movingColor,
            bool movedKing,
            bool wasCapture,
            string materialBefore)
        {
            DrawState draw = state.DrawState;

            string materialAfter =
                GetMaterialSignature(state.Board);

            bool materialChanged =
                materialBefore != materialAfter;

            // Счётчик ходов без взятий
            if (wasCapture)
                draw.NoCapturePlies = 0;
            else
                draw.NoCapturePlies++;

            // Счётчик последовательных ходов дамками
            if (wasCapture || !movedKing)
                draw.KingOnlyPlies = 0;
            else
                draw.KingOnlyPlies++;

            // Отслеживаем изменение состава фигур
            if (materialChanged)
            {
                draw.WhiteMovesSinceMaterialChange = 0;
                draw.BlackMovesSinceMaterialChange = 0;
            }
            else
            {
                if (movingColor == PieceColor.White)
                    draw.WhiteMovesSinceMaterialChange++;
                else
                    draw.BlackMovesSinceMaterialChange++;
            }

            draw.MaterialSignature = materialAfter;

            // Определяем применимые правила окончания
            var checker = new DrawRuleChecker();

            int? materialLimit =
                checker.GetMaterialBalanceMoveLimit(state);

            int? kingOnlyLimit =
                checker.GetKingOnlyMoveLimit(state);

            // Если возникло новое окончание после изменения
            // состава фигур, начинаем отсчёт с нуля
            if (materialChanged ||
                (draw.MaterialEndingLimit != null &&
                 draw.MaterialEndingLimit != materialLimit))
            {
                draw.WhiteMovesSinceMaterialChange = 0;
                draw.BlackMovesSinceMaterialChange = 0;
            }

            // При изменении категории дамочного окончания
            // сбрасываем соответствующий счётчик
            if (materialChanged ||
                draw.KingOnlyEndingLimit != kingOnlyLimit)
            {
                draw.KingOnlyPlies = 0;
            }

            draw.MaterialEndingLimit = materialLimit;
            draw.KingOnlyEndingLimit = kingOnlyLimit;
        }

    }
}
