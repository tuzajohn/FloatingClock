using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FloatingClock.UI;

public class ConfirmationDialog : Window
{
    public bool IsConfirmed { get; private set; } = false;

    public ConfirmationDialog(string message, string title)
    {
        Width = 280;
        Height = 150; // Increased slightly to make comfortable room for the custom title bar
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Topmost = true;
        Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));

        this.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(
            new Uri("pack://application:,,,/FloatingClock.UI;component/Assets/app.ico"));


        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;

        // Add a subtle border overlay around the entire popup to match your MainWindow
        var outerBorder = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Background = Background
        };

        var mainLayout = new Grid();
        // Row 0: Custom Title Bar, Row 1: Message Content, Row 2: Buttons Actions
        mainLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        mainLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // 2. BUILD THE CUSTOM DARK TITLE BAR ROW
        var titleBar = new Grid
        {
            Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)), // Slightly lighter dark for contrast
            Height = 30,
            Margin = new Thickness(10, 0, 10, 0)
        };
        // Make the custom title bar draggable just like a regular window!
        titleBar.MouseLeftButtonDown += (s, e) => { if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) DragMove(); };

        var txtTitle = new TextBlock
        {
            Text = title,
            Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        titleBar.Children.Add(txtTitle);

        Grid.SetRow(titleBar, 0);
        mainLayout.Children.Add(titleBar);

        // 3. MESSAGE LAYOUT BLOCK
        var txtMessage = new TextBlock
        {
            Text = message,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(14, 10, 14, 10)
        };
        Grid.SetRow(txtMessage, 1);
        mainLayout.Children.Add(txtMessage);

        // 4. ACTION BUTTONS ROW
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 14, 14)
        };

        var btnYes = new Button
        {
            Content = "Yes",
            IsDefault = true,
            Padding = new Thickness(14, 4, 14, 4),
            Margin = new Thickness(0, 0, 8, 0)
        };
        StyleButton(btnYes, isAccent: true);
        btnYes.Click += (s, e) => { IsConfirmed = true; DialogResult = true; Close(); };

        var btnNo = new Button
        {
            Content = "No",
            IsCancel = true,
            Padding = new Thickness(14, 4, 14, 4)
        };
        StyleButton(btnNo, isAccent: false);
        btnNo.Click += (s, e) => { IsConfirmed = false; DialogResult = false; Close(); };

        buttonPanel.Children.Add(btnYes);
        buttonPanel.Children.Add(btnNo);

        Grid.SetRow(buttonPanel, 2);
        mainLayout.Children.Add(buttonPanel);

        outerBorder.Child = mainLayout;
        Content = outerBorder;
    }

    private void StyleButton(Button button, bool isAccent)
    {
        button.Foreground = Brushes.White;
        button.BorderThickness = new Thickness(0);
        button.Cursor = System.Windows.Input.Cursors.Hand;

        var baseColor = isAccent ? Color.FromRgb(211, 47, 47) : Color.FromRgb(60, 60, 60);
        var hoverColor = isAccent ? Color.FromRgb(244, 67, 54) : Color.FromRgb(80, 80, 80);
        var pressedColor = isAccent ? Color.FromRgb(183, 28, 28) : Color.FromRgb(45, 45, 45);

        var template = new ControlTemplate(typeof(Button));
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.Name = "ButtonBorder";
        borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(baseColor));
        borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
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
