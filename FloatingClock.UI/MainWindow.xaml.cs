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
    private readonly IRepository<ToDo> _todoRepository;

    public MainWindow(IRepository<ToDo> todoRepository)
    {
        InitializeComponent(); // Only needs to be called once

        _todoRepository = todoRepository;
        TimeFormat = GetSystemTimeFormat();
        SetupClockTimer();

        // Wire up the Loaded event to load tasks from SQLite on startup
        this.Loaded += MainWindow_Loaded;
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
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to organize items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }



    private Grid CreateTodoRow(ToDo todo)
    {
        // Outer structural row grid container
        var containerGrid = new Grid
        {
            Margin = new Thickness(0, 4, 0, 4), // Breathing room between individual items
            Tag = todo.Id
        };

        // Sleek, semi-transparent card background accentuating your dark mode widget theme
        var cardBackground = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)), // Subtle highlight overlay
            BorderBrush = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 8, 10, 8)
        };

        var contentLayout = new Grid();
        contentLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        contentLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

        // Left Column Layout: Task title on top, Date sub-label underneath
        var textStack = new StackPanel { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center };

        var txtTask = new TextBlock
        {
            Text = todo.Description,
            Foreground = Brushes.White,
            FontSize = 13,
            FontWeight = FontWeights.Medium,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 4)
        };

        // Format the date dynamically: highlight past due items in subtle red
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

        // Right Column Layout: Reusable custom-styled circular Delete Button
        var deleteBtn = new Button
        {
            Content = "✕", // Swapped out flat emoji for a sleek geometric close glyph
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
            Width = 20,
            Height = 20,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        // Custom runtime template style for the delete button (circular hover highlighting)
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

        // Hover background behavior
        var hoverTrigger = new Trigger { Property = IsMouseOverProperty, Value = true };
        hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), "BtnBg"));
        hoverTrigger.Setters.Add(new Setter(Button.ForegroundProperty, new SolidColorBrush(Color.FromRgb(239, 83, 80)))); // Pops red on hover
        btnTemplate.Triggers.Add(hoverTrigger);
        deleteBtn.Template = btnTemplate;

        deleteBtn.Click += async (s, args) =>
        {
            lstTodos.Items.Remove(containerGrid);
            if (containerGrid.Tag is Guid todoId)
            {
                await _todoRepository.DeleteAsync(todoId);
            }
        };

        Grid.SetColumn(deleteBtn, 1);
        contentLayout.Children.Add(deleteBtn);

        cardBackground.Child = contentLayout;
        containerGrid.Children.Add(cardBackground);

        return containerGrid;
    }


}