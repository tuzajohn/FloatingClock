using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using FloatingClock.UI.Core.Data;
using FloatingClock.UI.Core.Domain;

using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace FloatingClock.UI;

public class InputDialog : Window
{
    private TextBox txtInput;
    private DatePicker datePicker;
    private ComboBox cmbHours;
    private ComboBox cmbMinutes;

    public string Answer => txtInput.Text;
    public ToDo? NewToDo { get; private set; }

    public InputDialog(string prompt, string title)
    {
        Width = 320;
        Height = 275;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Topmost = true;
        Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));

        this.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(
            new Uri("pack://application:,,,/FloatingClock.UI;component/Assets/app.ico"));


        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;

        var outerBorder = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(55, 55, 55)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Background = Background
        };

        var mainLayout = new Grid();
        mainLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // Custom Title Bar
        var titleBar = new Grid
        {
            Background = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
            Height = 34,
            Margin = new Thickness(14, 0, 8, 0)
        };
        titleBar.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star)
        });

        titleBar.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Auto)
        });
        titleBar.MouseLeftButtonDown += (s, e) =>
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) 
                DragMove();
        };

        var txtTitle = new TextBlock { Text = title, Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)), FontSize = 12, FontWeight = FontWeights.Medium, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(txtTitle, 0);
        titleBar.Children.Add(txtTitle);

        // Title Bar Close Button
        var closeBtn = new Button { Content = "✕", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)), Width = 24, Height = 24, VerticalAlignment = VerticalAlignment.Center, Cursor = System.Windows.Input.Cursors.Hand };
        var closeTemplate = new ControlTemplate(typeof(Button));
        var closeBorderFactory = new FrameworkElementFactory(typeof(Border));
        closeBorderFactory.Name = "CloseBg";
        closeBorderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        closeBorderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));
        var closeContent = new FrameworkElementFactory(typeof(ContentPresenter));
        closeContent.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        closeContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        closeBorderFactory.AppendChild(closeContent);
        closeTemplate.VisualTree = closeBorderFactory;
        var closeHoverTrigger = new Trigger { Property = IsMouseOverProperty, Value = true };
        closeHoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)), "CloseBg"));
        closeHoverTrigger.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));
        closeTemplate.Triggers.Add(closeHoverTrigger);
        closeBtn.Template = closeTemplate;
        closeBtn.Click += (s, e) => { DialogResult = false; Close(); };
        Grid.SetColumn(closeBtn, 1);
        titleBar.Children.Add(closeBtn);

        Grid.SetRow(titleBar, 0);
        mainLayout.Children.Add(titleBar);

        // Main Layout Grid
        var contentGrid = new Grid { Margin = new Thickness(16, 12, 16, 16) };
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // Prompt Label
        var lbl = new TextBlock { Text = prompt.ToUpper(), Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)), FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(2, 0, 0, 6) };
        Grid.SetRow(lbl, 0);
        contentGrid.Children.Add(lbl);

        // Task TextBox
        txtInput = new TextBox { Margin = new Thickness(0, 0, 0, 14), Padding = new Thickness(6), Background = new SolidColorBrush(Color.FromRgb(42, 42, 42)), Foreground = Brushes.White, BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)), FontSize = 13 };
        txtInput.GotFocus += (s, e) => txtInput.BorderBrush = new SolidColorBrush(Color.FromRgb(211, 47, 47));
        txtInput.LostFocus += (s, e) => txtInput.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));
        Grid.SetRow(txtInput, 1);
        contentGrid.Children.Add(txtInput);

        // Date/Time Label
        var dataLbl = new TextBlock { Text = "DUE DATE AND TIME", Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)), FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(2, 0, 0, 6) };
        Grid.SetRow(dataLbl, 2);
        contentGrid.Children.Add(dataLbl);

        var dateTimePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };

        // 1. DATEPICKER PAST BLOCK CONSTRAINT
        datePicker = new DatePicker
        {
            SelectedDate = DateTime.Today,
            Width = 140,
            Margin = new Thickness(0, 0, 8, 0),
            Foreground = Brushes.Black,
            // Gray out and disable all calendar selection handles prior to today's date
            DisplayDateStart = DateTime.Today
        };
        dateTimePanel.Children.Add(datePicker);

        // Hour Dropdown
        cmbHours = new ComboBox { Width = 50, Margin = new Thickness(0, 0, 4, 0), Height = 26, VerticalContentAlignment = VerticalAlignment.Center };
        for (int i = 0; i < 24; i++) cmbHours.Items.Add(i.ToString("D2"));
        cmbHours.SelectedValue = DateTime.Now.Hour.ToString("D2");
        dateTimePanel.Children.Add(cmbHours);

        var colon = new TextBlock { Text = ":", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 4, 0), FontWeight = FontWeights.Bold };
        dateTimePanel.Children.Add(colon);

        // Minute Dropdown
        cmbMinutes = new ComboBox { Width = 50, Height = 26, VerticalContentAlignment = VerticalAlignment.Center };
        for (int i = 0; i < 60; i++) cmbMinutes.Items.Add(i.ToString("D2"));
        cmbMinutes.SelectedValue = DateTime.Now.Minute.ToString("D2");
        dateTimePanel.Children.Add(cmbMinutes);

        Grid.SetRow(dateTimePanel, 3);
        contentGrid.Children.Add(dateTimePanel);

        // Hidden Escape key hook button
        var btnCancelHidden = new Button { IsCancel = true, Width = 0, Height = 0, Visibility = Visibility.Collapsed };
        btnCancelHidden.Click += (s, e) => { DialogResult = false; Close(); };
        contentGrid.Children.Add(btnCancelHidden);

        // Save Button
        var btnSave = new Button { Content = "Save Task", IsDefault = true, Padding = new Thickness(16, 6, 16, 6), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom };
        StyleButton(btnSave, isAccent: true);

        btnSave.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(txtInput.Text))
            {
                MessageBox.Show("Please enter a description for the task item.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime baseDate = datePicker.SelectedDate ?? DateTime.Today;
            int hours = int.Parse((string)cmbHours.SelectedValue);
            int minutes = int.Parse((string)cmbMinutes.SelectedValue);

            // Stitch current selections together
            var finalCombinedDateTime = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, hours, minutes, 0);

            // 2. TIME-PICKER PAST VALUE VALIDATION CONSTRAINT
            if (finalCombinedDateTime < DateTime.Now)
            {
                MessageBox.Show("The deadline time cannot be in the past. Please select a future time.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NewToDo = new ToDo(Guid.NewGuid(), "Item - title", txtInput.Text, DueDate: finalCombinedDateTime, false);
            DialogResult = true;
            Close();
        };

        Grid.SetRow(btnSave, 4);
        contentGrid.Children.Add(btnSave);

        Grid.SetRow(contentGrid, 1);
        mainLayout.Children.Add(contentGrid);

        outerBorder.Child = mainLayout;
        Content = outerBorder;

        Loaded += (s, e) => txtInput.Focus();
    }

    private void StyleButton(Button button, bool isAccent)
    {
        button.Foreground = Brushes.White;
        button.BorderThickness = new Thickness(0);
        button.Cursor = System.Windows.Input.Cursors.Hand;
        button.FontWeight = FontWeights.Medium;

        var baseColor = isAccent ? Color.FromRgb(211, 47, 47) : Color.FromRgb(60, 60, 60);
        var hoverColor = isAccent ? Color.FromRgb(244, 67, 54) : Color.FromRgb(80, 80, 80);
        var pressedColor = isAccent ? Color.FromRgb(183, 28, 28) : Color.FromRgb(45, 45, 45);

        var template = new ControlTemplate(typeof(Button));
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.Name = "ButtonBorder";
        borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(baseColor));
        borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
        borderFactory.SetValue(Border.PaddingProperty, button.Padding);
        var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        borderFactory.AppendChild(contentFactory);
        template.VisualTree = borderFactory;
        var hoverTrigger = new Trigger { Property = IsMouseOverProperty, Value = true };
        hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(hoverColor), "ButtonBorder"));
        template.Triggers.Add(hoverTrigger);
        var pressedTrigger = new Trigger { Property = System.Windows.Controls.Primitives.ButtonBase.IsPressedProperty, Value = true };
        pressedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(pressedColor), "ButtonBorder"));
        template.Triggers.Add(pressedTrigger);
        button.Template = template;
    }
}