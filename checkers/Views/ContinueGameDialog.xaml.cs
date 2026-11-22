
using System.Windows;

namespace checkers.Views
{
    public enum ContinueGameChoice
    {
        Continue,
        NewGame,
        FinishGame,
        Later
    }

    public partial class ContinueGameDialog : Window
    {
        public ContinueGameChoice Choice { get; private set; }
            = ContinueGameChoice.Later;

        public ContinueGameDialog()
        {
            InitializeComponent();
        }

        private void Continue_Click(
            object sender, RoutedEventArgs e)
        {
            Choice = ContinueGameChoice.Continue;
            DialogResult = true;
        }

        private void NewGame_Click(
            object sender, RoutedEventArgs e)
        {
            Choice = ContinueGameChoice.NewGame;
            DialogResult = true;
        }

        private void Later_Click(
            object sender, RoutedEventArgs e)
        {
            Choice = ContinueGameChoice.Later;
            DialogResult = true;
        }

        private void FinishGame_Click(
            object sender, RoutedEventArgs e)
        {
            Choice = ContinueGameChoice.FinishGame;
            DialogResult = true;
        }
    }
}
