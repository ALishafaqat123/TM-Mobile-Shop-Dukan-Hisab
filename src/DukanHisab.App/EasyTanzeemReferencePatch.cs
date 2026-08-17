using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using DukanHisab.Core;

namespace DukanHisab.App;

internal static class EasyTanzeemReferenceBootstrapper
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnWindowLoaded),
            true);

        EventManager.RegisterClassHandler(
            typeof(Button),
            Button.ClickEvent,
            new RoutedEventHandler(OnAnyButtonClick),
            true);
    }

    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window) return;
        window.Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(window.EnsureEasyTanzeemAndReferenceUi));
    }

    private static void OnAnyButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (Window.GetWindow(button) is not MainWindow window) return;

        if (ReferenceEquals(button.Parent, window.ClosingMessage.Parent))
        {
            e.Handled = true;
            window.SaveClosingWithEasyTanzeem();
            return;
        }

        if (button == window.NavDashboard || button == window.NavJazz || button == window.NavEasy ||
            button == window.NavTanzeem || button == window.NavAccessories || button == window.NavClosing ||
            button == window.NavHistory)
        {
            window.ResetEasyTanzeemNavVisual();
        }

        window.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() =>
            {
                window.RefreshExtendedAccountUi();
                window.PopulateExtendedCounterChoices();
            }));
    }
}

public partial class MainWindow
{
    private Button? _navEasyTanzeem;
    private Button? _referenceButton;
    private TextBlock? _dashEasyTanzeem;
    private TextBlock? _closeOpenEasyTanzeem;
    private TextBlock? _closeBookEasyTanzeem;
    private TextBox? _closeEasyTanzeem;

    internal void EnsureEasyTanzeemAndReferenceUi()
    {
        EnsureEasyTanzeemNavigation();
        EnsureEasyTanzeemDashboardCard();
        EnsureEasyTanzeemClosingRow();

        DayPicker.SelectedDateChanged += (_, _) => Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() =>
            {
                RefreshExtendedAccountUi();
                PopulateExtendedCounterChoices();
            }));

        RefreshExtendedAccountUi();
        PopulateExtendedCounterChoices();
        PolishVisualTree(this);
        CurrencyTypographyPatch.AttachFor(this);
    }

    private void EnsureEasyTanzeemNavigation()
    {
        if (NavEasy.Parent is not StackPanel navPanel) return;

        if (_navEasyTanzeem is null)
        {
            _navEasyTanzeem = new Button
            {
                Content = "●  ایزی پیسہ تنظم",
                Style = TryFindResource("SideButton") as Style,
                FontFamily = new FontFamily("Jameel Noori Nastaleeq"),
                FontSize = 17,
                MinHeight = 44,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                Padding = new Thickness(14, 9, 14, 9),
                Margin = new Thickness(0, 3, 0, 3),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            _navEasyTanzeem.Click += (_, _) =>
            {
                _currentAccount = Accounts.EasyTanzeem;
                PrepareClearAccountForms();
                PopulateExtendedCounterChoices();
                FastShowPage(AccountPage, "ایزی پیسہ تنظم", _navEasyTanzeem);
                RefreshExtendedAccountUi();
            };

            var easyIndex = navPanel.Children.IndexOf(NavEasy);
            navPanel.Children.Insert(easyIndex >= 0 ? easyIndex + 1 : navPanel.Children.Count, _navEasyTanzeem);
        }

        if (_referenceButton is null)
        {
            _referenceButton = new Button
            {
                Content = "☑  آج کا ریفرنس",
                Style = TryFindResource("SideButton") as Style,
                FontFamily = new FontFamily("Jameel Noori Nastaleeq"),
                FontSize = 17,
                MinHeight = 44,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                Padding = new Thickness(14, 9, 14, 9),
                Margin = new Thickness(0, 3, 0, 3),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            _referenceButton.Click += (_, _) => ShowManualReferenceDialog();

            var easyTanzeemIndex = navPanel.Children.IndexOf(_navEasyTanzeem);
            navPanel.Children.Insert(easyTanzeemIndex >= 0 ? easyTanzeemIndex + 1 : navPanel.Children.Count, _referenceButton);
        }
    }

    private void EnsureEasyTanzeemDashboardCard()
    {
        if (_dashEasyTanzeem is not null) return;
        if (DashEasy.Parent is not StackPanel easyStack) return;
        if (easyStack.Parent is not Border easyCard) return;
        if (easyCard.Parent is not UniformGrid accountsGrid) return;

        accountsGrid.Columns = 6;
        _dashEasyTanzeem = new TextBlock
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 22,
            FontWeight = FontWeights.Bold
        };

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = "ایزی پیسہ تنظم",
            FontFamily = new FontFamily("Jameel Noori Nastaleeq"),
            FontSize = 16
        });
        stack.Children.Add(_dashEasyTanzeem);

        accountsGrid.Children.Add(new Border
        {
            Style = TryFindResource("Card") as Style,
            Child = stack
        });
    }

    private void EnsureEasyTanzeemClosingRow()
    {
        if (_closeEasyTanzeem is not null) return;
        if (CloseEasy.Parent is not Grid closingGrid) return;

        var row = closingGrid.RowDefinitions.Count;
        closingGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var label = new TextBlock
        {
            Text = "ایزی پیسہ تنظم",
            FontFamily = new FontFamily("Jameel Noori Nastaleeq"),
            VerticalAlignment = VerticalAlignment.Center
        };
        _closeOpenEasyTanzeem = NewClosingValueText();
        _closeBookEasyTanzeem = NewClosingValueText();
        _closeEasyTanzeem = new TextBox
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 16,
            MinHeight = 40,
            Margin = new Thickness(3),
            Padding = new Thickness(7, 5, 7, 5)
        };

        AddClosingCell(closingGrid, label, row, 0);
        AddClosingCell(closingGrid, _closeOpenEasyTanzeem, row, 1);
        AddClosingCell(closingGrid, _closeBookEasyTanzeem, row, 2);
        AddClosingCell(closingGrid, _closeEasyTanzeem, row, 3);
    }

    private static TextBlock NewClosingValueText() => new()
    {
        FontFamily = new FontFamily("Segoe UI"),
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(3)
    };

    private static void AddClosingCell(Grid grid, UIElement element, int row, int column)
    {
        Grid.SetRow(element, row);
        Grid.SetColumn(element, column);
        grid.Children.Add(element);
    }

    internal void ResetEasyTanzeemNavVisual()
    {
        if (_navEasyTanzeem is null) return;
        _navEasyTanzeem.Background = Brushes.Transparent;
        _navEasyTanzeem.Foreground = Brushes.White;
    }

    internal void RefreshExtendedAccountUi()
    {
        var current = AccountingEngine.CurrentBalances(_day);
        if (_dashEasyTanzeem is not null)
            _dashEasyTanzeem.Text = MoneyText(current.EasyTanzeem);

        if (_closeOpenEasyTanzeem is not null)
            _closeOpenEasyTanzeem.Text = MoneyText(_day.Opening.EasyTanzeem);
        if (_closeBookEasyTanzeem is not null)
            _closeBookEasyTanzeem.Text = MoneyText(current.EasyTanzeem);
        if (_closeEasyTanzeem is not null && !_closeEasyTanzeem.IsKeyboardFocusWithin)
            _closeEasyTanzeem.Text = _day.Closing.EasyTanzeem;

        if (_referenceButton is not null)
        {
            _referenceButton.Content = string.IsNullOrWhiteSpace(_day.ManualReferenceAt)
                ? "☑  آج کا ریفرنس"
                : "✓  آج کا ریفرنس محفوظ";
            _referenceButton.ToolTip = string.IsNullOrWhiteSpace(_day.ManualReferenceAt)
                ? "اس دن کی صبح کی اصل رقمیں نئی بنیاد کے طور پر محفوظ کریں"
                : $"دستی ریفرنس محفوظ: {_day.ManualReferenceAt}";
        }
    }

    internal void PopulateExtendedCounterChoices()
    {
        var accountChoices = new[]
        {
            Accounts.Drawer, Accounts.Jazz, Accounts.Easy, Accounts.EasyTanzeem, Accounts.OneLoad
        }
        .Where(x => x != _currentAccount)
        .Concat(new[] { Accounts.Tanzeem, Accounts.External })
        .ToArray();

        if (_receiveCounter is not null)
        {
            FillCounterCombo(_receiveCounter, accountChoices);
            if (_receiveCounter.Items.Count > 0) _receiveCounter.SelectedIndex = 0;
        }
        if (_sendCounter is not null)
        {
            FillCounterCombo(_sendCounter, accountChoices);
            if (_sendCounter.Items.Count > 0) _sendCounter.SelectedIndex = 0;
        }

        var tanzeemChoices = new[]
        {
            Accounts.Drawer, Accounts.Jazz, Accounts.Easy, Accounts.EasyTanzeem, Accounts.OneLoad
        };
        if (_tanzeemToAccount is not null)
        {
            FillCounterCombo(_tanzeemToAccount, tanzeemChoices);
            if (_tanzeemToAccount.Items.Count > 0) _tanzeemToAccount.SelectedIndex = 0;
        }
        if (_tanzeemFromAccount is not null)
        {
            FillCounterCombo(_tanzeemFromAccount, tanzeemChoices);
            if (_tanzeemFromAccount.Items.Count > 0) _tanzeemFromAccount.SelectedIndex = 0;
        }
    }

    private void ShowManualReferenceDialog()
    {
        var dialog = new Window
        {
            Owner = this,
            Title = "آج کا ریفرنس / نئی بنیاد",
            Width = 650,
            Height = 660,
            MinWidth = 620,
            MinHeight = 620,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.FromRgb(245, 247, 246)),
            FlowDirection = FlowDirection.RightToLeft,
            ResizeMode = ResizeMode.CanResizeWithGrip
        };

        var root = new StackPanel { Margin = new Thickness(22) };
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = root
        };
        dialog.Content = scroll;

        root.Children.Add(new TextBlock
        {
            Text = $"تاریخ: {_selectedDate:dd-MM-yyyy}",
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 8)
        });
        root.Children.Add(new TextBlock
        {
            Text = "یہ رقمیں اس دن کی صبح / شروعات کی اصل گنتی ہوں گی۔ اس دن کی تمام انٹریاں اسی نئی بنیاد پر شمار ہوں گی۔ پچھلے دنوں کی ہسٹری تبدیل نہیں ہوگی۔",
            FontFamily = new FontFamily("Jameel Noori Nastaleeq"),
            FontSize = 19,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(55, 83, 74)),
            Margin = new Thickness(0, 0, 0, 12)
        });

        if (!string.IsNullOrWhiteSpace(_day.ManualReferenceAt))
        {
            root.Children.Add(new TextBlock
            {
                Text = $"اس دن کا دستی ریفرنس پہلے سے محفوظ ہے: {_day.ManualReferenceAt}",
                FontFamily = new FontFamily("Jameel Noori Nastaleeq"),
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(10, 122, 91)),
                Margin = new Thickness(0, 0, 0, 10)
            });
        }

        var boxes = new Dictionary<string, TextBox>();
        AddReferenceField(root, boxes, Accounts.Drawer, _day.Opening.Drawer);
        AddReferenceField(root, boxes, Accounts.Computer, _day.Opening.Computer);
        AddReferenceField(root, boxes, Accounts.Jazz, _day.Opening.Jazz);
        AddReferenceField(root, boxes, Accounts.Easy, _day.Opening.Easy);
        AddReferenceField(root, boxes, Accounts.EasyTanzeem, _day.Opening.EasyTanzeem);
        AddReferenceField(root, boxes, Accounts.OneLoad, _day.Opening.Oneload);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };
        var save = new Button
        {
            Content = "ریفرنس محفوظ کریں",
            FontFamily = new FontFamily("Jameel Noori Nastaleeq"),
            FontSize = 18,
            Background = new SolidColorBrush(Color.FromRgb(10, 122, 91)),
            Foreground = Brushes.White,
            Padding = new Thickness(18, 8, 18, 8),
            Margin = new Thickness(4)
        };
        var cancel = new Button
        {
            Content = "منسوخ",
            FontFamily = new FontFamily("Jameel Noori Nastaleeq"),
            FontSize = 18,
            Padding = new Thickness(18, 8, 18, 8),
            Margin = new Thickness(4)
        };
        cancel.Click += (_, _) => dialog.Close();
        save.Click += (_, _) => SaveManualReference(dialog, boxes);
        buttons.Children.Add(save);
        buttons.Children.Add(cancel);
        root.Children.Add(buttons);

        dialog.ShowDialog();
    }

    private void AddReferenceField(StackPanel root, IDictionary<string, TextBox> boxes, string account, decimal currentBase)
    {
        var panel = new Border
        {
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(225, 231, 228)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(10, 6, 10, 8),
            Margin = new Thickness(0, 4, 0, 4)
        };
        var stack = new StackPanel();
        panel.Child = stack;
        stack.Children.Add(new TextBlock
        {
            Text = Accounts.UrduName(account),
            FontFamily = new FontFamily("Jameel Noori Nastaleeq"),
            FontSize = 18,
            FontWeight = FontWeights.Bold
        });
        stack.Children.Add(new TextBlock
        {
            Text = $"موجودہ پرانی بنیاد: {AccountingEngine.Format(currentBase)} روپے",
            FontFamily = new FontFamily("Jameel Noori Nastaleeq"),
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(95, 112, 106))
        });

        var box = new TextBox
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 17,
            MinHeight = 38,
            Padding = new Thickness(8, 5, 8, 5),
            Margin = new Thickness(0, 4, 0, 0)
        };
        if (!string.IsNullOrWhiteSpace(_day.ManualReferenceAt))
            box.Text = AccountingEngine.Format(currentBase);
        stack.Children.Add(box);
        boxes[account] = box;
        root.Children.Add(panel);
    }

    private void SaveManualReference(Window dialog, IReadOnlyDictionary<string, TextBox> boxes)
    {
        var values = new Dictionary<string, decimal>();
        foreach (var account in Accounts.Internal)
        {
            if (!boxes.TryGetValue(account, out var box) || !AccountingEngine.TryMoney(box.Text, out var value) || value < 0)
            {
                MessageBox.Show(
                    $"{Accounts.UrduName(account)} کی اصل رقم درست لکھیں۔ صفر بھی لکھ سکتے ہیں۔",
                    "آج کا ریفرنس",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                box?.Focus();
                return;
            }
            values[account] = value;
        }

        if (_day.Transactions.Count > 0 || AccountingEngine.ClosingComplete(_day.Closing))
        {
            var message = "اس دن کی انٹریاں پہلے سے موجود ہیں۔ جو رقمیں آپ نے لکھی ہیں وہ دن کی صبح / شروعات کی اصل گنتی ہونی چاہئیں، کیونکہ موجودہ انٹریاں اسی ریفرنس پر دوبارہ شمار ہوں گی۔";
            if (AccountingEngine.ClosingComplete(_day.Closing))
                message += "\n\nاس دن کا آخری حساب بھی محفوظ ہے، اس لیے منافع نئی بنیاد کے مطابق دوبارہ شمار ہوگا۔";
            message += "\n\nکیا یہی ریفرنس محفوظ کرنا ہے؟";
            if (MessageBox.Show(message, "ریفرنس کی تصدیق", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
        }

        var before = ReferenceSummary(_day.Opening);
        _day.Opening = new AccountBalances
        {
            Drawer = values[Accounts.Drawer],
            Computer = values[Accounts.Computer],
            Jazz = values[Accounts.Jazz],
            Easy = values[Accounts.Easy],
            EasyTanzeem = values[Accounts.EasyTanzeem],
            Oneload = values[Accounts.OneLoad]
        };
        _day.ManualReferenceAt = DateTime.Now.ToString("hh:mm:ss tt", CultureInfo.InvariantCulture);
        var after = ReferenceSummary(_day.Opening);
        _day.Audit.Add(AccountingEngine.Audit(
            "manual_reference",
            "اس دن کی صبح کا دستی ریفرنس / نئی بنیاد محفوظ کی گئی",
            before,
            after));

        _store.Save(_day);
        RefreshAll();
        RefreshExtendedAccountUi();
        StatusText.Text = "آج کا دستی ریفرنس محفوظ ہوگیا؛ اب اس دن کا منافع اسی نئی بنیاد سے شمار ہوگا۔";
        dialog.DialogResult = true;
    }

    private static string ReferenceSummary(AccountBalances b)
        => $"گلہ {AccountingEngine.Format(b.Drawer)} | کمپیوٹر {AccountingEngine.Format(b.Computer)} | جیز {AccountingEngine.Format(b.Jazz)} | ایزی شفاقت {AccountingEngine.Format(b.Easy)} | ایزی تنظم {AccountingEngine.Format(b.EasyTanzeem)} | ون لوڈ {AccountingEngine.Format(b.Oneload)}";

    internal void SaveClosingWithEasyTanzeem()
    {
        if (_closeEasyTanzeem is null)
        {
            SaveClosing_Click(this, new RoutedEventArgs());
            return;
        }

        var inputs = new Dictionary<string, TextBox>
        {
            [Accounts.Drawer] = CloseDrawer,
            [Accounts.Computer] = CloseComputer,
            [Accounts.Easy] = CloseEasy,
            [Accounts.EasyTanzeem] = _closeEasyTanzeem,
            [Accounts.Jazz] = CloseJazz,
            [Accounts.OneLoad] = CloseOneLoad
        };
        var values = new Dictionary<string, decimal>();
        foreach (var pair in inputs)
        {
            if (!AccountingEngine.TryMoney(pair.Value.Text, out var value) || value < 0)
            {
                ClosingMessage.Text = $"{Accounts.UrduName(pair.Key)} کی اصل رقم درست لکھیں۔";
                pair.Value.Focus();
                return;
            }
            values[pair.Key] = value;
        }

        var oldClosing = _day.Closing.Clone();
        var oldBook = AccountingEngine.CurrentBalances(_day);
        var oldWasComplete = AccountingEngine.ClosingComplete(oldClosing);
        var oldSummary = oldWasComplete ? ClosingTextExtended(oldClosing) : "Closing نامکمل";

        foreach (var pair in values)
            _day.Closing.Set(pair.Key, AccountingEngine.Format(pair.Value));
        _day.Closing.CheckedAt = DateTime.Now.ToString("hh:mm:ss tt", CultureInfo.InvariantCulture);
        var newSummary = ClosingTextExtended(_day.Closing);

        if (oldWasComplete && oldSummary != newSummary)
            _day.Audit.Add(AccountingEngine.Audit("closing_edit", "Closing میں درستی / ترمیم", oldSummary, newSummary));
        else if (!oldWasComplete)
            _day.Audit.Add(AccountingEngine.Audit("closing_save", "Closing پہلی بار محفوظ", "", newSummary));

        _store.Save(_day);
        var syncNote = _store.SyncNextOpeningAfterClosingChange(
            _selectedDate, oldClosing, oldBook, oldWasComplete, _day.Closing);
        RefreshAll();
        RefreshExtendedAccountUi();
        ClosingMessage.Text = string.IsNullOrWhiteSpace(syncNote) ? "Closing محفوظ ہوگئی۔" : syncNote;
        StatusText.Text = ClosingMessage.Text;
    }

    private static string ClosingTextExtended(ClosingData c)
        => $"گلہ {c.Drawer} | کمپیوٹر {c.Computer} | جیز {c.Jazz} | ایزی شفاقت {c.Easy} | ایزی تنظم {c.EasyTanzeem} | ون لوڈ {c.Oneload}";
}
