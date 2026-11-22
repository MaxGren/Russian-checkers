
using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using RussianCheckers.Data.Models;
using RussianCheckers.Data.Repositories;
using RussianCheckers.Data.Security;


namespace RussianCheckers.Data.Services
{
    public class AuthService
    {
        private readonly UserRepository _userRepository;

        public AuthService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<(bool Success, string Message)> RegisterAsync(
            string login,
            string password,
            string confirmPassword)
        {
            login = login?.Trim() ?? "";

            if (login.Length < 3 || login.Length > 50)
                return (false, "Логин должен содержать от 3 до 50 символов.");

            if (password == null || password.Length < 8)
                return (false, "Пароль должен содержать минимум 8 символов.");

            if (password.Length > 128)
                return (false, "Пароль слишком длинный (максимум 128 символов).");

            if (password != confirmPassword)
                return (false, "Пароли не совпадают.");

            if (await _userRepository.UserExistsAsync(login))
                return (false, "Этот логин уже занят.");

            string passwordHash = PasswordHasher.HashPassword(password);

            try
            {
                await _userRepository.CreateUserAsync(login, passwordHash);
                return (true, "Регистрация прошла успешно!");
            }
            catch (SqliteException ex)
                when (ex.SqliteErrorCode == 19 &&
                      ex.SqliteExtendedErrorCode == 2067)
            {
                return (false, "Этот логин уже занят.");
            }
        }

        public async Task<(User? User, string Message)> LoginAsync(
            string login,
            string password)
        {
            login = login?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrEmpty(password))
            {
                return (null, "Введите логин и пароль.");
            }

            User? user = await _userRepository.GetByLoginAsync(login);

            if (user == null ||
                !PasswordHasher.VerifyPassword(
                    password, user.PasswordHash))
            {
                return (null, "Неверный логин или пароль.");
            }

            return (user, "Авторизация прошла успешно!");
        }
    }
}
