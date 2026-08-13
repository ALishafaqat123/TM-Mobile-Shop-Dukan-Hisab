using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DukanHisab.Core;
using DukanHisab.Storage;

namespace DukanHisab.App;

public partial class MainWindow : Window
{
    private readonly DayStore _store = new();
    private DayRecord _day = new();
    private DateTime _selectedDate = DateTime.Today;
    private string _currentAccount = Accounts.Jazz;
    private string? _editTransactionId;
    private bool _loading;

    private static readonly AccountBalances Baseline = new() { Drawer = 31700m, Computer = 2000m, Easy = 35607m, Jazz = 91268m, Oneload = 9953m };
    private static readonly DateTime BaselineDate = new(2026, 8, 10);

    public MainWindow()
    {
        InitializeComponent();
        AccountGrid.MouseDoubleClick += AccountGrid_MouseDoubleClick;
        TanzeemGrid.MouseDoubleClick += TanzeemGrid_MouseDoubleClick;
        AccGrid.MouseDoubleClick += AccGrid_MouseDoubleClick;
        PopulateCombos();
        var imported = _store.ImportAllLegacyCopies();
        if (imported > 0) StatusText.Text = $"v1.20 سے {imported} دن read-only copy کے طور پر import ہوئے۔";
        LoadDay(DateTime.Today);
        ShowPage(DashboardPage, "ڈیش بورڈ", NavDashboard);
    }

    private void PopulateCombos()
    {
        AccountDirection.SelectedIndex = 0; TanzeemType.SelectedIndex = 0; AccType.SelectedIndex = 0;
        FillCounterCombo(AccountCounter, [Accounts.Drawer, Accounts.Easy, Accounts.Jazz, Accounts.OneLoad, Accounts.Tanzeem, Accounts.External]);
        FillCounterCombo(TanzeemAccount, [Accounts.Drawer, Accounts.Jazz, Accounts.Easy, Accounts.OneLoad]);
        AccountCounter.SelectedIndex = 0; TanzeemAccount.SelectedIndex = 0;
    }

    private static void FillCounterCombo(ComboBox combo, IEnumerable<string> accounts)
    {
        combo.Items.Clear();
        foreach (var account in accounts) combo.Items.Add(new ComboBoxItem { Content = Accounts.UrduName(account), Tag = account });
    }

    private void LoadDay(DateTime date)
    {
        if (_loading) return;
        _loading = true;
        try
        {
            _selectedDate = date.Date;
            var loaded = _store.Load(_selectedDate);
            if (loaded is null)
            {
                AccountBalances opening;
                var previousDate = _store.FindPreviousDate(_selectedDate);
                if (previousDate is not null)
                {
                    var previous = _store.Load(previousDate.Value);
                    opening = previous is null ? Baseline.Clone() : AccountingEngine.OpeningFromPrevious(previous, true);
                }
                else opening = Baseline.Clone();
                loaded = _store.CreateNew(_selectedDate, opening);
            }
            _day = loaded;
            if (_store.RepairOpeningFromPreviousIfNeeded(_selectedDate, _day, out var repairNote) && !string.IsNullOrWhiteSpace(repairNote)) StatusText.Text = repairNote;
            AccountingEngine.Recalculate(_day);
            DayPicker.SelectedDate = _selectedDate;
            ClearEditState();
            RefreshAll();
        }
        finally { _loading = false; }
    }

    private void RefreshAll()
    {
        AccountingEngine.Recalculate(_day);
        RefreshDashboard(); RefreshAccountPage(); RefreshTanzeem(); RefreshAccessories(); RefreshClosing(); RefreshHistory();
    }

    private void RefreshDashboard()
    {
        var c = AccountingEngine.CurrentBalances(_day); var tz = AccountingEngine.TanzeemTotals(_day); var live = AccountingEngine.LiveProfit(_day);
        var src = AccountingEngine.ProfitSourceSummary(_day, false); var closing = AccountingEngine.GetClosingSummary(_day);
        DashDrawer.Text = MoneyText(c.Drawer); DashJazz.Text = MoneyText(c.Jazz); DashEasy.Text = MoneyText(c.Easy); DashOneLoad.Text = MoneyText(c.Oneload); DashComputer.Text = MoneyText(c.Computer);
        DashCurrentTotal.Text = MoneyText(c.Total); DashLiveProfit.Text = MoneyText(live); DashClosingProfit.Text = closing.Complete ? MoneyText(closing.ActualProfit ?? 0m) : "—";
        DashTIn.Text = $"{MoneyText(tz.In)} ({tz.InCount})"; DashTOut.Text = $"{MoneyText(tz.Out)} ({tz.OutCount})";
        DashJazzCommission.Text = MoneyText(src.Jazz); DashEasyCommission.Text = MoneyText(src.Easy); DashTanzeemCommission.Text = MoneyText(src.Tanzeem); DashAccessories.Text = MoneyText(src.Accessories); DashUnexplained.Text = MoneyText(src.Unexplained);
        RecentGrid.ItemsSource = _day.Transactions.AsEnumerable().Reverse().Take(12).Select(ToView).ToList();
    }

    private void RefreshAccountPage()
    {
        if (AccountPage.Visibility != Visibility.Visible) return;
        var c = AccountingEngine.CurrentBalances(_day);
        AccountOpening.Text = MoneyText(_day.Opening.Get(_currentAccount)); AccountCurrent.Text = MoneyText(c.Get(_currentAccount));
        var list = _day.Transactions.Where(t => t.Primary == _currentAccount).Reverse().Select(ToView).ToList(); AccountCount.Text = list.Count.ToString(CultureInfo.InvariantCulture); AccountGrid.ItemsSource = list;
    }

    private void RefreshTanzeem()
    {
        var tz = AccountingEngine.TanzeemTotals(_day); TInTotal.Text = $"{MoneyText(tz.In)} ({tz.InCount} دفعہ)"; TOutTotal.Text = $"{MoneyText(tz.Out)} ({tz.OutCount} دفعہ)";
        TanzeemGrid.ItemsSource = _day.Transactions.Where(t => t.TanzeemType is "in" or "out").Reverse().Select(ToView).ToList();
    }

    private void RefreshAccessories()
    {
        decimal sales = 0, returns = 0; var txs = _day.Transactions.Where(t => t.Category == "accessory").ToList();
        foreach (var t in txs) { if (t.AccessoryType == "return") returns += t.Amount; else sales += t.Amount; }
        AccSales.Text = MoneyText(sales); AccReturns.Text = MoneyText(returns); AccNet.Text = MoneyText(sales - returns); AccGrid.ItemsSource = txs.AsEnumerable().Reverse().Select(ToView).ToList();
    }

    private void RefreshClosing()
    {
        var book = AccountingEngine.CurrentBalances(_day); var summary = AccountingEngine.GetClosingSummary(_day); var tz = AccountingEngine.TanzeemTotals(_day);
        CloseOpenComputer.Text = MoneyText(_day.Opening.Computer); CloseBookComputer.Text = MoneyText(book.Computer); CloseOpenDrawer.Text = MoneyText(_day.Opening.Drawer); CloseBookDrawer.Text = MoneyText(book.Drawer);
        CloseOpenJazz.Text = MoneyText(_day.Opening.Jazz); CloseBookJazz.Text = MoneyText(book.Jazz); CloseOpenEasy.Text = MoneyText(_day.Opening.Easy); CloseBookEasy.Text = MoneyText(book.Easy); CloseOpenOneLoad.Text = MoneyText(_day.Opening.Oneload); CloseBookOneLoad.Text = MoneyText(book.Oneload);
        CloseOpeningTotal.Text = MoneyText(_day.Opening.Total); CloseTIn.Text = MoneyText(tz.In); CloseTOut.Text = MoneyText(tz.Out); CloseActualTotal.Text = summary.Complete ? MoneyText(summary.ActualTotal) : "—"; CloseProfit.Text = summary.Complete ? MoneyText(summary.ActualProfit ?? 0m) : "—"; AuditGrid.ItemsSource = _day.Audit.AsEnumerable().Reverse().ToList();
        if (!CloseDrawer.IsKeyboardFocusWithin && !CloseComputer.IsKeyboardFocusWithin && !CloseEasy.IsKeyboardFocusWithin && !CloseJazz.IsKeyboardFocusWithin && !CloseOneLoad.IsKeyboardFocusWithin)
        { CloseDrawer.Text = _day.Closing.Drawer; CloseComputer.Text = _day.Closing.Computer; CloseEasy.Text = _day.Closing.Easy; CloseJazz.Text = _day.Closing.Jazz; CloseOneLoad.Text = _day.Closing.Oneload; }
    }

    private void RefreshHistory()
    {
        var selected = HistoryDates.SelectedItem?.ToString(); HistoryDates.ItemsSource = _store.ListDates().Select(d => d.ToString("dd-MM-yyyy")).ToList(); if (selected is not null) HistoryDates.SelectedItem = selected;
    }

    private void ShowPage(FrameworkElement page, string title, Button nav)
    {
        DashboardPage.Visibility = Visibility.Collapsed; AccountPage.Visibility = Visibility.Collapsed; TanzeemPage.Visibility = Visibility.Collapsed; AccessoriesPage.Visibility = Visibility.Collapsed; ClosingPage.Visibility = Visibility.Collapsed; HistoryPage.Visibility = Visibility.Collapsed;
        page.Visibility = Visibility.Visible; PageTitle.Text = title;
        foreach (var b in new[] { NavDashboard, NavJazz, NavEasy, NavTanzeem, NavAccessories, NavClosing, NavHistory }) { b.Background = Brushes.Transparent; b.Foreground = Brushes.White; }
        nav.Background = new SolidColorBrush(Color.FromRgb(221, 170, 52)); nav.Foreground = Brushes.Black; RefreshAll();
    }

    private void NavDashboard_Click(object sender, RoutedEventArgs e) => ShowPage(DashboardPage, "ڈیش بورڈ", NavDashboard);
    private void NavJazz_Click(object sender, RoutedEventArgs e) { _currentAccount = Accounts.Jazz; FillAccountCounters(); ShowPage(AccountPage, "جیزکیش ٹی ایم موبائل", NavJazz); }
    private void NavEasy_Click(object sender, RoutedEventArgs e) { _currentAccount = Accounts.Easy; FillAccountCounters(); ShowPage(AccountPage, "ایزی پیسہ شفاقت", NavEasy); }
    private void NavTanzeem_Click(object sender, RoutedEventArgs e) => ShowPage(TanzeemPage, "تنظیم اِن / آؤٹ", NavTanzeem);
    private void NavAccessories_Click(object sender, RoutedEventArgs e) => ShowPage(AccessoriesPage, "موبائل اسیسریز", NavAccessories);
    private void NavClosing_Click(object sender, RoutedEventArgs e) => ShowPage(ClosingPage, "دن کا آخری حساب / کل کاروباری رقم", NavClosing);
    private void NavHistory_Click(object sender, RoutedEventArgs e) => ShowPage(HistoryPage, "روزانہ ہسٹری", NavHistory);

    private void FillAccountCounters()
    {
        var choices = _currentAccount == Accounts.Jazz ? new[] { Accounts.Drawer, Accounts.Easy, Accounts.OneLoad, Accounts.Tanzeem, Accounts.External } : new[] { Accounts.Drawer, Accounts.Jazz, Accounts.OneLoad, Accounts.Tanzeem, Accounts.External };
        FillCounterCombo(AccountCounter, choices); AccountCounter.SelectedIndex = 0;
    }

    private void DayPicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e) { if (_loading || DayPicker.SelectedDate is null) return; LoadDay(DayPicker.SelectedDate.Value); }
    private void Today_Click(object sender, RoutedEventArgs e) => LoadDay(DateTime.Today);
    private void ImportLegacy_Click(object sender, RoutedEventArgs e) { var imported = _store.ImportAllLegacyCopies(); StatusText.Text = imported > 0 ? $"v1.20 سے {imported} دن کی copy import ہوگئی۔ اصل v1.20 data نہیں بدلا۔" : "کوئی نئی v1.20 فائل import کے لیے باقی نہیں تھی۔"; LoadDay(_selectedDate); }

    private void SaveAccountTransaction_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetPositive(AccountAmount.Text, out var amount, "رقم")) return; var direction = SelectedTag(AccountDirection); var counter = SelectedTag(AccountCounter); if (string.IsNullOrEmpty(direction) || string.IsNullOrEmpty(counter)) return; if (!TryCommission(AccountCommission.Text, out var commission)) return;
        var old = _editTransactionId is null ? null : _day.Transactions.FirstOrDefault(x => x.Id == _editTransactionId);
        var tx = AccountingEngine.BuildTransaction(_currentAccount, direction, amount, counter, AccountNote.Text, commission, old?.Id, old?.Time); UpsertTransaction(tx, old);
        AccountAmount.Text = AccountCommission.Text = AccountNote.Text = ""; _editTransactionId = null; StatusText.Text = "انٹری محفوظ ہوگئی۔";
    }

    private void SaveTanzeem_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetPositive(TanzeemAmount.Text, out var amount, "رقم")) return; var type = SelectedTag(TanzeemType); var account = SelectedTag(TanzeemAccount); if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(account)) return; if (!TryCommission(TanzeemCommission.Text, out var commission)) return;
        var old = _editTransactionId is null ? null : _day.Transactions.FirstOrDefault(x => x.Id == _editTransactionId); var tx = AccountingEngine.BuildTanzeem(type, amount, account, TanzeemNote.Text, commission, old?.Id, old?.Time); UpsertTransaction(tx, old);
        TanzeemAmount.Text = TanzeemCommission.Text = TanzeemNote.Text = ""; _editTransactionId = null; StatusText.Text = "تنظیم انٹری محفوظ ہوگئی۔";
    }

    private void SaveAccessory_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetPositive(AccAmount.Text, out var amount, "رقم")) return; var type = SelectedTag(AccType); var old = _editTransactionId is null ? null : _day.Transactions.FirstOrDefault(x => x.Id == _editTransactionId); var tx = AccountingEngine.BuildAccessory(amount, type == "return", AccNote.Text, old?.Id, old?.Time); UpsertTransaction(tx, old);
        AccAmount.Text = AccNote.Text = ""; _editTransactionId = null; StatusText.Text = "اسیسریز انٹری محفوظ ہوگئی۔";
    }

    private void UpsertTransaction(TransactionRecord tx, TransactionRecord? old)
    {
        var wasClosed = AccountingEngine.ClosingComplete(_day.Closing);
        if (old is null) { _day.Transactions.Add(tx); if (wasClosed) _day.Audit.Add(AccountingEngine.Audit("late_entry", "Closing کے بعد نئی انٹری شامل کی گئی", "", AccountingEngine.TransactionShort(tx))); }
        else { var index = _day.Transactions.FindIndex(x => x.Id == old.Id); if (index >= 0) _day.Transactions[index] = tx; _day.Audit.Add(AccountingEngine.Audit("transaction_edit", "انٹری میں ترمیم", AccountingEngine.TransactionShort(old), AccountingEngine.TransactionShort(tx))); }
        _store.Save(_day); RefreshAll();
    }

    private void DeleteSelectedTransaction_Click(object sender, RoutedEventArgs e) => DeleteByView(AccountGrid.SelectedItem as TxView);
    private void DeleteSelectedTanzeem_Click(object sender, RoutedEventArgs e) => DeleteByView(TanzeemGrid.SelectedItem as TxView);
    private void DeleteSelectedAccessory_Click(object sender, RoutedEventArgs e) => DeleteByView(AccGrid.SelectedItem as TxView);

    private void DeleteByView(TxView? view)
    {
        if (view is null) { StatusText.Text = "حذف کرنے کے لیے انٹری منتخب کریں۔"; return; } var old = _day.Transactions.FirstOrDefault(x => x.Id == view.Id); if (old is null) return;
        if (MessageBox.Show("منتخب انٹری حذف کریں؟", "دکان حساب", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _day.Transactions.Remove(old); _day.Audit.Add(AccountingEngine.Audit("transaction_delete", "انٹری حذف کی گئی", AccountingEngine.TransactionShort(old), "حذف")); _store.Save(_day); RefreshAll(); StatusText.Text = "انٹری حذف ہوگئی؛ audit history محفوظ ہے۔";
    }

    private void AccountGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (AccountGrid.SelectedItem is not TxView v) return; var tx = _day.Transactions.FirstOrDefault(x => x.Id == v.Id); if (tx is null) return; _editTransactionId = tx.Id; SelectTag(AccountDirection, tx.Direction); AccountAmount.Text = AccountingEngine.Format(tx.Amount); SelectTag(AccountCounter, tx.Counter); AccountCommission.Text = tx.CommissionMode == "override" ? AccountingEngine.Format(AccountingEngine.Money(tx.CommissionOverride)) : ""; AccountNote.Text = tx.Note; StatusText.Text = "ترمیم موڈ: تبدیلی کے بعد محفوظ کریں۔";
    }
    private void TanzeemGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (TanzeemGrid.SelectedItem is not TxView v) return; var tx = _day.Transactions.FirstOrDefault(x => x.Id == v.Id); if (tx is null) return; _editTransactionId = tx.Id; SelectTag(TanzeemType, tx.TanzeemType); TanzeemAmount.Text = AccountingEngine.Format(tx.Amount); SelectTag(TanzeemAccount, tx.Primary); TanzeemCommission.Text = tx.CommissionMode == "override" ? AccountingEngine.Format(AccountingEngine.Money(tx.CommissionOverride)) : ""; TanzeemNote.Text = tx.Note; StatusText.Text = "ترمیم موڈ: تبدیلی کے بعد محفوظ کریں۔";
    }
    private void AccGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (AccGrid.SelectedItem is not TxView v) return; var tx = _day.Transactions.FirstOrDefault(x => x.Id == v.Id); if (tx is null) return; _editTransactionId = tx.Id; SelectTag(AccType, tx.AccessoryType == "return" ? "return" : "sale"); AccAmount.Text = AccountingEngine.Format(tx.Amount); AccNote.Text = tx.Note; StatusText.Text = "ترمیم موڈ: تبدیلی کے بعد محفوظ کریں۔";
    }

    private void SaveClosing_Click(object sender, RoutedEventArgs e)
    {
        var inputs = new Dictionary<string, TextBox> { [Accounts.Drawer] = CloseDrawer, [Accounts.Computer] = CloseComputer, [Accounts.Easy] = CloseEasy, [Accounts.Jazz] = CloseJazz, [Accounts.OneLoad] = CloseOneLoad };
        var values = new Dictionary<string, decimal>();
        foreach (var pair in inputs) { if (!AccountingEngine.TryMoney(pair.Value.Text, out var v) || v < 0) { ClosingMessage.Text = $"{Accounts.UrduName(pair.Key)} کی اصل رقم درست لکھیں۔"; pair.Value.Focus(); return; } values[pair.Key] = v; }
        var oldClosing = _day.Closing.Clone(); var oldBook = AccountingEngine.CurrentBalances(_day); var oldWasComplete = AccountingEngine.ClosingComplete(oldClosing); var oldSummary = oldWasComplete ? ClosingText(oldClosing) : "Closing نامکمل";
        foreach (var pair in values) _day.Closing.Set(pair.Key, AccountingEngine.Format(pair.Value)); _day.Closing.CheckedAt = DateTime.Now.ToString("hh:mm:ss tt", CultureInfo.InvariantCulture); var newSummary = ClosingText(_day.Closing);
        if (oldWasComplete && oldSummary != newSummary) _day.Audit.Add(AccountingEngine.Audit("closing_edit", "Closing میں درستی / ترمیم", oldSummary, newSummary)); else if (!oldWasComplete) _day.Audit.Add(AccountingEngine.Audit("closing_save", "Closing پہلی بار محفوظ", "", newSummary));
        _store.Save(_day); var syncNote = _store.SyncNextOpeningAfterClosingChange(_selectedDate, oldClosing, oldBook, oldWasComplete, _day.Closing); RefreshAll(); ClosingMessage.Text = string.IsNullOrWhiteSpace(syncNote) ? "Closing محفوظ ہوگئی۔" : syncNote; StatusText.Text = ClosingMessage.Text;
    }

    private static string ClosingText(ClosingData c) => $"گلہ {c.Drawer} | کمپیوٹر {c.Computer} | جیز {c.Jazz} | ایزی {c.Easy} | ون لوڈ {c.Oneload}";

    private void HistoryDates_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HistoryDates.SelectedItem is null) return; if (!DateTime.TryParseExact(HistoryDates.SelectedItem.ToString(), "dd-MM-yyyy", null, DateTimeStyles.None, out var date)) return; var day = _store.Load(date); if (day is null) return;
        AccountingEngine.Recalculate(day); var tz = AccountingEngine.TanzeemTotals(day); var live = AccountingEngine.LiveProfit(day); var closing = AccountingEngine.GetClosingSummary(day); var liveSrc = AccountingEngine.ProfitSourceSummary(day, false); var closeSrc = AccountingEngine.ProfitSourceSummary(day, true);
        HistorySummary.Text = $"تاریخ: {date:dd-MM-yyyy}\n\nOpening Total: {MoneyText(day.Opening.Total)}\nحسابی موجود رقم: {MoneyText(AccountingEngine.CurrentBalances(day).Total)}\nتنظیم اِن: {MoneyText(tz.In)} ({tz.InCount})\nتنظیم آؤٹ: {MoneyText(tz.Out)} ({tz.OutCount})\nLive منافع: {MoneyText(live)}\nLive تقسیم: Jazz {MoneyText(liveSrc.Jazz)} | Easy {MoneyText(liveSrc.Easy)} | Tanzeem {MoneyText(liveSrc.Tanzeem)} | Accessories {MoneyText(liveSrc.Accessories)} | Computer {MoneyText(liveSrc.Computer)} | غیر واضح {MoneyText(liveSrc.Unexplained)}\n\n" + (closing.Complete ? $"Closing Total: {MoneyText(closing.ActualTotal)}\nاصل منافع: {MoneyText(closing.ActualProfit ?? 0m)}\nClosing غیر واضح: {MoneyText(closeSrc.Unexplained)}\n" : "Closing نامکمل ہے۔\n") + $"درستیاں / ترامیم: {day.Audit.Count}\nانٹریز: {day.Transactions.Count}";
    }

    private bool TryGetPositive(string raw, out decimal value, string label) { if (!AccountingEngine.TryMoney(raw, out value) || value <= 0) { StatusText.Text = $"{label} درست عدد میں لکھیں۔"; return false; } return true; }
    private bool TryCommission(string raw, out string normalized) { normalized = ""; if (string.IsNullOrWhiteSpace(raw)) return true; if (!AccountingEngine.TryMoney(raw, out var v) || v < 0) { StatusText.Text = "کمیشن درست لکھیں یا خانہ خالی چھوڑیں۔"; return false; } normalized = AccountingEngine.Format(v); return true; }
    private static string SelectedTag(ComboBox combo) => (combo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
    private static void SelectTag(ComboBox combo, string tag) { foreach (var item in combo.Items.OfType<ComboBoxItem>()) if (item.Tag?.ToString() == tag) { combo.SelectedItem = item; return; } }
    private void ClearEditState() { _editTransactionId = null; AccountAmount.Text = AccountCommission.Text = AccountNote.Text = ""; TanzeemAmount.Text = TanzeemCommission.Text = TanzeemNote.Text = ""; AccAmount.Text = AccNote.Text = ""; }
    private static string MoneyText(decimal value) => AccountingEngine.Format(value) + " روپے";

    private TxView ToView(TransactionRecord tx)
    {
        var isAcc = tx.Category == "accessory"; var cm = AccountingEngine.TransactionCommission(tx);
        return new TxView { Id = tx.Id, Time = tx.Time, Account = isAcc ? "موبائل اسیسریز" : Accounts.UrduName(tx.Primary), Direction = isAcc ? (tx.AccessoryType == "return" ? "واپسی" : "فروخت") : (tx.Direction == "in" ? "اِن" : "آؤٹ"), Amount = AccountingEngine.Format(tx.Amount), Commission = cm > 0 ? AccountingEngine.Format(cm) : "—", Counter = isAcc ? "دوسرے گلے کی نقد" : Accounts.UrduName(tx.Counter), Note = tx.Note };
    }

    private sealed class TxView { public string Id { get; set; } = ""; public string Time { get; set; } = ""; public string Account { get; set; } = ""; public string Direction { get; set; } = ""; public string Amount { get; set; } = ""; public string Commission { get; set; } = ""; public string Counter { get; set; } = ""; public string Note { get; set; } = ""; }
}
