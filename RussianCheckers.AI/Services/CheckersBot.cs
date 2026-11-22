
using System;
using System.Threading;
using System.Threading.Tasks;
using RussianCheckers.AI.Algorithms;
using RussianCheckers.AI.Enums;
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Models;

namespace RussianCheckers.AI.Services
{
    public class CheckersBot
    {
        private readonly Minimax _minimax = new();

        public BotDifficulty Difficulty { get; }

        public PieceColor Color { get; }

        public CheckersBot(
            PieceColor color,
            BotDifficulty difficulty)
        {
            Color = color;
            Difficulty = difficulty;
        }


        public int GetSearchDepth()
        {
            return Difficulty switch
            {
                BotDifficulty.Easy => 1,
                BotDifficulty.Medium => 2,
                BotDifficulty.Hard => 4,
                _ => 1
            };
        }


        public Task<Move?> FindMoveAsync(
            GameState state,
            CancellationToken cancellationToken = default)
        {
            // Создаём копию состояния,
            // чтобы поиск не изменял реальную партию.
            GameState stateCopy = state.Clone();

            int depth = GetSearchDepth();

            // Minimax выполняет вычисления в отдельном
            // потоке, чтобы не блокировать интерфейс WPF.
            return Task.Run(
                () => _minimax.FindBestMove(
                    stateCopy,
                    Color,
                    depth,
                    cancellationToken),
                cancellationToken);
        }
    }
}
