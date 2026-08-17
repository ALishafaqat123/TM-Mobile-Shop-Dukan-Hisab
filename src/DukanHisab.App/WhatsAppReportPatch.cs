using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using DukanHisab.Core;

namespace DukanHisab.App;

internal static class WhatsAppReportBootstrapper
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
        window.Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(window.EnsureWhatsAppReportButton));
    }
}

public partial class MainWindow
{
    private Button? _whatsAppReportButton;
    private static readonly FontFamily WhatsAppUrduFont = new("Jameel Noori Nastaleeq");

    internal void EnsureWhatsAppReportButton()
    {
        if (_whatsAppReportButton is not null) return;
        if (NavHistory.Parent is not StackPanel navPanel) return;

        var button = new Button
        {
            Content = "واٹس ایپ پر مکمل رپورٹ",
            FontFamily = WhatsAppUrduFont,
            FontSize = 17,
            MinHeight = 44,
            HorizontalContentAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(14, 9, 14, 9),
            Margin = new Thickness(0, 3, 0, 3),
            Cursor = System.Windows.Input.Cursors.Hand,
            Style = TryFindResource("SideButton") as Style
        };
        button.Click += WhatsAppReport_Click;

        var historyIndex = navPanel.Children.IndexOf(NavHistory);
        navPanel.Children.Insert(historyIndex >= 0 ? historyIndex + 1 : navPanel.Children.Count, button);
        _whatsAppReportButton = button;
    }

    private void WhatsAppReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var report = BuildWhatsAppDetailedReport();
            Clipboard.SetText(report);

            var encoded = WebUtility.UrlEncode(report);
            var url = "https://web.whatsapp.com/send?text=" + encoded;
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

            StatusText.Text = "مکمل رپورٹ واٹس ایپ ویب میں کھول دی گئی ہے؛ رپورٹ کلپ بورڈ پر بھی کاپی ہے۔";
        }
        catch (Exception ex)
        {
            StatusText.Text = "واٹس ایپ ویب نہیں کھل سکا؛ رپورٹ کلپ بورڈ پر کاپی کرنے کی کوشش کی گئی ہے۔";
            MessageBox.Show(
                "واٹس ایپ ویب کھولنے میں مسئلہ آیا۔\n\n" + ex.Message,
                "دکان حساب",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private string BuildWhatsAppDetailedReport()
    {
        AccountingEngine.Recalculate(_day);
        var current = AccountingEngine.CurrentBalances(_day);
        var closing = AccountingEngine.GetClosingSummary(_day);
        var displayedBalances = closing.Complete && closing.Actual is not null ? closing.Actual : current;
        var profit = AccountingEngine.ProfitSourceSummary(_day, closing.Complete);

        var sb = new StringBuilder();
        sb.AppendLine("*دکان حساب — مکمل روزانہ رپورٹ*");
        sb.AppendLine($"تاریخ: {_selectedDate:dd-MM-yyyy}");
        if (!string.IsNullOrWhiteSpace(_day.ManualReferenceAt))
            sb.AppendLine($"صبح کا دستی ریفرنس محفوظ: {_day.ManualReferenceAt}");
        sb.AppendLine();

        AppendAccountReport(sb, "جیزکیش ٹی ایم موبائل", Accounts.Jazz, displayedBalances.Jazz);
        AppendAccountReport(sb, "ایزی پیسہ شفاقت", Accounts.Easy, displayedBalances.Easy);
        AppendAccountReport(sb, "ایزی پیسہ تنظم", Accounts.EasyTanzeem, displayedBalances.EasyTanzeem);
        AppendTanzeemReport(sb);
        AppendAccessoriesReport(sb);

        sb.AppendLine("*منافع*");
        sb.AppendLine($"کل منافع: {AccountingEngine.Format(profit.Total)} روپے");
        sb.AppendLine($"کمپیوٹر نکال کر: {AccountingEngine.Format(profit.ShopWithoutComputer)} روپے");
        if (!closing.Complete)
            sb.AppendLine("نوٹ: دن کا آخری حساب ابھی محفوظ نہیں، اس لیے منافع حسابی موجودہ رقموں کے مطابق ہے۔");

        return sb.ToString().TrimEnd();
    }

    private void AppendAccountReport(StringBuilder sb, string title, string account, decimal balance)
    {
        sb.AppendLine($"*{title}*");
        sb.AppendLine($"موجودہ رقم: {AccountingEngine.Format(balance)} روپے");

        var incoming = _day.Transactions.Where(t => t.Primary == account && t.Direction == "in").ToList();
        var outgoing = _day.Transactions.Where(t => t.Primary == account && t.Direction == "out").ToList();

        sb.AppendLine("*وصول کیے*");
        AppendAccountTransactionList(sb, incoming);

        sb.AppendLine("*سینڈ کیے*");
        AppendAccountTransactionList(sb, outgoing);
        sb.AppendLine();
    }

    private void AppendTanzeemReport(StringBuilder sb)
    {
        var totals = AccountingEngine.TanzeemTotals(_day);
        var toTanzeem = _day.Transactions.Where(t => t.TanzeemType == "in").ToList();
        var fromTanzeem = _day.Transactions.Where(t => t.TanzeemType == "out").ToList();

        sb.AppendLine("*تنظم کا حساب*");
        sb.AppendLine($"دکان سے تنظم کے پاس گئے — کل: {AccountingEngine.Format(totals.In)} روپے");
        AppendTanzeemList(sb, toTanzeem);

        sb.AppendLine($"تنظم سے دکان میں آئے — کل: {AccountingEngine.Format(totals.Out)} روپے");
        AppendTanzeemList(sb, fromTanzeem);
        sb.AppendLine();
    }

    private void AppendAccessoriesReport(StringBuilder sb)
    {
        var sales = _day.Transactions.Where(t => t.Category == "accessory" && t.AccessoryType != "return").ToList();
        var returns = _day.Transactions.Where(t => t.Category == "accessory" && t.AccessoryType == "return").ToList();
        var saleTotal = sales.Sum(t => t.Amount);
        var returnTotal = returns.Sum(t => t.Amount);

        sb.AppendLine("*موبائل اسیسریز*");
        sb.AppendLine($"فروخت — کل: {AccountingEngine.Format(saleTotal)} روپے");
        AppendAccessoryList(sb, sales);
        sb.AppendLine($"واپسی — کل: {AccountingEngine.Format(returnTotal)} روپے");
        AppendAccessoryList(sb, returns);
        sb.AppendLine($"خالص اسیسریز: {AccountingEngine.Format(saleTotal - returnTotal)} روپے");
        sb.AppendLine();
    }

    // JazzCash/Easypaisa account reports deliberately omit commission. The report is
    // meant to show the actual sent/received amounts cleanly; commission may vary by customer.
    private static void AppendAccountTransactionList(StringBuilder sb, IReadOnlyList<TransactionRecord> items)
    {
        if (items.Count == 0)
        {
            sb.AppendLine("کوئی انٹری نہیں۔");
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            var tx = items[i];
            var line = new StringBuilder();
            line.Append($"{i + 1}. {tx.Time} — {AccountingEngine.Format(tx.Amount)} روپے");
            if (!string.IsNullOrWhiteSpace(tx.Counter))
                line.Append($" — دوسری طرف: {ReportAccountName(tx.Counter)}");
            if (!string.IsNullOrWhiteSpace(tx.Note))
                line.Append($" — {tx.Note.Trim()}");
            sb.AppendLine(line.ToString());
        }
    }

    private static void AppendTanzeemList(StringBuilder sb, IReadOnlyList<TransactionRecord> items)
    {
        if (items.Count == 0)
        {
            sb.AppendLine("کوئی انٹری نہیں۔");
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            var tx = items[i];
            var line = new StringBuilder();
            line.Append($"{i + 1}. {tx.Time} — {AccountingEngine.Format(tx.Amount)} روپے — دکان کا اکاؤنٹ: {ReportAccountName(tx.Primary)}");
            var commission = AccountingEngine.TransactionCommission(tx);
            if (commission > 0)
                line.Append($" — کمیشن: {AccountingEngine.Format(commission)}");
            if (!string.IsNullOrWhiteSpace(tx.Note))
                line.Append($" — {tx.Note.Trim()}");
            sb.AppendLine(line.ToString());
        }
    }

    private static void AppendAccessoryList(StringBuilder sb, IReadOnlyList<TransactionRecord> items)
    {
        if (items.Count == 0)
        {
            sb.AppendLine("کوئی انٹری نہیں۔");
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            var tx = items[i];
            var line = $"{i + 1}. {tx.Time} — {AccountingEngine.Format(tx.Amount)} روپے";
            if (!string.IsNullOrWhiteSpace(tx.Note)) line += " — " + tx.Note.Trim();
            sb.AppendLine(line);
        }
    }

    private static string ReportAccountName(string key)
    {
        return Accounts.UrduName(key).Replace("تنظیم", "تنظم", StringComparison.Ordinal);
    }
}
