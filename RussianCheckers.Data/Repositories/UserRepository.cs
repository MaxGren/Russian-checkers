
using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using RussianCheckers.Data.Database;
using RussianCheckers.Data.Models;

namespace RussianCheckers.Data.Repositories
{
    public class UserRepository
    {
        public async Task<bool> UserExistsAsync(string login)
        {
            const string sql =
                "SELECT COUNT(*) FROM Users WHERE Login = @login";

            await using var connection =
                await SqliteDatabase.OpenConnectionAsync();

            await using var command =
                new SqliteCommand(sql, connection);

            command.Parameters.AddWithValue("@login", login);

            long count = Convert.ToInt64(
                await command.ExecuteScalarAsync());

            return count > 0;
        }

        public async Task<int> CreateUserAsync(
            string login,
            string passwordHash)
        {
            const string sql = @"
                INSERT INTO Users (Login, PasswordHash)
                VALUES (@login, @passwordHash);

                SELECT last_insert_rowid();";

            await using var connection =
                await SqliteDatabase.OpenConnectionAsync();

            await using var command =
                new SqliteCommand(sql, connection);

            command.Parameters.AddWithValue("@login", login);
            command.Parameters.AddWithValue(
                "@passwordHash", passwordHash);

            object? result = await command.ExecuteScalarAsync();

            return Convert.ToInt32(result);
        }

        public async Task<User?> GetByLoginAsync(string login)
        {
            const string sql = @"
                SELECT Id, Login, PasswordHash, CreatedAt
                FROM Users
                WHERE Login = @login
                LIMIT 1;";

            await using var connection =
                await SqliteDatabase.OpenConnectionAsync();

            await using var command =
                new SqliteCommand(sql, connection);

            command.Parameters.AddWithValue("@login", login);

            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new User
            {
                Id = reader.GetInt32(0),
                Login = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                CreatedAt = reader.GetDateTime(3)
            };
        }
    }
}
