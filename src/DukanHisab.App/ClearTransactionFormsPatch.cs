using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DukanHisab.Core;

namespace DukanHisab.App;

public partial class MainWindow
{
    private bool _clearTransactionFormsBuilt;

    private TextBox? _receiveAmount;
    private ComboBox? _receiveCounter;
    private TextBox? _receiveCommission;
    private TextBox? _receiveNote;

    private TextBox? _sendAmount;
    private ComboBox? _sendCounter;
    private TextBox? _sendCommission;
    private TextBox? _sendNote;

    private TextBox? _tanzeemToAmount;
    private ComboBox? _tanzeemToAccount;
    private TextBox? _tanzeemToCommission;
    private TextBox? _tanzeemToNote;

    private TextBox? _tanzeemFromAmount;
    private ComboBox? _tanzeemFromAccount;
    private TextBox? _tanzeemFromCommission;
    private TextBox? _tanzeemFromNote;

    private static readonly FontFamily UrduFormFont = new("Jameel Noori Nastaleeq");
    private static readonly FontFamily NumberFormFont = new("Segoe UI");

    internal void BuildClearTransactionForms()
    {
        if (_clearTransactionFormsBuilt) return;
        _clearTransactionFormsBuilt = true;

        HideLegacyEditor(AccountDirection);
        HideLegacyEditor(TanzeemType);

        var accountRows = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
        accountRows.Children.Add(BuildAccountDirectionRow("وصول کیے", "اکاؤنٹ میں رقم وصول ہوئی", "in", true));
        accountRows.Children.Add(BuildAccountDirectionRow("سینڈ کیے", "اکاؤنٹ سے رقم سینڈ کی", "out", false));
        Grid.SetRow(accountRows, 1);
        AccountPage.Children.Add(accountRows);

        var tanzeemRows = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
        tanzeemRows.Children.Add(BuildTanzeemDirectionRow("دکان سے تنظم کے پاس گئے", "in", true));
        tanzeemRows.Children.Add(BuildTanzeemDirectionRow("تنظم سے دکان میں آئے", "out", false));
        Grid.SetRow(tanzeemRows, 1);
        TanzeemPage.Children.Add(tanzeemRows);

        PopulateClearAccountCounters();
        PopulateClearTanzeemAccounts();

        // Existing double-click handlers still preserve all edit/audit behaviour.
        // These additional handlers only move the selected values into the new visible row.
        AccountGrid.MouseDoubleClick += ClearAccountGrid_MouseDoubleClick;
        TanzeemGrid.MouseDoubleClick += ClearTanzeemGrid_MouseDoubleClick;
        DayPicker.SelectedDateChanged += (_, _) =>
        {
            _editTransactionId = null;
            ClearVisibleAccountFields();
            ClearVisibleTanzeemFields();
        };

        PolishDirectionLabels();
    }

    internal void PrepareClearAccountForms()
    {
        if (!_clearTransactionFormsBuilt) return;
        _editTransactionId = null;
        ClearVisibleAccountFields();
        PopulateClearAccountCounters();
    }

    internal void PrepareClearTanzeemForms()
    {
        if (!_clearTransactionFormsBuilt) return;
        _editTransactionId = null;
        ClearVisibleTanzeemFields();
        PopulateClearTanzeemAccounts();
    }

    internal void PolishDirectionLabels()
    {
        if (AccountGrid.ItemsSource is IEnumerable<TxView> accountItems)
        {
            var changed = false;
            foreach (var item in accountItems)
            {
                if (item.Direction == "اِن") { item.Direction = "وصول کیے"; changed = true; }
                else if (item.Direction == "آؤٹ") { item.Direction = "سینڈ کیے"; changed = true; }
            }
            if (changed) AccountGrid.Items.Refresh();
        }

        if (TanzeemGrid.ItemsSource is IEnumerable<TxView> tanzeemItems)
        {
            var changed = false;
            foreach (var item in tanzeemItems)
            {
                if (item.Direction == "اِن") { item.Direction = "دکان سے تنظم کے پاس گئے"; changed = true; }
                else if (item.Direction == "آؤٹ") { item.Direction = "تنظم سے دکان میں آئے"; changed = true; }
            }
            if (changed) TanzeemGrid.Items.Refresh();
        }
    }

    private Border BuildAccountDirectionRow(string title, string subtitle, string direction, bool receive)
    {
        var amount = NewNumberBox();
        var counter = NewUrduCombo();
        var commission = NewNumberBox();
        var note = NewUrduTextBox();

        if (receive)
        {
            _receiveAmount = amount; _receiveCounter = counter; _receiveCommission = commission; _receiveNote = note;
        }
        else
        {
            _sendAmount = amount; _sendCounter = counter; _sendCommission = commission; _sendNote = note;
        }

        var border = NewSoftRow(receive ? Color.FromRgb(238, 248, 243) : Color.FromRgb(255, 245, 238));
        var grid = NewFormGrid();
        border.Child = grid;

        AddAt(grid, NewHeading(title, subtitle), 0);
        AddAt(grid, NewField("رقم", amount), 1);
        AddAt(grid, NewField("دوسری طرف", counter), 2);
        AddAt(grid, NewField("کمیشن (خالی چھوڑیں تو خودکار)", commission), 3);
        AddAt(grid, NewField("نوٹ", note), 4);

        var buttons = NewButtonPanel();
        var save = NewActionButton(receive ? "وصول محفوظ کریں" : "سینڈ محفوظ کریں", Color.FromRgb(10, 122, 91));
        var delete = NewActionButton("منتخب حذف", Color.FromRgb(165, 63, 63));
        save.Click += (_, _) => SaveVisibleAccountTransaction(direction, amount, counter, commission, note);
        delete.Click += (_, _) => DeleteByView(AccountGrid.SelectedItem as TxView);
        buttons.Children.Add(save);
        buttons.Children.Add(delete);
        AddAt(grid, buttons, 5);

        return border;
    }

    private Border BuildTanzeemDirectionRow(string title, string type, bool toTanzeem)
    {
        var amount = NewNumberBox();
        var account = NewUrduCombo();
        var commission = NewNumberBox();
        var note = NewUrduTextBox();

        if (toTanzeem)
        {
            _tanzeemToAmount = amount; _tanzeemToAccount = account; _tanzeemToCommission = commission; _tanzeemToNote = note;
        }
        else
        {
            _tanzeemFromAmount = amount; _tanzeemFromAccount = account; _tanzeemFromCommission = commission; _tanzeemFromNote = note;
        }

        var border = NewSoftRow(toTanzeem ? Color.FromRgb(238, 248, 243) : Color.FromRgb(255, 245, 238));
        var grid = NewFormGrid();
        border.Child = grid;

        AddAt(grid, NewHeading(title, toTanzeem ? "دکان کی رقم تنظم کو دی" : "تنظم سے رقم دکان میں آئی"), 0);
        AddAt(grid, NewField("رقم", amount), 1);
        AddAt(grid, NewField("دکان کا اکاؤنٹ", account), 2);
        AddAt(grid, NewField("کمیشن (خالی چھوڑیں تو خودکار)", commission), 3);
        AddAt(grid, NewField("نوٹ", note), 4);

        var buttons = NewButtonPanel();
        var save = NewActionButton("محفوظ کریں", Color.FromRgb(10, 122, 91));
        var delete = NewActionButton("منتخب حذف", Color.FromRgb(165, 63, 63));
        save.Click += (_, _) => SaveVisibleTanzeemTransaction(type, amount, account, commission, note);
        delete.Click += (_, _) => DeleteByView(TanzeemGrid.SelectedItem as TxView);
        buttons.Children.Add(save);
        buttons.Children.Add(delete);
        AddAt(grid, buttons, 5);

        return border;
    }

    private void SaveVisibleAccountTransaction(string direction, TextBox amountBox, ComboBox counterBox, TextBox commissionBox, TextBox noteBox)
    {
        if (!TryGetPositive(amountBox.Text, out var amount, "رقم")) return;
        var counter = SelectedTag(counterBox);
        if (string.IsNullOrEmpty(counter)) { StatusText.Text = "دوسری طرف کا اکاؤنٹ منتخب کریں۔"; return; }
        if (!TryCommission(commissionBox.Text, out var commission)) return;

        var old = _editTransactionId is null ? null : _day.Transactions.FirstOrDefault(x => x.Id == _editTransactionId);
        var tx = AccountingEngine.BuildTransaction(_currentAccount, direction, amount, counter, noteBox.Text, commission, old?.Id, old?.Time);
        UpsertTransaction(tx, old);

        _editTransactionId = null;
        ClearVisibleAccountFields();
        StatusText.Text = direction == "in" ? "وصول کی انٹری محفوظ ہوگئی۔" : "سینڈ کی انٹری محفوظ ہوگئی۔";
        PolishDirectionLabels();
    }

    private void SaveVisibleTanzeemTransaction(string type, TextBox amountBox, ComboBox accountBox, TextBox commissionBox, TextBox noteBox)
    {
        if (!TryGetPositive(amountBox.Text, out var amount, "رقم")) return;
        var account = SelectedTag(accountBox);
        if (string.IsNullOrEmpty(account)) { StatusText.Text = "دکان کا اکاؤنٹ منتخب کریں۔"; return; }
        if (!TryCommission(commissionBox.Text, out var commission)) return;

        var old = _editTransactionId is null ? null : _day.Transactions.FirstOrDefault(x => x.Id == _editTransactionId);
        var tx = AccountingEngine.BuildTanzeem(type, amount, account, noteBox.Text, commission, old?.Id, old?.Time);
        UpsertTransaction(tx, old);

        _editTransactionId = null;
        ClearVisibleTanzeemFields();
        StatusText.Text = type == "in" ? "دکان سے تنظم کے پاس گئی رقم محفوظ ہوگئی۔" : "تنظم سے دکان میں آئی رقم محفوظ ہوگئی۔";
        PolishDirectionLabels();
    }

    private void ClearAccountGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (AccountGrid.SelectedItem is not TxView view) return;
        var tx = _day.Transactions.FirstOrDefault(x => x.Id == view.Id);
        if (tx is null) return;

        _editTransactionId = tx.Id;
        var incoming = tx.Direction == "in";
        var amount = incoming ? _receiveAmount : _sendAmount;
        var counter = incoming ? _receiveCounter : _sendCounter;
        var commission = incoming ? _receiveCommission : _sendCommission;
        var note = incoming ? _receiveNote : _sendNote;
        if (amount is null || counter is null || commission is null || note is null) return;

        amount.Text = AccountingEngine.Format(tx.Amount);
        SelectTag(counter, tx.Counter);
        commission.Text = tx.CommissionMode == "override" ? AccountingEngine.Format(AccountingEngine.Money(tx.CommissionOverride)) : "";
        note.Text = tx.Note;
        StatusText.Text = incoming ? "ترمیم موڈ: وصول والی قطار میں تبدیلی کریں۔" : "ترمیم موڈ: سینڈ والی قطار میں تبدیلی کریں۔";
    }

    private void ClearTanzeemGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (TanzeemGrid.SelectedItem is not TxView view) return;
        var tx = _day.Transactions.FirstOrDefault(x => x.Id == view.Id);
        if (tx is null) return;

        _editTransactionId = tx.Id;
        var toTanzeem = tx.TanzeemType == "in";
        var amount = toTanzeem ? _tanzeemToAmount : _tanzeemFromAmount;
        var account = toTanzeem ? _tanzeemToAccount : _tanzeemFromAccount;
        var commission = toTanzeem ? _tanzeemToCommission : _tanzeemFromCommission;
        var note = toTanzeem ? _tanzeemToNote : _tanzeemFromNote;
        if (amount is null || account is null || commission is null || note is null) return;

        amount.Text = AccountingEngine.Format(tx.Amount);
        SelectTag(account, tx.Primary);
        commission.Text = tx.CommissionMode == "override" ? AccountingEngine.Format(AccountingEngine.Money(tx.CommissionOverride)) : "";
        note.Text = tx.Note;
        StatusText.Text = toTanzeem ? "ترمیم موڈ: دکان سے تنظم والی قطار میں تبدیلی کریں۔" : "ترمیم موڈ: تنظم سے دکان والی قطار میں تبدیلی کریں۔";
    }

    private void PopulateClearAccountCounters()
    {
        if (_receiveCounter is null || _sendCounter is null) return;
        var choices = _currentAccount == Accounts.Jazz
            ? new[] { Accounts.Drawer, Accounts.Easy, Accounts.OneLoad, Accounts.Tanzeem, Accounts.External }
            : new[] { Accounts.Drawer, Accounts.Jazz, Accounts.OneLoad, Accounts.Tanzeem, Accounts.External };
        FillCounterCombo(_receiveCounter, choices);
        FillCounterCombo(_sendCounter, choices);
        _receiveCounter.SelectedIndex = 0;
        _sendCounter.SelectedIndex = 0;
    }

    private void PopulateClearTanzeemAccounts()
    {
        if (_tanzeemToAccount is null || _tanzeemFromAccount is null) return;
        var choices = new[] { Accounts.Drawer, Accounts.Jazz, Accounts.Easy, Accounts.OneLoad };
        FillCounterCombo(_tanzeemToAccount, choices);
        FillCounterCombo(_tanzeemFromAccount, choices);
        _tanzeemToAccount.SelectedIndex = 0;
        _tanzeemFromAccount.SelectedIndex = 0;
    }

    private void ClearVisibleAccountFields()
    {
        foreach (var box in new[] { _receiveAmount, _receiveCommission, _receiveNote, _sendAmount, _sendCommission, _sendNote })
            if (box is not null) box.Text = "";
        if (_receiveCounter is not null && _receiveCounter.Items.Count > 0) _receiveCounter.SelectedIndex = 0;
        if (_sendCounter is not null && _sendCounter.Items.Count > 0) _sendCounter.SelectedIndex = 0;
    }

    private void ClearVisibleTanzeemFields()
    {
        foreach (var box in new[] { _tanzeemToAmount, _tanzeemToCommission, _tanzeemToNote, _tanzeemFromAmount, _tanzeemFromCommission, _tanzeemFromNote })
            if (box is not null) box.Text = "";
        if (_tanzeemToAccount is not null && _tanzeemToAccount.Items.Count > 0) _tanzeemToAccount.SelectedIndex = 0;
        if (_tanzeemFromAccount is not null && _tanzeemFromAccount.Items.Count > 0) _tanzeemFromAccount.SelectedIndex = 0;
    }

    private static void HideLegacyEditor(FrameworkElement control)
    {
        if (control.Parent is FrameworkElement first && first.Parent is Grid grid && grid.Parent is Border border)
            border.Visibility = Visibility.Collapsed;
    }

    private Border NewSoftRow(Color background)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(background),
            BorderBrush = new SolidColorBrush(Color.FromRgb(225, 231, 228)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(6, 4, 6, 4)
        };
        return border;
    }

    private static Grid NewFormGrid()
    {
        var grid = new Grid { VerticalAlignment = VerticalAlignment.Center };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(155) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.7, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        return grid;
    }

    private static StackPanel NewHeading(string title, string subtitle)
    {
        var panel = new StackPanel { Margin = new Thickness(4, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontFamily = UrduFormFont,
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = subtitle,
            FontFamily = UrduFormFont,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(85, 112, 103)),
            TextWrapping = TextWrapping.Wrap
        });
        return panel;
    }

    private static StackPanel NewField(string label, FrameworkElement input)
    {
        var panel = new StackPanel { Margin = new Thickness(3, 0, 3, 0) };
        panel.Children.Add(new TextBlock
        {
            Text = label,
            FontFamily = UrduFormFont,
            FontSize = 15,
            Margin = new Thickness(2, 0, 2, 2)
        });
        panel.Children.Add(input);
        return panel;
    }

    private static TextBox NewNumberBox() => new()
    {
        FontFamily = NumberFormFont,
        FontSize = 16,
        MinHeight = 38,
        Padding = new Thickness(7, 5, 7, 5),
        Margin = new Thickness(2)
    };

    private static TextBox NewUrduTextBox() => new()
    {
        FontFamily = UrduFormFont,
        FontSize = 16,
        MinHeight = 38,
        Padding = new Thickness(7, 4, 7, 4),
        Margin = new Thickness(2)
    };

    private static ComboBox NewUrduCombo() => new()
    {
        FontFamily = UrduFormFont,
        FontSize = 16,
        MinHeight = 38,
        Margin = new Thickness(2),
        Padding = new Thickness(5, 3, 5, 3)
    };

    private static StackPanel NewButtonPanel() => new()
    {
        Orientation = Orientation.Horizontal,
        VerticalAlignment = VerticalAlignment.Bottom,
        Margin = new Thickness(4, 0, 0, 0)
    };

    private static Button NewActionButton(string text, Color background) => new()
    {
        Content = text,
        FontFamily = UrduFormFont,
        FontSize = 16,
        MinHeight = 38,
        Padding = new Thickness(10, 5, 10, 5),
        Margin = new Thickness(3, 2, 3, 2),
        Background = new SolidColorBrush(background),
        Foreground = Brushes.White,
        BorderThickness = new Thickness(0),
        Cursor = System.Windows.Input.Cursors.Hand
    };

    private static void AddAt(Grid grid, UIElement element, int column)
    {
        Grid.SetColumn(element, column);
        grid.Children.Add(element);
    }
}
