using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace FloatingClock.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;
        private string TimeFormat { get; set; }

        private string GetSystemTimeFormat()
        {
            // Check the system's time format (12-hour or 24-hour)
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            var timeFormat = culture.DateTimeFormat.ShortTimePattern;
            return timeFormat.Contains('H') ? "HH:mm:ss" : "hh:mm:ss tt";
        }

        public MainWindow()
        {
            InitializeComponent();
            TimeFormat = GetSystemTimeFormat();
            SetupClockTimer();
        }

        private void SetupClockTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();

            // Update immediately on startup
            UpdateClock();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateClock();
        }

        private void UpdateClock()
        {
            TimeFormat = GetSystemTimeFormat();
            txtClock.Text = DateTime.Now.ToString(TimeFormat);
        }

        // WPF's built-in DragMove makes dragging borderless windows effortless
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        // Right-click context menu to close the app (since there's no title bar)
        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonUp(e);

            ContextMenu menu = new();
            MenuItem exitItem = new() { Header = "Exit" };
            exitItem.Click += (s, args) => Application.Current.Shutdown();
            menu.Items.Add(exitItem);

            menu.IsOpen = true;
        }

        private void AddTodo_Click(object sender, RoutedEventArgs e)
        {
            // Open the custom popup dialog
            var inputDialog = new InputDialog("Enter your todo item:", "Add New Todo");

            // If the user clicked save and entered text, add it to the list
            if (inputDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(inputDialog.Answer))
            {
                string taskText = inputDialog.Answer;

                var newRow = new Grid
                {
                    ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }
            }
                };

                var txtTask = new TextBlock
                {
                    Text = taskText,
                    Margin = new Thickness(5),
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetColumn(txtTask, 0);
                newRow.Children.Add(txtTask);

                var deleteBtn = new Button
                {
                    Content = "❌",
                    Margin = new Thickness(4, 0, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent
                };
                Grid.SetColumn(deleteBtn, 1);

                deleteBtn.Click += (s, args) =>
                {
                    lstTodos.Items.Remove(newRow);
                };
                newRow.Children.Add(deleteBtn);

                lstTodos.Items.Add(newRow);
            }
        }

        private void ClearTodos_Click(object sender, RoutedEventArgs e)
        {
            lstTodos.Items.Clear();
        }

        private void OrganizeTodos_Click(object sender, RoutedEventArgs e)
        {
            // Add your sorting or organizing logic here
        }
    }
}