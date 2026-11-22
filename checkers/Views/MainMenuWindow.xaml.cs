
using System;
using System.Windows;
using RussianCheckers.Data.Models;
using RussianCheckers.Data.Repositories;
using RussianCheckers.AI.Enums;
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Models;
using System.Threading.Tasks;

namespace checkers.Views
{
    public partial class MainMenuWindow : Window
    {
        private readonly User _currentUser;
        private readonly GameRepository _gameRepository;

        public bool LogoutRequested { get; private set; }

        public MainMenuWindow(User user)
        {
            InitializeComponent();

            _currentUser = user;

            WelcomeText.Text =
                $"Добро пожаловать, {_currentUser.Login}!";


            _gameRepository = new GameRepository();

            Loaded += MainMenuWindow_Loaded;
        }

        // Проверяем незавершённую партию после авторизации
        private async void MainMenuWindow_Loaded(
            object sender, RoutedEventArgs e)
        {
            Loaded -= MainMenuWindow_Loaded;

            try
            {
                GameRecord? activeGame =
                    await _gameRepository.GetActiveGameAsync(
                        _currentUser.Id);

                if (activeGame == null)
                    return;

                var dialog = new ContinueGameDialog
                {
                    Owner = this
                };

                dialog.ShowDialog();


                switch (dialog.Choice)
                {
                    case ContinueGameChoice.Continue:
                        OpenSavedGame(activeGame);
                        break;

                    case ContinueGameChoice.NewGame:
                        await StartNewGameAsync(true);
                        break;

                    case ContinueGameChoice.FinishGame:

                        MessageBoxResult confirmation = MessageBox.Show(
                            "Вы уверены, что хотите завершить " +
                            "незавершённую партию?\n\n" +
                            "Продолжить её после этого будет нельзя.",
                            "Завершение партии",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (confirmation == MessageBoxResult.Yes)
                        {
                            await _gameRepository.FinishGameAsync(
                                activeGame.Id,
                                _currentUser.Id,
                                "Abandoned");

                            MessageBox.Show(
                                "Партия завершена.\n" +
                                "История ходов сохранена.",
                                "Партия завершена",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }

                        break;

                    case ContinueGameChoice.Later:
                        // Сохранение остаётся в базе
                        break;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка проверки сохранённой партии:\n"
                    + ex.Message,
                    "Ошибка базы данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Открытие сохранённой партии
        private void OpenSavedGame(GameRecord game)
        {
            var gameWindow = new GameWindow(game)
            {
                Owner = this
            };

            gameWindow.ShowDialog();
        }

        // Создание новой партии
        private async Task StartNewGameAsync(
            bool replaceExisting = false)
        {
            GameRecord? activeGame =
                await _gameRepository.GetActiveGameAsync(
                    _currentUser.Id);

            if (activeGame != null)
            {
                if (!replaceExisting)
                {
                    MessageBox.Show(
                        "У вас есть незавершённая партия. " +
                        "Используйте кнопку «Продолжить игру».",
                        "Незавершённая партия");
                    return;
                }

                MessageBoxResult confirmation = MessageBox.Show(
                    "Текущая партия будет завершена " +
                    "со статусом «Прервана».\n\n" +
                    "Начать новую игру?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmation != MessageBoxResult.Yes)
                    return;

                // Завершаем предыдущую партию
                await _gameRepository.FinishGameAsync(
                    activeGame.Id,
                    _currentUser.Id,
                    "Abandoned");
            }

            bool playerIsWhite =
                Random.Shared.Next(2) == 0;

            PieceColor playerColor = playerIsWhite
                ? PieceColor.White
                : PieceColor.Black;

            BotDifficulty difficulty =
                DifficultyComboBox.SelectedIndex switch
                {
                    0 => BotDifficulty.Easy,
                    1 => BotDifficulty.Medium,
                    2 => BotDifficulty.Hard,
                    _ => BotDifficulty.Easy
                };

            var gameState = new GameState();

            int gameId =
                await _gameRepository.CreateGameAsync(
                    _currentUser.Id,
                    playerColor,
                    difficulty,
                    gameState);

            var gameWindow = new GameWindow(
                playerIsWhite,
                difficulty,
                gameId,
                _currentUser.Id)
            {
                Owner = this
            };

            gameWindow.ShowDialog();
        }

        private async void NewGameButton_Click(
            object sender, RoutedEventArgs e)
        {
            try
            {
                await StartNewGameAsync();
            }
            catch (Exception ex)
            {
                ShowDatabaseError(ex);
            }
        }

        private async void ContinueGameButton_Click(
            object sender, RoutedEventArgs e)
        {
            try
            {
                GameRecord? game =
                    await _gameRepository.GetActiveGameAsync(
                        _currentUser.Id);

                if (game == null)
                {
                    MessageBox.Show(
                        "У вас нет незавершённых партий.");
                    return;
                }

                OpenSavedGame(game);
            }
            catch (Exception ex)
            {
                ShowDatabaseError(ex);
            }
        }


        private void HistoryButton_Click(
            object sender, RoutedEventArgs e)
        {
            try
            {
                var historyWindow =
                    new HistoryWindow(_currentUser.Id)
                    {
                        Owner = this
                    };

                historyWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось открыть историю партий:\n"
                    + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void LogoutButton_Click(
            object sender, RoutedEventArgs e)
        {
            LogoutRequested = true;
            Close();
        }

        private void ShowDatabaseError(Exception ex)
        {
            MessageBox.Show(
                "Ошибка работы с базой данных:\n" +
                ex.Message,
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }


    }
}
