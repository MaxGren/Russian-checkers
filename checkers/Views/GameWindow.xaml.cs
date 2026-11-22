
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using RussianCheckers.AI.Enums;
using RussianCheckers.AI.Services;
using RussianCheckers.Core.Enums;
using RussianCheckers.Core.Models;
using RussianCheckers.Core.Services;
using System.Windows.Media.Animation;
using RussianCheckers.Data.Models;
using RussianCheckers.Data.Repositories;

namespace checkers.Views
{
    public partial class GameWindow : Window
    {
        private GameState _gameState;

        private readonly GameRepository _gameRepository;

        private bool _isSavingMove = false;
        private readonly GameEngine _gameEngine;
        private readonly CheckersBot _bot;

        private readonly int _gameId;
        private readonly int _userId;

        private readonly PieceColor _playerColor;
        private readonly PieceColor _botColor;

        private readonly CancellationTokenSource _cts = new();

        private BoardPosition? _selectedPosition;

        private List<Move> _legalMoves = new();
        private List<Move> _selectedMoves = new();

        // Индекс следующей клетки в маршруте взятия
        private int _pathStepIndex = 1;

        private bool _isBotThinking = false;
        private bool _gameOver = false;
        private bool _isClosing = false;




        public GameWindow(
            bool playerIsWhite,
            BotDifficulty difficulty,
            int gameId,
            int userId,
            GameState? restoredState = null)
        {
            InitializeComponent();

            _gameId = gameId;
            _userId = userId;


            _gameRepository = new GameRepository();

            _playerColor = playerIsWhite
                ? PieceColor.White
                : PieceColor.Black;

            _botColor = playerIsWhite
                ? PieceColor.Black
                : PieceColor.White;

            // Новая или восстановленная партия
            _gameState = restoredState ?? new GameState();

            _gameEngine = new GameEngine();

            _bot = new CheckersBot(
                _botColor,
                difficulty);

            PlayerColorText.Text = playerIsWhite
                ? "Ваш цвет: белые"
                : "Ваш цвет: чёрные";

            UpdateGame();

            Loaded += GameWindow_Loaded;
            Closed += GameWindow_Closed;
        }


        private async Task<bool> TryOfferRepetitionDrawAsync()
        {
            if (_gameOver || _isClosing)
                return false;

            if (!_gameEngine.IsThreefoldRepetition(_gameState))
                return false;

            MessageBoxResult answer = MessageBox.Show(
                "Текущая позиция повторилась три раза.\n\n" +
                "Хотите заявить ничью?",
                "Повторение позиции",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
                return false;

            try
            {
                await _gameRepository.FinishGameAsync(
                    _gameId,
                    _userId,
                    "Draw");

                _gameOver = true;
                TurnText.Text = "Партия завершена вничью";

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось сохранить ничью:\n" + ex.Message,
                    "Ошибка базы данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }
        }


        public GameWindow(GameRecord savedGame)
            : this(
                savedGame.PlayerColor == PieceColor.White,
                savedGame.Difficulty,
                savedGame.Id,
                savedGame.UserId,
                savedGame.State)
        {
        }

        private async Task<bool> TrySaveMoveAsync(
            Move move,
            PieceColor movingColor)
        {
            if (_isSavingMove || _isClosing || _gameOver)
                return false;

            _isSavingMove = true;

            try
            {
                // Работаем с копией текущей позиции
                GameState nextState = _gameState.Clone();

                // Проверяем и применяем ход на копии
                bool valid = _gameEngine.MakeMove(nextState, move);

                if (!valid)
                    return false;

                // Сохраняем ход и состояние в MySQL
                await _gameRepository.SaveMoveAsync(
                    _gameId,
                    _userId,
                    move,
                    movingColor,
                    nextState);

                // Только после успешного сохранения
                // обновляем реальное состояние игры
                _gameState = nextState;

                return true;
            }
            catch (Exception ex)
            {
                if (!_isClosing)
                {
                    MessageBox.Show(
                        "Не удалось сохранить ход в MySQL:\n" +
                        ex.Message,
                        "Ошибка сохранения",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                return false;
            }
            finally
            {
                _isSavingMove = false;
            }
        }





        private async Task<bool> TryOfferDrawAsync()
        {
            if (_gameOver || _isClosing)
                return false;

            var checker = new DrawRuleChecker();

            string? reason =
                checker.GetReachedDrawReason(_gameState);

            if (reason == null)
                return false;

            MessageBoxResult answer = MessageBox.Show(
                "Достигнуто условие ничьей:\n\n" +
                reason +
                "\n\nХотите заявить ничью?",
                "Ничья",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
                return false;

            try
            {
                await _gameRepository.FinishGameAsync(
                    _gameId,
                    _userId,
                    "Draw");

                _gameOver = true;

                TurnText.Text =
                    "Партия завершена вничью";

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось сохранить ничью:\n" +
                    ex.Message,
                    "Ошибка MySQL",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }
        }



        private void UpdateCapturedCount()
        {
            int playerPieces = 0;
            int botPieces = 0;

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Piece? piece = _gameState.Board.GetPiece(row, col);

                    if (piece == null)
                        continue;

                    if (piece.Color == _playerColor)
                        playerPieces++;
                    else
                        botPieces++;
                }
            }

            PlayerCapturedText.Text =
                $"Вы съели: {12 - botPieces}";

            BotCapturedText.Text =
                $"Компьютер съел: {12 - playerPieces}";
        }



        private async void GameWindow_Loaded(
            object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем все сохранённые ходы партии
                var history = await _gameRepository.GetMoveHistoryAsync(
                    _gameId,
                    _userId);

                if (_isClosing)
                    return;

                // Заполняем список истории ходов
                MovesList.Items.Clear();

                foreach (string moveText in history)
                {
                    MovesList.Items.Add(moveText);
                }

                // Прокручиваем к последнему ходу
                if (MovesList.Items.Count > 0)
                {
                    MovesList.ScrollIntoView(
                        MovesList.Items[MovesList.Items.Count - 1]);
                }

                // Если очередь компьютера — запускаем его
                if (!_gameOver &&
                    _gameState.CurrentTurn == _botColor)
                {
                    await MakeBotMoveAsync();
                }
            }
            catch (Exception ex)
            {
                if (!_isClosing)
                {
                    MessageBox.Show(
                        "Не удалось загрузить историю ходов:\n" +
                        ex.Message,
                        "Ошибка MySQL",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }


        private void GameWindow_Closed(
            object? sender, EventArgs e)
        {
            _isClosing = true;
            _cts.Cancel();
        }

        private void UpdateGame()
        {
            if (_gameOver || _isClosing)
                return;

            _legalMoves = _gameEngine.GetLegalMoves(
                _gameState);

            TurnText.Text = _gameState.CurrentTurn == _playerColor
                ? "Ваш ход"
                : "Ход компьютера";

            UpdateCapturedCount();
            DrawBoard();
        }


        private bool _isAnimating = false;

        private async Task AnimateMoveAsync(Move move)
        {
            _isAnimating = true;

            try
            {
                bool isWhite = _playerColor == PieceColor.White;

                int displayRow = isWhite
                    ? move.From.Row
                    : 7 - move.From.Row;

                int displayCol = isWhite
                    ? move.From.Column
                    : 7 - move.From.Column;

                int index = displayRow * 8 + displayCol;

                if (BoardGrid.Children[index] is not Grid cell)
                    return;

                var piece = cell.Children
                    .OfType<Ellipse>()
                    .FirstOrDefault();

                if (piece == null)
                    return;

                double cellWidth = BoardGrid.ActualWidth / 8;
                double cellHeight = BoardGrid.ActualHeight / 8;

                var transform = new TranslateTransform();
                piece.RenderTransform = transform;

                Panel.SetZIndex(cell, 100);

                double currentX = 0;
                double currentY = 0;

                // Анимируем каждый участок маршрута.
                foreach (BoardPosition position in move.Path.Skip(1))
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    int targetRow = isWhite
                        ? position.Row
                        : 7 - position.Row;

                    int targetCol = isWhite
                        ? position.Column
                        : 7 - position.Column;

                    double targetX =
                        (targetCol - displayCol) * cellWidth;

                    double targetY =
                        (targetRow - displayRow) * cellHeight;

                    var animationX = new DoubleAnimation
                    {
                        From = currentX,
                        To = targetX,
                        Duration = TimeSpan.FromMilliseconds(250)
                    };

                    var animationY = new DoubleAnimation
                    {
                        From = currentY,
                        To = targetY,
                        Duration = TimeSpan.FromMilliseconds(250)
                    };

                    var completion = new TaskCompletionSource<bool>(
                        TaskCreationOptions.RunContinuationsAsynchronously);

                    animationY.Completed += (s, e) =>
                        completion.TrySetResult(true);

                    transform.BeginAnimation(
                        TranslateTransform.XProperty, animationX);

                    transform.BeginAnimation(
                        TranslateTransform.YProperty, animationY);

                    await completion.Task.WaitAsync(_cts.Token);

                    currentX = targetX;
                    currentY = targetY;
                }
            }
            finally
            {
                _isAnimating = false;
            }
        }


        private void DrawBoard()
        {
            BoardGrid.Children.Clear();

            for (int displayRow = 0; displayRow < 8; displayRow++)
            {
                for (int displayCol = 0; displayCol < 8; displayCol++)
                {
                    int row = _playerColor == PieceColor.White
                        ? displayRow
                        : 7 - displayRow;

                    int col = _playerColor == PieceColor.White
                        ? displayCol
                        : 7 - displayCol;

                    var position = new BoardPosition(row, col);

                    bool isDark = (row + col) % 2 == 1;

                    var cell = new Grid
                    {
                        Background = isDark
                            ? new SolidColorBrush(
                                Color.FromRgb(139, 90, 43))
                            : new SolidColorBrush(
                                Color.FromRgb(240, 217, 181)),

                        Cursor = Cursors.Hand
                    };

                    if (_selectedPosition == position)
                    {
                        cell.Background = Brushes.Gold;
                    }

                    else if (_selectedMoves.Any(
                        m => m.Path.Count > _pathStepIndex &&
                             m.Path[_pathStepIndex] == position))
                    {
                        cell.Background = Brushes.LightGreen;
                    }


                    Piece? piece = _gameState.Board.GetPiece(
                        row, col);

                    if (piece != null)
                    {
                        var ellipse = new Ellipse
                        {
                            Fill = piece.Color == PieceColor.White
                                ? Brushes.WhiteSmoke
                                : Brushes.Black,

                            Stroke = piece.IsKing
                                ? Brushes.Gold
                                : Brushes.Gray,

                            StrokeThickness = piece.IsKing ? 4 : 2,
                            Margin = new Thickness(7),

                            IsHitTestVisible = false
                        };

                        cell.Children.Add(ellipse);
                    }

                    cell.MouseLeftButtonDown += async (s, e) =>
                        await Cell_ClickAsync(position);

                    BoardGrid.Children.Add(cell);
                }
            }
        }


        private async Task Cell_ClickAsync(BoardPosition position)
        {
            if (_isBotThinking || _isAnimating ||
                _isSavingMove || _gameOver || _isClosing)
                return;


            if (_gameState.CurrentTurn != _playerColor)
                return;

            Piece? piece = _gameState.Board.GetPiece(
                position.Row, position.Column);

            // Выбор шашки разрешён, пока
            // не начат выбор цепочки взятий.
            if (_pathStepIndex == 1 &&
                piece != null &&
                piece.Color == _playerColor)
            {

                _selectedMoves = _legalMoves
                    .Where(m => m.From == position)
                    .ToList();

                if (_selectedMoves.Count == 0)
                {
                    // Проверяем, есть ли обязательное взятие
                    bool captureRequired = _legalMoves.Any(m => m.IsCapture);

                    if (captureRequired)
                    {
                        TurnText.Text =
                            "Обязательное взятие! Выберите выделенную шашку.";

                        // Выделяем одну из шашек,
                        // которая может выполнить взятие.
                        _selectedPosition = _legalMoves[0].From;

                        _selectedMoves = _legalMoves
                            .Where(m => m.From == _selectedPosition)
                            .ToList();

                        DrawBoard();
                    }

                    return;
                }


                _selectedPosition = position;
                DrawBoard();
                return;
            }

            if (_selectedPosition == null)
                return;

            // Оставляем только маршруты,
            // проходящие через выбранную клетку.
            List<Move> matchingMoves = _selectedMoves
                .Where(m =>
                    m.Path.Count > _pathStepIndex &&
                    m.Path[_pathStepIndex] == position)
                .ToList();

            if (matchingMoves.Count == 0)
                return;

            _selectedMoves = matchingMoves;
            _pathStepIndex++;

            // Если есть продолжение взятия,
            // подсвечиваем следующие клетки.
            bool hasContinuation = _selectedMoves.Any(
                m => m.Path.Count > _pathStepIndex);

            if (hasContinuation)
            {
                TurnText.Text = "Продолжите взятие";
                DrawBoard();
                return;
            }

            // Полный маршрут выбран.
            Move move = _selectedMoves[0];

            try
            {
                await AnimateMoveAsync(move);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_isClosing)
                return;

            bool success = await TrySaveMoveAsync(
                    move,
                    _playerColor);

            if (!success)
            {
                _selectedPosition = null;
                _selectedMoves.Clear();
                _pathStepIndex = 1;

                UpdateGame();
                return;
            }

            AddMoveToHistory(move, _playerColor);

            // Сбрасываем выбранный маршрут.
            _selectedPosition = null;
            _selectedMoves.Clear();
            _pathStepIndex = 1;

            UpdateGame();

            if (CheckGameOver())
                return;

            if (await TryOfferRepetitionDrawAsync())
                return;

            if (await TryOfferDrawAsync())
                return;

            await MakeBotMoveAsync();

        }


        private async Task MakeBotMoveAsync()
        {
            if (_gameOver || _isClosing || _isBotThinking)
                return;

            if (_gameState.CurrentTurn != _botColor)
                return;

            _isBotThinking = true;
            TurnText.Text = "Компьютер думает...";

            try
            {

                var thinkingTime = System.Diagnostics.Stopwatch.StartNew();

                Move? botMove = await _bot.FindMoveAsync(
                    _gameState,
                    _cts.Token);

                thinkingTime.Stop();

                // Минимальное время ожидания перед ходом — 700 мс
                int remainingDelay = Math.Max(
                    0,
                    700 - (int)thinkingTime.ElapsedMilliseconds);

                if (remainingDelay > 0)
                {
                    await Task.Delay(remainingDelay, _cts.Token);
                }


                if (_isClosing || _cts.IsCancellationRequested)
                    return;

                if (botMove == null)
                {
                    CheckGameOver();
                    return;
                }

                await AnimateMoveAsync(botMove);

                if (_isClosing || _cts.IsCancellationRequested)
                    return;

                bool success = await TrySaveMoveAsync(
                    botMove,
                    _botColor);

                if (!success)
                {
                    DrawBoard();
                    return;
                }

                AddMoveToHistory(botMove, _botColor);

                UpdateGame();

                if (!CheckGameOver())
                {
                    if (await TryOfferRepetitionDrawAsync())
                        return;

                    await TryOfferDrawAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Вычисление отменено при закрытии окна.
            }
            catch (Exception ex)
            {
                if (!_isClosing)
                {
                    MessageBox.Show(
                        "Ошибка при вычислении хода компьютера:\n"
                        + ex.Message,
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            finally
            {
                _isBotThinking = false;

                if (!_isClosing && !_gameOver)
                    UpdateGame();
            }
        }

        private void AddMoveToHistory(
            Move move,
            PieceColor color)
        {
            string colorName = color == PieceColor.White
                ? "Белые"
                : "Чёрные";

            string separator = move.IsCapture ? " × " : " → ";

            string moveText =
                $"{_gameState.MoveCount}. {colorName}: " +
                $"{move.From.Row},{move.From.Column}" +
                separator +
                $"{move.To.Row},{move.To.Column}";

            MovesList.Items.Add(moveText);

            MovesList.ScrollIntoView(
                MovesList.Items[^1]);
        }

        private bool CheckGameOver()
        {
            PieceColor? winner = _gameEngine.GetWinner(
                _gameState);

            if (winner == null)
                return false;

            _gameOver = true;

            DrawBoard();

            string message = winner == _playerColor
                ? "Вы победили!"
                : "Компьютер победил!";

            TurnText.Text = "Партия окончена";

            MessageBox.Show(
                message,
                "Игра окончена",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return true;
        }


        private async void SurrenderButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (_gameOver)
            {
                Close();
                return;
            }

            if (_isBotThinking || _isAnimating || _isSavingMove)
            {
                MessageBox.Show(
                    "Дождитесь завершения текущего хода.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Вы уверены, что хотите сдаться?",
                "Подтверждение сдачи",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await _gameRepository.FinishGameAsync(
                    _gameId,
                    _userId,
                    "Lost");

                _gameOver = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось сохранить результат партии:\n" +
                    ex.Message,
                    "Ошибка MySQL",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void MenuButton_Click(
            object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
