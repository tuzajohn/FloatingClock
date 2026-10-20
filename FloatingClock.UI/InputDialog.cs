using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FloatingClock.UI
{
    // Lightweight custom input popup window
    public class InputDialog : Window
    {
        private TextBox txtInput;
        public string Answer => txtInput.Text;

        public InputDialog(string prompt, string title)
        {
            Width = 280;
            Height = 150;
            Title = title;
            WindowStyle = WindowStyle.ToolWindow;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));

            var grid = new Grid { Margin = new Thickness(12) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var lbl = new TextBlock { Text = prompt, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 6) };
            Grid.SetRow(lbl, 0);
            grid.Children.Add(lbl);

            txtInput = new TextBox { Margin = new Thickness(0, 0, 0, 12), Padding = new Thickness(4), Background = Brushes.White };
            Grid.SetRow(txtInput, 1);
            grid.Children.Add(txtInput);

            var btnSave = new Button
            {
                Content = "Save Task",
                IsDefault = true,
                Padding = new Thickness(10, 4, 10, 4),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            btnSave.Click += (s, e) => { DialogResult = true; Close(); };
            Grid.SetRow(btnSave, 2);
            grid.Children.Add(btnSave);

            Content = grid;
        }
    }
}