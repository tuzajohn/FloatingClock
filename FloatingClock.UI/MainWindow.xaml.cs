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

using FloatingClock.UI.Core.Data;
using FloatingClock.UI.Core.Domain;
using FloatingClock.UI.Helpers.Common;

namespace FloatingClock.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private CancellationTokenSource _timerCts = new CancellationTokenSource();
    private readonly IRepository<ToDo> _todoRepository;

    private const int WaitingPeriodInSeconds = 600; // 10 minutes

    public MainWindow(IRepository<ToDo> todoRepository)
    {
        InitializeComponent(); // Only needs to be called once

        _todoRepository = todoRepository;
        TimeFormat = GetSystemTimeFormat();
        SetupClockTimer();


        this.WindowStartupLocation = WindowStartupLocation.Manual;

        this.MouseLeftButtonUp += Window_MouseLeftButtonUp;

        this.Loaded += (s, e) =>
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width - 20;
            this.Top = desktopWorkingArea.Bottom - this.Height - 20;
        };

        this.Loaded += MainWindow_Loaded;

    }

    private void Window_Closed(object sender, EventArgs e)
    {
        _timerCts.Cancel();
        _timerCts.Dispose();
    }

    private DispatcherTimer timer;
    private string TimeFormat { get; set; }

    private string GetSystemTimeFormat()
    {
        // Check the system's time format (12-hour or 24-hour)
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        var timeFormat = culture.DateTimeFormat.ShortTimePattern;
        return timeFormat.Contains('H') ? "HH:mm:ss" : "hh:mm:ss tt";
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

    private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
        {
            // Allows the window to be dragged freely anywhere without fighting the user
            this.DragMove();
        }
    }

    private void Window_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Force WPF to evaluate the exact layout footprint before calculating
        this.UpdateLayout();

        var desktop = SystemParameters.WorkArea;
        double snapThreshold = 40; // 🧲 Distance in pixels where screen edges become "magnetic"
        double screenMargin = 15;  // 📏 Clean breathing room gap from the taskbar/edges

        double targetLeft = this.Left;
        double targetTop = this.Top;

        // ─── HORIZONTAL SNAPPING MATH (Left vs Right Edges) ───
        if (this.Left < desktop.Left + snapThreshold)
        {
            // Magnetize to Left Edge
            targetLeft = desktop.Left + screenMargin;
        }
        else if (this.Left + this.ActualWidth > desktop.Right - snapThreshold)
        {
            // Magnetize to Right Edge
            targetLeft = desktop.Right - this.ActualWidth - screenMargin;
        }

        // ─── VERTICAL SNAPPING MATH (Top vs Bottom Taskbar) ───
        if (this.Top < desktop.Top + snapThreshold)
        {
            // Magnetize to Top Edge
            targetTop = desktop.Top + screenMargin;
        }
        else if (this.Top + this.ActualHeight > desktop.Bottom - snapThreshold)
        {
            // Magnetize to Bottom Edge (Calculates exact height to stay cleanly above the taskbar)
            targetTop = desktop.Bottom - this.ActualHeight - screenMargin;
        }

        // 3. Move the window to the final calculated coordinates instantly
        this.Left = targetLeft;
        this.Top = targetTop;
    }
    private void UpdatePinnedPosition()
    {
        this.UpdateLayout();

        var desktop = SystemParameters.WorkArea;
        double screenMargin = 15;

        // MATH: If the window is currently sitting in the bottom zone of the screen,
        // recalculate the Top anchor so the expansion pushes the window UP instead of DOWN.
        if (this.Top > (desktop.Height / 2))
        {
            this.Top = desktop.Bottom - this.ActualHeight - screenMargin;
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

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var todos = await _todoRepository.GetAllAsync(todo => !string.IsNullOrEmpty(todo.Title));
            lstTodos.Items.Clear();

            foreach (var todo in todos)
            {
                lstTodos.Items.Add(CreateTodoRow(todo));
            }

            UpdatePinnedPosition();
            StartPeriodicRefresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void AddTodo_Click(object sender, RoutedEventArgs e)
    {
        var inputDialog = new InputDialog("Enter your todo item:", "Add New Todo");

        if (inputDialog.ShowDialog() == true && inputDialog.NewToDo is not null)
        {
            if (string.IsNullOrWhiteSpace(inputDialog.NewToDo.Title)) return;

            // Persist to SQLite (EF Core populates the Id property)
            await _todoRepository.AddAsync(inputDialog.NewToDo);

            // Generate UI row using the shared helper and add to ListBox
            lstTodos.Items.Add(CreateTodoRow(inputDialog.NewToDo));

            UpdatePinnedPosition();
        }
    }



    private async void ClearTodos_Click(object sender, RoutedEventArgs e)
    {
        if (lstTodos.Items.Count == 0) return;

        var isConfirmed = this.PromptConfirmation("Are you sure you want to permanently clear all tasks?", "Clear All To-Dos");

        if (!isConfirmed)
            return;


        try
        {
            foreach (var item in lstTodos.Items)
            {
                if (item is Grid row && row.Tag is Guid todoId)
                {
                    await _todoRepository.DeleteAsync(todoId);
                }
            }

            lstTodos.Items.Clear();

            UpdatePinnedPosition();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to clear tasks from database: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OrganizeTodos_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var allTodos = await _todoRepository.GetAllAsync(todo => !string.IsNullOrEmpty(todo.Title));
            var sortedTodos = allTodos.OrderBy(todo => todo.DueDate).ToList();

            lstTodos.Items.Clear();

            foreach (var todo in sortedTodos)
            {
                lstTodos.Items.Add(CreateTodoRow(todo));
            }

            UpdatePinnedPosition();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to organize items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StartPeriodicRefresh()
    {
        _ = RunRefreshLoopAsync(_timerCts.Token);
    }

    private async Task RunRefreshLoopAsync(CancellationToken cancellationToken)
    {
        await Task.Run(async () => await CleanExpiredTodosAsync(), cancellationToken);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                DateTime now = DateTime.Now;
                int currentSecond = now.Minute * 60 + now.Second;

                int secondsUntilNextTick = WaitingPeriodInSeconds - (currentSecond % WaitingPeriodInSeconds);

                DateTime nextTickTime = now.AddSeconds(secondsUntilNextTick)
                                           .AddMilliseconds(-now.Millisecond);

                TimeSpan delay = nextTickTime - DateTime.Now;

                await Task.Delay(delay, cancellationToken);
                await CleanExpiredTodosAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Timer was stopped gracefully
        }
    }

    private async Task CleanExpiredTodosAsync()
    {
        List<Guid> allTodoIds = [];
        Application.Current.Dispatcher.Invoke(() =>
        {
            allTodoIds = lstTodos.Items.Cast<Grid>()
                .Select(row => row.Tag)
                .OfType<Guid>()
                .ToList();
        });

        List<Guid> expiredIds = [];
        foreach (var id in allTodoIds)
        {
            var todo = await _todoRepository.GetByIdAsync(id);
            if (todo != null && todo.DueDate < DateTime.Now)
            {
                expiredIds.Add(id);
            }
        }

        if (expiredIds.Count > 0)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var rowsToRemove = lstTodos.Items.Cast<Grid>()
                    .Where(row => row.Tag is Guid id && expiredIds.Contains(id))
                    .ToList();

                foreach (var row in rowsToRemove)
                {
                    await _todoRepository.DeleteAsync((Guid)row.Tag);
                    lstTodos.Items.Remove(row);
                }
            });
        }
    }

    private Grid CreateTodoRow(ToDo todo)
    {
        var containerGrid = new Grid
        {
            Margin = new Thickness(0, 4, 0, 4),
            Tag = todo.Id
        };

        var cardBackground = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 8, 10, 8)
        };

        var contentLayout = new Grid();
        contentLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        contentLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

        var textStack = new StackPanel { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center };

        // ─── THE WRAPPING TEXTBLOCK FIX ───
        var txtTask = new TextBlock
        {
            Text = todo.Description,
            Foreground = Brushes.White,
            FontSize = 13,
            FontWeight = FontWeights.Medium,
            TextWrapping = TextWrapping.Wrap, // 🔄 Enables text wrapping

            // 📏 CRITICAL BUDGET: Restricts the horizontal layout boundary 
            // so long tasks wrap downward instead of stretching the card box.
            MaxWidth = 220
        };

        bool isPastDue = todo.DueDate < DateTime.Now;
        var date = new TextBlock
        {
            Text = todo.DueDate.ToString("g"),
            Foreground = isPastDue ? new SolidColorBrush(Color.FromRgb(239, 83, 80)) : new SolidColorBrush(Color.FromRgb(160, 160, 160)),
            FontSize = 11,
            FontWeight = isPastDue ? FontWeights.SemiBold : FontWeights.Normal
        };

        textStack.Children.Add(txtTask);
        textStack.Children.Add(date);
        Grid.SetColumn(textStack, 0);
        contentLayout.Children.Add(textStack);

        // Delete Button
        var deleteBtn = new Button
        {
            Content = "✕",
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
            Width = 20,
            Height = 20,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        var btnTemplate = new ControlTemplate(typeof(Button));
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.Name = "BtnBg";
        borderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));

        var btnContent = new FrameworkElementFactory(typeof(ContentPresenter));
        btnContent.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        btnContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        borderFactory.AppendChild(btnContent);
        btnTemplate.VisualTree = borderFactory;

        var hoverTrigger = new Trigger { Property = IsMouseOverProperty, Value = true };
        hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), "BtnBg"));
        hoverTrigger.Setters.Add(new Setter(Button.ForegroundProperty, new SolidColorBrush(Color.FromRgb(239, 83, 80))));
        btnTemplate.Triggers.Add(hoverTrigger);
        deleteBtn.Template = btnTemplate;

        deleteBtn.Click += async (s, args) =>
        {
            lstTodos.Items.Remove(containerGrid);
            if (containerGrid.Tag is Guid todoId)
            {
                await _todoRepository.DeleteAsync(todoId);
            }
            UpdatePinnedPosition(); // Keeps window positioning math in sync after deleting wrapped multi-line rows
        };

        Grid.SetColumn(deleteBtn, 1);
        contentLayout.Children.Add(deleteBtn);

        cardBackground.Child = contentLayout;
        containerGrid.Children.Add(cardBackground);

        return containerGrid;
    }

}