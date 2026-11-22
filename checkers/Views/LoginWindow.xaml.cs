
using System;
using System.Windows;
using RussianCheckers.Data.Models;
using RussianCheckers.Data.Repositories;
using RussianCheckers.Data.Services;

namespace checkers.Views
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _authService;

        public User? AuthenticatedUser { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();

            var repository = new UserRepository();
            _authService = new AuthService(repository);
        }

        private async void LoginButton_Click(
            object sender, RoutedEventArgs e)
        {
            LoginButton.IsEnabled = false;

            try
            {
                var result = await _authService.LoginAsync(
                    LoginTextBox.Text,
                    PasswordInput.Password);

                if (result.User == null)
                {
                    MessageBox.Show(
                        result.Message,
                        "Ошибка входа",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                AuthenticatedUser = result.User;

                DialogResult = true;
                Close();
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "Ошибка авторизации. Не удалось обратиться к локальной базе данных.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                LoginButton.IsEnabled = true;
            }
        }

        private void RegisterButton_Click(
            object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow
            {
                Owner = this
            };

            if (registerWindow.ShowDialog() == true)
            {
                MessageBox.Show(
                    "Регистрация завершена. Теперь войдите в аккаунт.",
                    "Информация",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }
}
