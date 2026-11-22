
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using RussianCheckers.AI.Enums;
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Models;
using RussianCheckers.Core.Services;
using RussianCheckers.Data.Database;
using RussianCheckers.Data.Models;
using RussianCheckers.Data.Serialization;

namespace RussianCheckers.Data.Repositories
{
    public class GameRepository
    {
        private readonly GameEngine _engine = new();

        // Подключение теперь создаёт SqliteDatabase.
        public GameRepository()
        {
        }

        public async Task<int> CreateGameAsync(
            int userId,
            PieceColor playerColor,
            BotDifficulty difficulty,
            GameState state)
        {
            const string sql = @"
                INSERT INTO Games
                (UserId, PlayerColor, Difficulty, Status,
                 CurrentTurn, BoardState, AdditionalState)
                VALUES
                (@userId, @color, @difficulty, 'InProgress',
                 @turn, @board, '{}');

                SELECT last_insert_rowid();";

            string json = GameStateSerializer.Serialize(state);

            await using var connection =
                await SqliteDatabase.OpenConnectionAsync();

            await using var command =
                new SqliteCommand(sql, connection);

            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue(
                "@color", playerColor.ToString());
            command.Parameters.AddWithValue(
                "@difficulty", difficulty.ToString());
            command.Parameters.AddWithValue(
                "@turn", state.CurrentTurn.ToString());
            command.Parameters.AddWithValue("@board", json);

            object? result = await command.ExecuteScalarAsync();

            return Convert.ToInt32(result);
        }

        public async Task SaveMoveAsync(
            int gameId,
            int userId,
            Move move,
            PieceColor movingColor,
            GameState newState)
        {
            await using var connection =
                await SqliteDatabase.OpenConnectionAsync();

            using var transaction = connection.BeginTransaction();

            // Получаем сохранённую позицию.
            const string selectSql = @"
                SELECT BoardState, Status, CurrentTurn
                FROM Games
                WHERE Id = @gameId AND UserId = @userId;";

            string savedJson;
            string savedStatus;
            string savedTurn;

            await using (var command = new SqliteCommand(
                selectSql, connection, transaction))
            {
                command.Parameters.AddWithValue(
                    "@gameId", gameId);
                command.Parameters.AddWithValue(
                    "@userId", userId);

                await using var reader =
                    await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    throw new InvalidOperationException(
                        "Партия не найдена.");
                }

                savedJson = reader.GetString(0);
                savedStatus = reader.GetString(1);
                savedTurn = reader.GetString(2);
            }

            if (savedStatus != "InProgress")
            {
                throw new InvalidOperationException(
                    "Партия уже завершена.");
            }

            if (savedTurn != movingColor.ToString())
            {
                throw new InvalidOperationException(
                    "Нарушена очередь ходов.");
            }

            // Повторно проверяем ход по сохранённой позиции.
            GameState expectedState =
                GameStateSerializer.Deserialize(savedJson);

            if (!_engine.MakeMove(expectedState, move))
            {
                throw new InvalidOperationException(
                    "Недопустимый ход.");
            }

            string expectedJson =
                GameStateSerializer.Serialize(expectedState);

            string newJson =
                GameStateSerializer.Serialize(newState);

            if (expectedJson != newJson)
            {
                throw new InvalidOperationException(
                    "Состояние игры не совпадает с сохранённым.");
            }

            string moveJson = JsonSerializer.Serialize(new
            {
                move.Path,
                move.CapturedPositions
            });

            const string insertSql = @"
                INSERT INTO Moves
                (GameId, MoveNumber, PlayerColor,
                 FromRow, FromColumn, ToRow, ToColumn, MoveData)
                VALUES
                (@gameId, @number, @color,
                 @fromRow, @fromCol, @toRow, @toCol, @data);";

            await using (var command = new SqliteCommand(
                insertSql, connection, transaction))
            {
                command.Parameters.AddWithValue(
                    "@gameId", gameId);
                command.Parameters.AddWithValue(
                    "@number", expectedState.MoveCount);
                command.Parameters.AddWithValue(
                    "@color", movingColor.ToString());
                command.Parameters.AddWithValue(
                    "@fromRow", move.From.Row);
                command.Parameters.AddWithValue(
                    "@fromCol", move.From.Column);
                command.Parameters.AddWithValue(
                    "@toRow", move.To.Row);
                command.Parameters.AddWithValue(
                    "@toCol", move.To.Column);
                command.Parameters.AddWithValue(
                    "@data", moveJson);

                await command.ExecuteNonQueryAsync();
            }

            PieceColor? winner =
                _engine.GetWinner(expectedState);

            // Обновляем положение шашек и очередь хода.
            const string updateSql = @"
                UPDATE Games
                SET BoardState = @board,
                    CurrentTurn = @turn,
                    UpdatedAt = CURRENT_TIMESTAMP
                WHERE Id = @gameId AND UserId = @userId;";

            await using (var command = new SqliteCommand(
                updateSql, connection, transaction))
            {
                command.Parameters.AddWithValue(
                    "@board", newJson);
                command.Parameters.AddWithValue(
                    "@turn", expectedState.CurrentTurn.ToString());
                command.Parameters.AddWithValue(
                    "@gameId", gameId);
                command.Parameters.AddWithValue(
                    "@userId", userId);

                await command.ExecuteNonQueryAsync();
            }

            // Если партия закончилась, сохраняем результат.
            if (winner != null)
            {
                const string finishSql = @"
                    UPDATE Games
                    SET Status = CASE
                            WHEN PlayerColor = @winner THEN 'Won'
                            ELSE 'Lost'
                        END,
                        Result = CASE
                            WHEN PlayerColor = @winner THEN 'Won'
                            ELSE 'Lost'
                        END,
                        FinishedAt = CURRENT_TIMESTAMP,
                        UpdatedAt = CURRENT_TIMESTAMP
                    WHERE Id = @gameId AND UserId = @userId;";

                await using var command = new SqliteCommand(
                    finishSql, connection, transaction);

                command.Parameters.AddWithValue(
                    "@winner", winner.ToString());
                command.Parameters.AddWithValue(
                    "@gameId", gameId);
                command.Parameters.AddWithValue(
                    "@userId", userId);

                await command.ExecuteNonQueryAsync();
            }

            // Ход, состояние доски и результат сохраняются вместе.
            transaction.Commit();
        }

        public async Task<GameRecord?> GetActiveGameAsync(
            int userId)
        {
            const string sql = @"
                SELECT Id, UserId, PlayerColor, Difficulty,
                       Status, Result, BoardState,
                       StartedAt, UpdatedAt, FinishedAt
                FROM Games
                WHERE UserId = @userId
                  AND Status = 'InProgress'
                ORDER BY Id DESC
                LIMIT 1;";

            await using var connection =
                await SqliteDatabase.OpenConnectionAsync();

            await using var command =
                new SqliteCommand(sql, connection);

            command.Parameters.AddWithValue("@userId", userId);

            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new GameRecord
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),

                PlayerColor = Enum.Parse<PieceColor>(
                    reader.GetString(2)),

                Difficulty = Enum.Parse<BotDifficulty>(
                    reader.GetString(3)),

                Status = reader.GetString(4),

                Result = reader.IsDBNull(5)
                    ? null
                    : reader.GetString(5),

                State = GameStateSerializer.Deserialize(
                    reader.GetString(6)),

                StartedAt = reader.GetDateTime(7),
                UpdatedAt = reader.GetDateTime(8),

                FinishedAt = reader.IsDBNull(9)
                    ? null
                    : reader.GetDateTime(9)
            };
        }

        public async Task<List<GameHistoryItem>>
            GetGameHistoryAsync(int userId)
        {
            const string sql = @"
                SELECT
                    g.Id,
                    g.StartedAt,
                    g.FinishedAt,
                    g.PlayerColor,
                    g.Difficulty,
                    g.Status,
                    COUNT(m.Id) AS MoveCount
                FROM Games g
                LEFT JOIN Moves m ON m.GameId = g.Id
                WHERE g.UserId = @userId
                GROUP BY
                    g.Id, g.StartedAt, g.FinishedAt,
                    g.PlayerColor, g.Difficulty, g.Status
                ORDER BY g.StartedAt DESC, g.Id DESC;";

            var history = new List<GameHistoryItem>();

            await using var connection =
                await SqliteDatabase.OpenConnectionAsync();

            await using var command =
                new SqliteCommand(sql, connection);

            command.Parameters.AddWithValue("@userId", userId);

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                history.Add(new GameHistoryItem
                {
                    Id = reader.GetInt32(0),
                    StartedAt = reader.GetDateTime(1),

                    FinishedAt = reader.IsDBNull(2)
                        ? null
                        : reader.GetDateTime(2),

                    PlayerColor = reader.GetString(3),
                    Difficulty = reader.GetString(4),
                    Status = reader.GetString(5),
                    MoveCount = reader.GetInt32(6)
                });
            }

            return history;
        }

        public async Task<List<string>> GetMoveHistoryAsync(
            int gameId,
            int userId)
        {
            const string sql = @"
                SELECT m.MoveNumber, m.PlayerColor,
                       m.FromRow, m.FromColumn,
                       m.ToRow, m.ToColumn, m.MoveData
                FROM Moves m
                INNER JOIN Games g ON g.Id = m.GameId
                WHERE m.GameId = @gameId
                  AND g.UserId = @userId
                ORDER BY m.MoveNumber;";

            var history = new List<string>();

            await using var connection =
                await SqliteDatabase.OpenConnectionAsync();

            await using var command =
                new SqliteCommand(sql, connection);

            command.Parameters.AddWithValue("@gameId", gameId);
            command.Parameters.AddWithValue("@userId", userId);

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int number = reader.GetInt32(0);

                string color = reader.GetString(1) == "White"
                    ? "Белые"
                    : "Чёрные";

                int fromRow = reader.GetInt32(2);
                int fromCol = reader.GetInt32(3);
                int toRow = reader.GetInt32(4);
                int toCol = reader.GetInt32(5);

                using var json =
                    JsonDocument.Parse(reader.GetString(6));

                bool isCapture = json.RootElement
                    .GetProperty("CapturedPositions")
                    .GetArrayLength() > 0;

                string separator =
                    isCapture ? " × " : " → ";

                history.Add(
                    $"{number}. {color}: " +
                    $"{fromRow},{fromCol}" +
                    separator +
                    $"{toRow},{toCol}");
            }

            return history;
        }

        public async Task FinishGameAsync(
            int gameId,
            int userId,
            string result)
        {
            if (result != "Won" &&
                result != "Lost" &&
                result != "Draw" &&
                result != "Abandoned")
            {
                throw new ArgumentException(
                    "Недопустимый результат партии.",
                    nameof(result));
            }

            const string sql = @"
                UPDATE Games
                SET Status = @result,
                    Result = @result,
                    FinishedAt = CURRENT_TIMESTAMP,
                    UpdatedAt = CURRENT_TIMESTAMP
                WHERE Id = @gameId
                  AND UserId = @userId
                  AND Status = 'InProgress';";

            await using var connection =
                await SqliteDatabase.OpenConnectionAsync();

            await using var command =
                new SqliteCommand(sql, connection);

            command.Parameters.AddWithValue("@result", result);
            command.Parameters.AddWithValue("@gameId", gameId);
            command.Parameters.AddWithValue("@userId", userId);

            int affectedRows =
                await command.ExecuteNonQueryAsync();

            if (affectedRows != 1)
            {
                throw new InvalidOperationException(
                    "Не удалось завершить партию. " +
                    "Возможно, она уже завершена.");
            }
        }
    }
}
