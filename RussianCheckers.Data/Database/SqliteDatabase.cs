
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace RussianCheckers.Data.Database
{
    public static class SqliteDatabase
    {
        public static string DatabasePath
        {
            get
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "RussianCheckers");

                Directory.CreateDirectory(folder);

                return Path.Combine(folder, "checkers.db");
            }
        }

        public static string ConnectionString
        {
            get
            {
                var builder = new SqliteConnectionStringBuilder
                {
                    DataSource = DatabasePath,
                    Mode = SqliteOpenMode.ReadWriteCreate,
                    ForeignKeys = true
                };

                return builder.ToString();
            }
        }

        public static async Task<SqliteConnection> OpenConnectionAsync()
        {
            var connection = new SqliteConnection(ConnectionString);

            await connection.OpenAsync();

            return connection;
        }

        public static async Task InitializeAsync()
        {
            await using var connection = await OpenConnectionAsync();

            const string sql = @"
                CREATE TABLE IF NOT EXISTS Users
                (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Login TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL
                        DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS Games
                (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    PlayerColor TEXT NOT NULL,
                    Difficulty TEXT NOT NULL,
                    Status TEXT NOT NULL DEFAULT 'InProgress',
                    Result TEXT,
                    CurrentTurn TEXT NOT NULL,
                    BoardState TEXT NOT NULL,
                    AdditionalState TEXT,
                    StartedAt TEXT NOT NULL
                        DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TEXT NOT NULL
                        DEFAULT CURRENT_TIMESTAMP,
                    FinishedAt TEXT,

                    FOREIGN KEY (UserId)
                        REFERENCES Users(Id)
                );

                CREATE TABLE IF NOT EXISTS Moves
                (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GameId INTEGER NOT NULL,
                    MoveNumber INTEGER NOT NULL,
                    PlayerColor TEXT NOT NULL,
                    FromRow INTEGER NOT NULL,
                    FromColumn INTEGER NOT NULL,
                    ToRow INTEGER NOT NULL,
                    ToColumn INTEGER NOT NULL,
                    MoveData TEXT NOT NULL,

                    FOREIGN KEY (GameId)
                        REFERENCES Games(Id),

                    UNIQUE(GameId, MoveNumber)
                );

                CREATE INDEX IF NOT EXISTS
                    IX_Games_UserId_Status
                    ON Games(UserId, Status);

                CREATE INDEX IF NOT EXISTS
                    IX_Moves_GameId
                    ON Moves(GameId);
            ";

            await using var command =
                new SqliteCommand(sql, connection);

            await command.ExecuteNonQueryAsync();
        }
    }
}
