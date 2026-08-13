using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DukanHisab.App;

internal static class SoftUiBootstrapper
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnWindowLoaded),
            true);
    }

    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window) return;

        // Run after the existing Loaded polish so the programmatically-created
        // Jazz/Easy/Tanzm rows are already present before the soft pass starts.
        window.Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(window.ApplySoftUiAndClosing));
    }
}

public partial class MainWindow
{
    private bool _softUiAndClosingApplied;

    internal void ApplySoftUiAndClosing()
    {
        if (_softUiAndClosingApplied) return;
        _softUiAndClosingApplied = true;

        TryApplyClassicIcon();
        SoftUiTools.Apply(this);
        PolishClosingLayout();
    }

    private void TryApplyClassicIcon()
    {
        try
        {
            Icon = BitmapFrame.Create(new Uri("pack://application:,,,/DukanHisab.ico", UriKind.Absolute));
        }
        catch
        {
            // Local source-only builds may not have the decoded icon file.
            // GitHub release builds restore it before publish.
        }
    }

    private void PolishClosingLayout()
    {
        if (ClosingPage.Content is not StackPanel stack || stack.Children.Count < 4) return;

        var guide = stack.Children.OfType<Border>().FirstOrDefault();
        var accountCard = stack.Children.OfType<Border>()
            .FirstOrDefault(b => b.Child is Grid g && g.Children.OfType<TextBox>().Any(x => ReferenceEquals(x, CloseComputer)));
        var summary = stack.Children.OfType<UniformGrid>().FirstOrDefault();
        var auditCard = stack.Children.OfType<Border>().LastOrDefault();

        if (guide is not null)
        {
            guide.Background = SoftUiTools.Brush("#FFF7E4");
            guide.BorderBrush = SoftUiTools.Brush("#EAD7A5");
            guide.BorderThickness = new Thickness(1);
            guide.CornerRadius = new CornerRadius(16);
            guide.Padding = new Thickness(18, 14, 18, 14);
            guide.Margin = new Thickness(7, 7, 7, 10);

            if (guide.Child is TextBlock note)
            {
                note.Text = "دن کے آخر میں ہر اکاؤنٹ کی اصل موجود رقم لکھیں۔ یہی اصل منافع کی آخری بنیاد ہے۔ تنظم سے متعلق آنے اور جانے والی رقم درست درج ہونا ضروری ہے۔";
                note.FontFamily = SoftUiTools.UrduFont;
                note.FontSize = 18;
                note.Foreground = SoftUiTools.Brush("#57451D");
                note.LineHeight = 31;
            }
        }

        if (accountCard is not null)
        {
            var index = stack.Children.IndexOf(accountCard);
            if (index >= 0)
            {
                stack.Children.Insert(index, BuildClosingFlow());
                stack.Children.Insert(index + 1, new TextBlock
                {
                    Text = "اکاؤنٹس کی اصل موجود رقم",
                    FontFamily = SoftUiTools.UrduFont,
                    FontSize = 22,
                    FontWeight = FontWeights.Bold,
                    Foreground = SoftUiTools.Brush("#183D33"),
                    Margin = new Thickness(10, 10, 10, 2),
                    HorizontalAlignment = HorizontalAlignment.Right
                });
            }

            accountCard.Background = Brushes.White;
            accountCard.BorderBrush = SoftUiTools.Brush("#DDE7E3");
            accountCard.BorderThickness = new Thickness(1);
            accountCard.CornerRadius = new CornerRadius(16);
            accountCard.Padding = new Thickness(18, 16, 18, 16);
            accountCard.Margin = new Thickness(7, 6, 7, 8);

            if (accountCard.Child is Grid grid)
            {
                foreach (var text in grid.Children.OfType<TextBlock>())
                {
                    text.FontFamily = SoftUiTools.UrduFont;
                    text.VerticalAlignment = VerticalAlignment.Center;
                    text.Margin = new Thickness(8, 5, 8, 5);

                    if (Grid.GetRow(text) == 0)
                    {
                        text.FontSize = 18;
                        text.FontWeight = FontWeights.Bold;
                        text.Foreground = SoftUiTools.Brush("#28483F");
                        text.Text = Grid.GetColumn(text) switch
                        {
                            0 => "اکاؤنٹ",
                            1 => "ابتدائی رقم",
                            2 => "حسابی موجود رقم",
                            3 => "اصل موجود رقم",
                            _ => text.Text
                        };
                    }
                    else if (Grid.GetColumn(text) == 0)
                    {
                        text.FontSize = 17;
                        text.Foreground = SoftUiTools.Brush("#2D403A");
                    }
                }

                foreach (var box in grid.Children.OfType<TextBox>())
                {
                    box.FontFamily = SoftUiTools.NumberFont;
                    box.FontSize = 17;
                    box.HorizontalContentAlignment = HorizontalAlignment.Right;
                    box.MinHeight = 42;
                    box.Margin = new Thickness(8, 5, 8, 5);
                    box.Padding = new Thickness(10, 7, 10, 7);
                }
            }
        }

        if (summary is not null)
        {
            summary.Margin = new Thickness(1, 2, 1, 8);
            var palette = new[] { "#EEF5FF", "#EDF8F2", "#FFF7E4", "#F4EEFF", "#FFF0E9" };
            var cards = summary.Children.OfType<Border>().ToList();
            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                card.Background = SoftUiTools.Brush(palette[Math.Min(i, palette.Length - 1)]);
                card.BorderBrush = SoftUiTools.Brush("#DEE7E3");
                card.BorderThickness = new Thickness(1);
                card.CornerRadius = new CornerRadius(16);
                card.Padding = new Thickness(16, 12, 16, 12);
                card.Margin = new Thickness(6);

                if (card.Child is StackPanel panel)
                {
                    var texts = panel.Children.OfType<TextBlock>().ToList();
                    if (texts.Count > 0)
                    {
                        texts[0].FontFamily = SoftUiTools.UrduFont;
                        texts[0].FontSize = 17;
                        texts[0].Foreground = SoftUiTools.Brush("#40544E");
                        texts[0].Text = texts[0].Text switch
                        {
                            "ابتدائی کل رقم" => "ابتدائی کل رقم",
                            "اختتامی کل رقم" => "اصل کل موجود رقم",
                            "دکان سے تنظم کے پاس گئے" => "دکان سے تنظم کے پاس گئے",
                            "تنظم سے دکان میں آئے" => "تنظم سے دکان میں آئے",
                            _ => texts[0].Text
                        };
                    }
                    if (texts.Count > 1)
                    {
                        texts[1].FontSize = Math.Max(texts[1].FontSize, 23);
                        texts[1].FontWeight = FontWeights.Bold;
                    }
                }
            }
        }

        foreach (var button in SoftUiTools.Descendants<Button>(ClosingPage))
        {
            if (button.Content?.ToString()?.Contains("آخری حساب", StringComparison.Ordinal) == true)
            {
                button.Background = SoftUiTools.Brush("#DDAA34");
                button.Foreground = SoftUiTools.Brush("#183027");
                button.BorderBrush = SoftUiTools.Brush("#C8982A");
                button.MinHeight = 46;
                button.FontSize = 18;
                button.Padding = new Thickness(18, 8, 18, 8);
            }
        }

        if (auditCard is not null && !ReferenceEquals(auditCard, accountCard))
        {
            auditCard.Background = Brushes.White;
            auditCard.BorderBrush = SoftUiTools.Brush("#DDE7E3");
            auditCard.BorderThickness = new Thickness(1);
            auditCard.CornerRadius = new CornerRadius(16);
            auditCard.Padding = new Thickness(16);
            auditCard.Margin = new Thickness(7, 6, 7, 12);
        }
    }

    private static Border BuildClosingFlow()
    {
        var grid = new UniformGrid
        {
            Columns = 3,
            Margin = new Thickness(7, 2, 7, 4)
        };

        grid.Children.Add(ClosingStep("۱", "ابتدائی رقم", "دن شروع ہونے کی رقم", "#EEF5FF"));
        grid.Children.Add(ClosingStep("۲", "حسابی موجود رقم", "انٹریوں کے مطابق", "#EDF8F2"));
        grid.Children.Add(ClosingStep("۳", "اصل موجود رقم", "دن کے آخر میں آپ لکھیں", "#FFF7E4"));

        return new Border
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Child = grid,
            Margin = new Thickness(0, 0, 0, 4)
        };
    }

    private static Border ClosingStep(string number, string title, string subtitle, string background)
    {
        var panel = new StackPanel { Margin = new Thickness(4) };
        panel.Children.Add(new TextBlock
        {
            Text = $"{number}  {title}",
            FontFamily = SoftUiTools.UrduFont,
            FontSize = 19,
            FontWeight = FontWeights.Bold,
            Foreground = SoftUiTools.Brush("#23453C"),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        panel.Children.Add(new TextBlock
        {
            Text = subtitle,
            FontFamily = SoftUiTools.UrduFont,
            FontSize = 15,
            Foreground = SoftUiTools.Brush("#657770"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 1, 0, 0)
        });

        return new Border
        {
            Background = SoftUiTools.Brush(background),
            BorderBrush = SoftUiTools.Brush("#DEE7E3"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(12, 9, 12, 9),
            Margin = new Thickness(5),
            Child = panel
        };
    }
}

internal static class SoftUiTools
{
    internal static readonly FontFamily UrduFont = new("Jameel Noori Nastaleeq");
    internal static readonly FontFamily NumberFont = new("Segoe UI");

    private static readonly ControlTemplate RoundedButtonTemplate = MakeButtonTemplate(10);
    private static readonly ControlTemplate RoundedNavButtonTemplate = MakeButtonTemplate(9);
    private static readonly HashSet<Control> FocusWired = [];

    internal static void Apply(DependencyObject root)
    {
        ApplyNode(root);
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++) Apply(VisualTreeHelper.GetChild(root, i));
    }

    private static void ApplyNode(DependencyObject node)
    {
        switch (node)
        {
            case Window window:
                window.Background = Brush("#F4F7F5");
                break;

            case Border border:
                ApplyBorder(border);
                break;

            case Button button:
                ApplyButton(button);
                break;

            case TextBox box:
                ApplyTextBox(box);
                break;

            case ComboBox combo:
                ApplyComboBox(combo);
                break;

            case DatePicker picker:
                picker.MinHeight = Math.Max(picker.MinHeight, 42);
                picker.BorderBrush = Brush("#C8D5D0");
                picker.BorderThickness = new Thickness(1);
                picker.Background = Brushes.White;
                WireFocus(picker);
                break;

            case DataGrid grid:
                grid.Background = Brushes.White;
                grid.BorderBrush = Brush("#DDE7E3");
                grid.BorderThickness = new Thickness(1);
                grid.GridLinesVisibility = DataGridGridLinesVisibility.Horizontal;
                grid.HorizontalGridLinesBrush = Brush("#E9EFEC");
                grid.VerticalGridLinesBrush = Brushes.Transparent;
                grid.RowBackground = Brushes.White;
                grid.AlternatingRowBackground = Brush("#FAFCFB");
                grid.AlternationCount = 2;
                grid.RowHeight = Math.Max(grid.RowHeight, 39);
                grid.ColumnHeaderHeight = Math.Max(grid.ColumnHeaderHeight, 42);
                grid.CanUserResizeRows = false;
                break;

            case TextBlock text when text.Text is "Next Preview 0.3" or "Next Preview 0.2" or "Next Preview 0.1":
                text.Text = "Next Preview 0.4";
                break;
        }
    }

    private static void ApplyBorder(Border border)
    {
        var isCardStyle = ReferenceEquals(border.Style, Application.Current.TryFindResource("Card") as Style);
        var alreadyRounded = border.CornerRadius.TopLeft > 0;
        if (!isCardStyle && !alreadyRounded) return;

        if (border.Background is SolidColorBrush bg && bg.Color == Color.FromRgb(8, 37, 31)) return;

        border.CornerRadius = new CornerRadius(Math.Max(border.CornerRadius.TopLeft, 15));
        border.BorderBrush ??= Brush("#DEE7E3");
        if (border.BorderThickness == new Thickness(0)) border.BorderThickness = new Thickness(1);

        if (isCardStyle)
        {
            border.Padding = new Thickness(16);
            border.Margin = new Thickness(7);
        }
    }

    private static void ApplyButton(Button button)
    {
        var nav = button.Name.StartsWith("Nav", StringComparison.OrdinalIgnoreCase);
        button.Template = nav ? RoundedNavButtonTemplate : RoundedButtonTemplate;
        button.MinHeight = Math.Max(button.MinHeight, nav ? 44 : 42);
        button.VerticalContentAlignment = VerticalAlignment.Center;
        button.Cursor = System.Windows.Input.Cursors.Hand;

        if (nav)
        {
            button.BorderThickness = new Thickness(0);
            button.Padding = new Thickness(14, 10, 14, 10);
        }
        else
        {
            if (button.BorderThickness == new Thickness(0)) button.BorderThickness = new Thickness(1);
            button.BorderBrush ??= Brush("#C7D3CF");
            button.Padding = new Thickness(14, 8, 14, 8);
        }
    }

    private static void ApplyTextBox(TextBox box)
    {
        box.MinHeight = Math.Max(box.MinHeight, 42);
        box.Padding = new Thickness(10, 7, 10, 7);
        box.BorderBrush = Brush("#C8D5D0");
        box.BorderThickness = new Thickness(1);
        box.Background = Brush("#FCFDFD");
        box.VerticalContentAlignment = VerticalAlignment.Center;
        WireFocus(box);
    }

    private static void ApplyComboBox(ComboBox combo)
    {
        combo.MinHeight = Math.Max(combo.MinHeight, 42);
        combo.Padding = new Thickness(8, 5, 8, 5);
        combo.BorderBrush = Brush("#C8D5D0");
        combo.BorderThickness = new Thickness(1);
        combo.Background = Brushes.White;
        WireFocus(combo);
    }

    private static void WireFocus(Control control)
    {
        if (!FocusWired.Add(control)) return;
        control.GotKeyboardFocus += (_, _) =>
        {
            control.BorderBrush = Brush("#3B8F78");
            control.BorderThickness = new Thickness(1.5);
        };
        control.LostKeyboardFocus += (_, _) =>
        {
            control.BorderBrush = Brush("#C8D5D0");
            control.BorderThickness = new Thickness(1);
        };
    }

    private static ControlTemplate MakeButtonTemplate(double radius)
    {
        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
        border.SetBinding(Border.BackgroundProperty, new Binding(nameof(Control.Background))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
        });
        border.SetBinding(Border.BorderBrushProperty, new Binding(nameof(Control.BorderBrush))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
        });
        border.SetBinding(Border.BorderThicknessProperty, new Binding(nameof(Control.BorderThickness))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
        });

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
        presenter.SetBinding(ContentPresenter.HorizontalAlignmentProperty, new Binding(nameof(Control.HorizontalContentAlignment))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
        });
        presenter.SetBinding(ContentPresenter.VerticalAlignmentProperty, new Binding(nameof(Control.VerticalContentAlignment))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
        });
        presenter.SetBinding(FrameworkElement.MarginProperty, new Binding(nameof(Control.Padding))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
        });
        border.AppendChild(presenter);
        template.VisualTree = border;

        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.90));
        template.Triggers.Add(hover);

        var pressed = new Trigger { Property = ButtonBase.IsPressedProperty, Value = true };
        pressed.Setters.Add(new Setter(UIElement.OpacityProperty, 0.78));
        template.Triggers.Add(pressed);

        return template;
    }

    internal static SolidColorBrush Brush(string hex)
    {
        return (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
    }

    internal static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
}
