
using System;
using System.Linq;
using System.Threading;
using RussianCheckers.AI.Evaluation;
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Models;
using RussianCheckers.Core.Services;

namespace RussianCheckers.AI.Algorithms
{
    public class Minimax
    {
        private const int WinScore = 1_000_000;

        private readonly GameEngine _engine = new();
        private readonly PositionEvaluator _evaluator = new();

        public Move? FindBestMove(
            GameState state,
            PieceColor botColor,
            int depth,
            CancellationToken cancellationToken = default)
        {
            if (depth < 1)
                throw new ArgumentOutOfRangeException(nameof(depth));

            if (state.CurrentTurn != botColor)
                throw new ArgumentException(
                    "Сейчас не очередь компьютера.");

            cancellationToken.ThrowIfCancellationRequested();

            var legalMoves = _engine.GetLegalMoves(state);

            if (legalMoves.Count == 0)
                return null;

            Move? bestMove = null;
            int bestScore = int.MinValue;
            int alpha = int.MinValue;
            int beta = int.MaxValue;

            foreach (Move move in legalMoves)
            {
                cancellationToken.ThrowIfCancellationRequested();

                GameState nextState = state.Clone();

                if (!_engine.MakeMove(nextState, move))
                    continue;

                int score = Search(
                    nextState,
                    depth - 1,
                    alpha,
                    beta,
                    botColor,
                    cancellationToken);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }

                alpha = Math.Max(alpha, bestScore);
            }

            return bestMove;
        }

        private int Search(
            GameState state,
            int depth,
            int alpha,
            int beta,
            PieceColor botColor,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var legalMoves = _engine.GetLegalMoves(state);

            // Если ходов нет, текущая сторона проиграла.
            if (legalMoves.Count == 0)
            {
                return state.CurrentTurn == botColor
                    ? -WinScore - depth
                    : WinScore + depth;
            }

            if (depth == 0)
                return _evaluator.Evaluate(state.Board, botColor);

            bool maximizing = state.CurrentTurn == botColor;

            int bestScore = maximizing
                ? int.MinValue
                : int.MaxValue;

            foreach (Move move in legalMoves)
            {
                cancellationToken.ThrowIfCancellationRequested();

                GameState nextState = state.Clone();

                if (!_engine.MakeMove(nextState, move))
                    continue;

                int score = Search(
                    nextState,
                    depth - 1,
                    alpha,
                    beta,
                    botColor,
                    cancellationToken);

                if (maximizing)
                {
                    bestScore = Math.Max(bestScore, score);
                    alpha = Math.Max(alpha, bestScore);
                }
                else
                {
                    bestScore = Math.Min(bestScore, score);
                    beta = Math.Min(beta, bestScore);
                }

                // Альфа-бета-отсечение
                if (beta <= alpha)
                    break;
            }

            return bestScore;
        }
    }
}
