
using System;
using System.Windows;
using Microsoft.Data.Sqlite;
using RussianCheckers.Data.Repositories;
using RussianCheckers.Data.Services;

namespace checkers.Views
{
    public partial class RegisterWindow : Window
    {
        private readonly AuthService _authService;

        public RegisterWindow()
        {
            InitializeComponent();

            var repository = new UserRepository();
            _authService = new AuthService(repository);
        }

        private async void RegisterButton_Click(
            object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text;
            string password = PasswordInput.Password;
            string confirmPassword = ConfirmPasswordInput.Password;

            RegisterButton.IsEnabled = false;

            try
            {
                var result = await _authService.RegisterAsync(
                    login, password, confirmPassword);

                MessageBox.Show(
                    result.Message,
                    result.Success ? "Успех" : "Ошибка",
                    MessageBoxButton.OK,
                    result.Success
                        ? MessageBoxImage.Information
                        : MessageBoxImage.Warning);

                if (result.Success)
                {
                    DialogResult = true;
                    Close();
                }
            }
            catch (SqliteException ex)
            {
                MessageBox.Show(
                    "Ошибка регистрации в SQLite:\n" + ex.Message,
                    "Ошибка базы данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                RegisterButton.IsEnabled = true;
            }
        }

        private void BackButton_Click(
            object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
