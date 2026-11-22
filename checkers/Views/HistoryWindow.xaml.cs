
using System;
using System.Windows;
using System.Windows.Controls;
using RussianCheckers.Data.Models;
using RussianCheckers.Data.Repositories;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace checkers.Views
{
    public partial class HistoryWindow : Window
    {
        private readonly int _userId;
        private readonly GameRepository _repository;

        private int _selectionVersion = 0;

        public HistoryWindow(int userId)
        {
            InitializeComponent();

            _userId = userId;


            _repository = new GameRepository();

            Loaded += HistoryWindow_Loaded;
        }


        private async void ExportProtocolButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Проверяем, выбрана ли партия
            if (GamesDataGrid.SelectedItem is not GameHistoryItem game)
            {
                MessageBox.Show(
                    "Сначала выберите партию из списка.",
                    "Экспорт протокола",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            try
            {
                // Получаем ходы выбранной партии из SQLite
                var moves = await _repository.GetMoveHistoryAsync(
                    game.Id,
                    _userId);

                // Предлагаем выбрать место сохранения файла
                var dialog = new SaveFileDialog
                {
                    Title = "Сохранение протокола партии",
                    Filter = "Текстовые файлы (*.txt)|*.txt",
                    FileName = $"Game_{game.Id}.txt",
                    DefaultExt = ".txt",
                    AddExtension = true
                };

                if (dialog.ShowDialog(this) != true)
                    return;

                // Формируем содержимое протокола
                var protocol = new StringBuilder();

                protocol.AppendLine("РУССКИЕ ШАШКИ");
                protocol.AppendLine("Протокол партии");
                protocol.AppendLine(new string('=', 40));
                protocol.AppendLine();

                protocol.AppendLine($"Номер партии: {game.Id}");
                protocol.AppendLine(
                    $"Дата начала: {game.StartedAt:dd.MM.yyyy HH:mm}");

                protocol.AppendLine(
                    $"Цвет игрока: {(game.PlayerColor == "White" ? "Белые" : "Чёрные")}");

                string difficulty = game.Difficulty switch
                {
                    "Easy" => "Лёгкая",
                    "Medium" => "Средняя",
                    "Hard" => "Сложная",
                    _ => game.Difficulty
                };

                string result = game.Status switch
                {
                    "Won" => "Победа",
                    "Lost" => "Поражение",
                    "Draw" => "Ничья",
                    "InProgress" => "Партия не завершена",
                    "Abandoned" => "Партия прервана",
                    _ => game.Status
                };

                protocol.AppendLine($"Сложность: {difficulty}");
                protocol.AppendLine($"Результат: {result}");

                if (game.FinishedAt.HasValue)
                {
                    protocol.AppendLine(
                        $"Дата завершения: {game.FinishedAt.Value:dd.MM.yyyy HH:mm}");
                }

                protocol.AppendLine();
                protocol.AppendLine("ХОДЫ ПАРТИИ:");
                protocol.AppendLine(new string('-', 40));

                if (moves.Count == 0)
                {
                    protocol.AppendLine("Ходы отсутствуют.");
                }
                else
                {
                    foreach (string move in moves)
                    {
                        protocol.AppendLine(move);
                    }
                }

                protocol.AppendLine(new string('-', 40));
                protocol.AppendLine("Конец протокола.");

                // Сохраняем в UTF-8 с BOM для Windows
                await File.WriteAllTextAsync(
                    dialog.FileName,
                    protocol.ToString(),
                    new UTF8Encoding(true));

                MessageBox.Show(
                    "Протокол партии успешно сохранён!",
                    "Экспорт завершён",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось сохранить протокол:\n" +
                    ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private async void HistoryWindow_Loaded(
            object sender, RoutedEventArgs e)
        {
            try
            {
                var history =
                    await _repository.GetGameHistoryAsync(_userId);

                GamesDataGrid.ItemsSource = history;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить историю партий:\n"
                    + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void GamesDataGrid_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (GamesDataGrid.SelectedItem
                is not GameHistoryItem game)
            {
                MovesList.ItemsSource = null;
                return;
            }

            // Запоминаем номер запроса,
            // чтобы старый ответ не заменил новый.
            int version = ++_selectionVersion;

            try
            {
                var moves =
                    await _repository.GetMoveHistoryAsync(
                        game.Id,
                        _userId);

                if (version != _selectionVersion)
                    return;

                MovesList.ItemsSource = moves;
            }
            catch (Exception ex)
            {
                if (version != _selectionVersion)
                    return;

                MessageBox.Show(
                    "Не удалось загрузить ходы:\n"
                    + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(
            object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
