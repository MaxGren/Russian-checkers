
using System;
using System.Windows;
using checkers.Views;
using RussianCheckers.Data.Database;

namespace checkers
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnExplicitShutdown;


            try
            {
                // Создаём SQLite-базу и таблицы при необходимости
                SqliteDatabase.InitializeAsync()
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось создать базу данных SQLite:\n\n" +
                    ex.Message,
                    "Ошибка базы данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown();
                return;
            }


            while (true)
            {
                try
                {
                    var loginWindow = new LoginWindow();

                    bool? loginResult = loginWindow.ShowDialog();

                    if (loginResult != true ||
                        loginWindow.AuthenticatedUser == null)
                    {
                        break;
                    }

                    var menuWindow = new MainMenuWindow(
                        loginWindow.AuthenticatedUser);

                    MainWindow = menuWindow;

                    menuWindow.ShowDialog();

                    if (!menuWindow.LogoutRequested)
                    {
                        break;
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show(
                        "Произошла ошибка при запуске приложения.",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    break;
                }
            }

            Shutdown();
        }
    }
}
