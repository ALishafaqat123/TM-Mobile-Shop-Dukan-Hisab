using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DukanHisab.App;

public partial class MainWindow
{
    private bool _uiPolishApplied;

    static MainWindow()
    {
        // Button class handlers run before the XAML instance Click handler. We only
        // intercept the seven navigation buttons; all save/delete buttons keep their
        // existing handlers unchanged.
        EventManager.RegisterClassHandler(
            typeof(Button),
            Button.ClickEvent,
            new RoutedEventHandler(InterceptNavigationClick),
            true);

        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(ApplyUiPolishOnLoaded),
            true);
    }

    private static void InterceptNavigationClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (Window.GetWindow(button) is not MainWindow window) return;

        switch (button.Name)
        {
            case "NavDashboard":
                window.FastShowPage(window.DashboardPage, "ڈیش بورڈ", window.NavDashboard);
                break;
            case "NavJazz":
                window._currentAccount = DukanHisab.Core.Accounts.Jazz;
                window.FillAccountCounters();
                window.FastShowPage(window.AccountPage, "جیزکیش ٹی ایم موبائل", window.NavJazz);
                break;
            case "NavEasy":
                window._currentAccount = DukanHisab.Core.Accounts.Easy;
                window.FillAccountCounters();
                window.FastShowPage(window.AccountPage, "ایزی پیسہ شفاقت", window.NavEasy);
                break;
            case "NavTanzeem":
                window.FastShowPage(window.TanzeemPage, "تنظیم اِن / آؤٹ", window.NavTanzeem);
                break;
            case "NavAccessories":
                window.FastShowPage(window.AccessoriesPage, "موبائل اسیسریز", window.NavAccessories);
                break;
            case "NavClosing":
                window.FastShowPage(window.ClosingPage, "دن کا آخری حساب / کل کاروباری رقم", window.NavClosing);
                break;
            case "NavHistory":
                window.FastShowPage(window.HistoryPage, "روزانہ ہسٹری", window.NavHistory);
                break;
            default:
                return;
        }

        // Prevent the old ShowPage handler from running afterwards. The old handler
        // refreshed every hidden grid and was the main cause of the tab-switch pause.
        e.Handled = true;
    }

    private void FastShowPage(FrameworkElement page, string title, Button nav)
    {
        DashboardPage.Visibility = Visibility.Collapsed;
        AccountPage.Visibility = Visibility.Collapsed;
        TanzeemPage.Visibility = Visibility.Collapsed;
        AccessoriesPage.Visibility = Visibility.Collapsed;
        ClosingPage.Visibility = Visibility.Collapsed;
        HistoryPage.Visibility = Visibility.Collapsed;

        page.Visibility = Visibility.Visible;
        PageTitle.Text = title;

        foreach (var b in new[]
                 {
                     NavDashboard, NavJazz, NavEasy, NavTanzeem,
                     NavAccessories, NavClosing, NavHistory
                 })
        {
            b.Background = Brushes.Transparent;
            b.Foreground = Brushes.White;
        }

        nav.Background = Application.Current.Resources["AccentBrush"] as Brush ?? Brushes.Goldenrod;
        nav.Foreground = Brushes.Black;

        // Recalculate the day once, then refresh only the visible page. Hidden pages
        // are refreshed lazily when the user opens them.
        DukanHisab.Core.AccountingEngine.Recalculate(_day);
        if (ReferenceEquals(page, DashboardPage)) RefreshDashboard();
        else if (ReferenceEquals(page, AccountPage)) RefreshAccountPage();
        else if (ReferenceEquals(page, TanzeemPage)) RefreshTanzeem();
        else if (ReferenceEquals(page, AccessoriesPage)) RefreshAccessories();
        else if (ReferenceEquals(page, ClosingPage)) RefreshClosing();
        else if (ReferenceEquals(page, HistoryPage)) RefreshHistory();
    }

    private static void ApplyUiPolishOnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window || window._uiPolishApplied) return;
        window._uiPolishApplied = true;
        PolishVisualTree(window);
    }

    private static void PolishVisualTree(DependencyObject root)
    {
        if (root is TextBlock text)
        {
            text.Text = PolishText(text.Text);
            TextOptions.SetTextFormattingMode(text, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(text, TextRenderingMode.ClearType);

            if (text.FontFamily.Source.Contains("Jameel", StringComparison.OrdinalIgnoreCase))
            {
                text.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
                var desired = Math.Max(27d, text.FontSize * 1.45d);
                if (double.IsNaN(text.LineHeight) || text.LineHeight < desired)
                    text.LineHeight = desired;
            }
        }
        else if (root is Button button)
        {
            if (button.Content is string caption) button.Content = PolishText(caption);
            if (button.FontFamily.Source.Contains("Jameel", StringComparison.OrdinalIgnoreCase))
            {
                button.MinHeight = Math.Max(button.MinHeight, 42d);
                button.VerticalContentAlignment = VerticalAlignment.Center;
            }
        }
        else if (root is ComboBox combo)
        {
            combo.MinHeight = Math.Max(combo.MinHeight, 40d);
        }
        else if (root is DataGrid grid)
        {
            grid.RowHeight = 36d;
            grid.ColumnHeaderHeight = 40d;
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++) PolishVisualTree(VisualTreeHelper.GetChild(root, i));
    }

    private static string PolishText(string text) => text switch
    {
        "Next Preview 0.1" => "Next Preview 0.2",
        "پرانی Smart PC v1.20 فائلیں صرف Read-only Import ہوتی ہیں۔" =>
            "پرانے v1.20 کا ڈیٹا صرف نقل کی صورت میں لیا جاتا ہے۔ اصل فائل تبدیل نہیں ہوتی۔",
        "v1.20 ڈیٹا کی Copy Import کریں" => "v1.20 کا ڈیٹا نقل کریں",
        "آج کا حسابی منافع (Live)" => "آج کا حسابی منافع",
        "اصل منافع / Closing" => "دن کا اصل منافع",
        "Opening" => "ابتدائی رقم",
        "کمیشن (خالی = Auto)" => "کمیشن (خالی چھوڑیں تو خودکار)",
        "اصل Closing" => "اصل اختتامی رقم",
        "Opening Total" => "ابتدائی کل رقم",
        "Closing Total" => "اختتامی کل رقم",
        "Closing محفوظ کریں" => "آخری حساب محفوظ کریں",
        "دن کے آخر میں اصل موجود رقمیں درج کریں۔ یہی Final Profit کی آخری سچائی ہے؛ JazzCash/Easypaisa کی تمام روزانہ انٹریاں لکھنا لازمی نہیں، لیکن تنظیم اِن/آؤٹ لازمی ہیں۔" =>
            "دن کے آخر میں اصل موجود رقمیں درج کریں۔ یہی اصل منافع کی آخری بنیاد ہے۔ جیزکیش اور ایزی پیسہ کی تمام روزانہ انٹریاں لکھنا لازمی نہیں، لیکن تنظیم اِن/آؤٹ لازمی ہیں۔",
        _ => text
    };
}
