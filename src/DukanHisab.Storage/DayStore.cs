using System.Text;
using System.Text.Json;
using DukanHisab.Core;

namespace DukanHisab.Storage;

public sealed class DayStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static readonly JsonSerializerOptions CompactJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public string Root { get; }
    public string DaysDirectory { get; }
    public string LegacyDaysDirectory { get; }
    public string LegacyMirrorDirectory { get; }

    public DayStore()
    {
        Root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DukanHisabNext");
        DaysDirectory = Path.Combine(Root, "days");
        LegacyDaysDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DukanHisabSmartPC", "days");
        LegacyMirrorDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Dukan Hisab Data");
        Directory.CreateDirectory(DaysDirectory);
    }

    public string NativePath(DateTime date) => Path.Combine(DaysDirectory, date.ToString("yyyy-MM-dd") + ".json");

    public DayRecord? Load(DateTime date)
    {
        var native = NativePath(date);
        if (File.Exists(native)) return ReadJson(native);
        var imported = ReadLegacy(date);
        if (imported is not null)
        {
            imported.Version = "next-imported-v1.20";
            Save(imported, false, false);
        }
        return imported;
    }

    public IReadOnlyList<DateTime> ListDates()
    {
        var dates = new HashSet<DateTime>();
        foreach (var dir in new[] { DaysDirectory, LegacyDaysDirectory })
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var file in Directory.GetFiles(dir, "????-??-??.json"))
                if (DateTime.TryParseExact(Path.GetFileNameWithoutExtension(file), "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var d)) dates.Add(d.Date);
        }
        if (Directory.Exists(LegacyMirrorDirectory))
        {
            foreach (var file in Directory.GetFiles(LegacyMirrorDirectory, "Dukan-Hisab-????-??-??.txt"))
            {
                var name = Path.GetFileNameWithoutExtension(file).Replace("Dukan-Hisab-", "");
                if (DateTime.TryParseExact(name, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var d)) dates.Add(d.Date);
            }
        }
        return dates.OrderByDescending(x => x).ToList();
    }

    public int ImportAllLegacyCopies()
    {
        var count = 0;
        foreach (var date in ListLegacyDates())
        {
            if (File.Exists(NativePath(date))) continue;
            var item = ReadLegacy(date);
            if (item is null) continue;
            item.Version = "next-imported-v1.20";
            Save(item, false, false);
            count++;
        }
        return count;
    }

    public DayRecord CreateNew(DateTime date, AccountBalances opening)
    {
        var day = new DayRecord
        {
            Version = "next-0.1",
            Date = date.ToString("yyyy-MM-dd"),
            Opening = opening.Clone(), Current = opening.Clone(), Transactions = [], Audit = [],
            Closing = new ClosingData(), Checks = new ChecksData()
        };
        AccountingEngine.Recalculate(day);
        Save(day);
        return day;
    }

    public void Save(DayRecord day, bool writeAuditTimestamp = true, bool writeTextMirror = true)
    {
        AccountingEngine.Recalculate(day);
        if (writeAuditTimestamp) day.SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var date = DateTime.ParseExact(day.Date, "yyyy-MM-dd", null);
        var path = NativePath(date);
        var tmp = path + ".tmp";
        var bak = path + ".bak";
        var json = JsonSerializer.Serialize(day, JsonOptions);
        File.WriteAllText(tmp, json, new UTF8Encoding(true));
        if (File.Exists(path)) File.Copy(path, bak, true);
        File.Move(tmp, path, true);
        _ = ReadJson(path) ?? throw new IOException("Saved day could not be read back.");

        if (writeTextMirror) WriteTextMirror(day, date);
    }

    private void WriteTextMirror(DayRecord day, DateTime date)
    {
        Directory.CreateDirectory(LegacyMirrorDirectory);
        var path = Path.Combine(LegacyMirrorDirectory, "Dukan-Hisab-" + date.ToString("yyyy-MM-dd") + ".txt");
        var tmp = path + ".tmp";
        var bak = path + ".bak";
        var report = BuildTextReport(day, date);

        File.WriteAllText(tmp, report, new UTF8Encoding(true));
        if (File.Exists(path)) File.Copy(path, bak, true);
        File.Move(tmp, path, true);

        // Verify the recovery payload can be read back before considering the mirror healthy.
        var recoveryLine = File.ReadLines(path, Encoding.UTF8)
            .LastOrDefault(x => x.StartsWith("SMART_DATA_JSON=", StringComparison.Ordinal));
        if (string.IsNullOrWhiteSpace(recoveryLine))
            throw new IOException("Daily TXT mirror was written without recovery data.");
        _ = JsonSerializer.Deserialize<DayRecord>(recoveryLine["SMART_DATA_JSON=".Length..], CompactJsonOptions)
            ?? throw new IOException("Daily TXT mirror recovery data could not be read back.");
    }

    private static string BuildTextReport(DayRecord day, DateTime date)
    {
        var sb = new StringBuilder();
        var current = AccountingEngine.CurrentBalances(day);
        var tz = AccountingEngine.TanzeemTotals(day);
        var live = AccountingEngine.LiveProfit(day);
        var liveSrc = AccountingEngine.ProfitSourceSummary(day, false);
        var closing = AccountingEngine.GetClosingSummary(day);

        sb.AppendLine($"دکان حساب Next — {date:dd-MM-yyyy}");
        sb.AppendLine();
        AppendAccount(sb, Accounts.Drawer, day.Opening.Drawer, current.Drawer);
        AppendAccount(sb, Accounts.Computer, day.Opening.Computer, current.Computer);
        AppendAccount(sb, Accounts.Easy, day.Opening.Easy, current.Easy);
        AppendAccount(sb, Accounts.Jazz, day.Opening.Jazz, current.Jazz);
        AppendAccount(sb, Accounts.OneLoad, day.Opening.Oneload, current.Oneload);
        sb.AppendLine();
        sb.AppendLine($"دکان سے تنظم کے پاس گئے: {Fmt(tz.In)} روپے ({tz.InCount} دفعہ)");
        sb.AppendLine($"تنظم سے دکان میں آئے: {Fmt(tz.Out)} روپے ({tz.OutCount} دفعہ)");
        sb.AppendLine($"انٹریوں کے مطابق حسابی منافع/فرق: {Fmt(live)} روپے");
        sb.AppendLine($"منافع کی حسابی تقسیم: جیزکیش کمیشن {Fmt(liveSrc.Jazz)} | ایزی پیسہ کمیشن {Fmt(liveSrc.Easy)} | تنظم کمیشن {Fmt(liveSrc.Tanzeem)} | اسیسریز {Fmt(liveSrc.Accessories)} | کمپیوٹر {Fmt(liveSrc.Computer)} | غیر واضح {Fmt(liveSrc.Unexplained)}");
        sb.AppendLine($"دکان کا منافع بغیر کمپیوٹر: {Fmt(liveSrc.ShopWithoutComputer)} روپے");
        sb.AppendLine();

        sb.AppendLine("دن کا آخری حساب / کل کاروباری رقم:");
        if (closing.Complete && closing.Actual is not null)
        {
            AppendClosing(sb, Accounts.Computer, closing.Book.Computer, closing.Actual.Computer);
            AppendClosing(sb, Accounts.Drawer, closing.Book.Drawer, closing.Actual.Drawer);
            AppendClosing(sb, Accounts.Jazz, closing.Book.Jazz, closing.Actual.Jazz);
            AppendClosing(sb, Accounts.Easy, closing.Book.Easy, closing.Actual.Easy);
            AppendClosing(sb, Accounts.OneLoad, closing.Book.Oneload, closing.Actual.Oneload);
            var closeSrc = AccountingEngine.ProfitSourceSummary(day, true);
            sb.AppendLine($"اصل اختتامی کل رقم: {Fmt(closing.ActualTotal)} روپے");
            sb.AppendLine($"دن کا اصل منافع: {Fmt(closing.ActualProfit ?? 0m)} روپے");
            sb.AppendLine($"آخری حساب کی تقسیم: جیزکیش کمیشن {Fmt(closeSrc.Jazz)} | ایزی پیسہ کمیشن {Fmt(closeSrc.Easy)} | تنظم کمیشن {Fmt(closeSrc.Tanzeem)} | اسیسریز {Fmt(closeSrc.Accessories)} | کمپیوٹر {Fmt(closeSrc.Computer)} | غیر واضح {Fmt(closeSrc.Unexplained)}");
            sb.AppendLine($"آخری حساب محفوظ: {day.Closing.CheckedAt}");
        }
        else
        {
            sb.AppendLine("آخری حساب ابھی مکمل نہیں۔");
        }

        sb.AppendLine();
        sb.AppendLine("آج کی تمام انٹریز:");
        for (var i = 0; i < day.Transactions.Count; i++)
            sb.AppendLine(TransactionReportLine(i + 1, day.Transactions[i]));

        if (day.Audit.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"درستیاں / ترامیم: {day.Audit.Count}");
        }

        sb.AppendLine();
        sb.Append("SMART_DATA_JSON=");
        sb.Append(JsonSerializer.Serialize(day, CompactJsonOptions));
        return sb.ToString();
    }

    private static void AppendAccount(StringBuilder sb, string account, decimal opening, decimal current)
        => sb.AppendLine($"{UrduAccount(account)}: آغاز {Fmt(opening)} | ابھی {Fmt(current)} روپے");

    private static void AppendClosing(StringBuilder sb, string account, decimal book, decimal actual)
        => sb.AppendLine($"{UrduAccount(account)}: حسابی {Fmt(book)} | اصل {Fmt(actual)} | فرق {Fmt(actual - book)} روپے");

    private static string TransactionReportLine(int index, TransactionRecord tx)
    {
        string primary;
        string direction;
        string counter;

        if (tx.Category == "accessory")
        {
            primary = "موبائل اسیسریز";
            direction = tx.AccessoryType == "return" ? "واپسی" : "فروخت";
            counter = "دوسرے گلے کی نقد";
        }
        else if (tx.TanzeemType is "in" or "out")
        {
            primary = UrduAccount(tx.Primary);
            direction = tx.TanzeemType == "in" ? "دکان سے تنظم کے پاس گئے" : "تنظم سے دکان میں آئے";
            counter = "تنظم";
        }
        else
        {
            primary = UrduAccount(tx.Primary);
            direction = tx.Direction == "in" ? "وصول کیے" : "سینڈ کیے";
            counter = UrduAccount(tx.Counter);
        }

        var line = $"{index}. {tx.Time} — {primary} — {direction} — {Fmt(tx.Amount)} — دوسری طرف: {counter}";
        var commission = AccountingEngine.TransactionCommission(tx);
        if (commission > 0) line += $" — کمیشن: {Fmt(commission)}";
        if (!string.IsNullOrWhiteSpace(tx.Note)) line += $" — {tx.Note.Trim()}";
        return line;
    }

    private static string UrduAccount(string key)
        => key == Accounts.Tanzeem ? "تنظم" : Accounts.UrduName(key).Replace("تنظیم", "تنظم", StringComparison.Ordinal);

    private static string Fmt(decimal value) => AccountingEngine.Format(value);

    public string SyncNextOpeningAfterClosingChange(DateTime sourceDate, ClosingData oldClosing, AccountBalances oldBook, bool oldWasComplete, ClosingData newClosing)
    {
        var nextDate = sourceDate.Date.AddDays(1);
        var next = LoadNativeOnly(nextDate);
        if (next is null) return "";

        var oldPrev = new DayRecord { Opening = new AccountBalances(), Current = oldBook.Clone(), Closing = oldClosing.Clone() };
        var newPrev = new DayRecord { Opening = new AccountBalances(), Closing = newClosing.Clone(), Current = oldBook.Clone() };
        var oldExpected = AccountingEngine.OpeningFromPrevious(oldPrev, oldWasComplete);
        var newExpected = AccountingEngine.OpeningFromPrevious(newPrev, true);

        if (!AccountingEngine.SameOpening(next.Opening, oldExpected))
            return "اگلے دن کا Opening پہلے ہی الگ رقموں پر ہے؛ خودکار تبدیلی نہیں کی گئی۔";
        if (AccountingEngine.SameOpening(next.Opening, newExpected)) return "";

        var before = next.Opening.Total;
        next.Opening = newExpected;
        next.Audit.Add(AccountingEngine.Audit("opening_sync", "پچھلے دن کی Closing درست ہونے پر Opening خودکار اپڈیٹ", AccountingEngine.Format(before) + " روپے", AccountingEngine.Format(newExpected.Total) + " روپے"));
        Save(next);
        return $"{nextDate:dd-MM-yyyy} کا Opening نئی Closing کے مطابق اپڈیٹ ہوگیا۔";
    }

    public bool RepairOpeningFromPreviousIfNeeded(DateTime date, DayRecord day, out string note)
    {
        note = "";
        var previousDate = FindPreviousDate(date);
        if (previousDate is null) return false;
        var previous = Load(previousDate.Value);
        if (previous is null || !AccountingEngine.ClosingComplete(previous.Closing)) return false;
        var correct = AccountingEngine.OpeningFromPrevious(previous, true);
        if (AccountingEngine.SameOpening(day.Opening, correct)) return false;

        var before = day.Opening.Total;
        day.Opening = correct;
        day.Audit.Add(AccountingEngine.Audit("opening_repair", "پچھلے دن کی Closing کے مطابق Opening درست کیا گیا", AccountingEngine.Format(before) + " روپے", AccountingEngine.Format(correct.Total) + " روپے"));
        Save(day);
        note = $"Opening {AccountingEngine.Format(before)} سے {AccountingEngine.Format(correct.Total)} روپے پر درست ہوا۔";
        return true;
    }

    public DateTime? FindPreviousDate(DateTime date)
    {
        var found = ListDates().Where(x => x.Date < date.Date).OrderByDescending(x => x).FirstOrDefault();
        return found == default ? null : found;
    }

    public DayRecord? LoadNativeOnly(DateTime date)
    {
        var path = NativePath(date);
        return File.Exists(path) ? ReadJson(path) : null;
    }

    private DayRecord? ReadLegacy(DateTime date)
    {
        var jsonPath = Path.Combine(LegacyDaysDirectory, date.ToString("yyyy-MM-dd") + ".json");
        if (File.Exists(jsonPath))
        {
            var direct = ReadJson(jsonPath);
            if (direct is not null) return direct;
            var bak = jsonPath + ".bak";
            if (File.Exists(bak))
            {
                var backup = ReadJson(bak);
                if (backup is not null) return backup;
            }
        }

        var txt = Path.Combine(LegacyMirrorDirectory, "Dukan-Hisab-" + date.ToString("yyyy-MM-dd") + ".txt");
        if (!File.Exists(txt)) return null;
        try
        {
            var line = File.ReadLines(txt, Encoding.UTF8).LastOrDefault(x => x.StartsWith("SMART_DATA_JSON=", StringComparison.Ordinal));
            if (line is null) return null;
            return JsonSerializer.Deserialize<DayRecord>(line["SMART_DATA_JSON=".Length..], JsonOptions);
        }
        catch { return null; }
    }

    private DayRecord? ReadJson(string path)
    {
        try { return JsonSerializer.Deserialize<DayRecord>(File.ReadAllText(path, Encoding.UTF8), JsonOptions); }
        catch { return null; }
    }

    private IEnumerable<DateTime> ListLegacyDates()
    {
        var dates = new HashSet<DateTime>();
        if (Directory.Exists(LegacyDaysDirectory))
            foreach (var file in Directory.GetFiles(LegacyDaysDirectory, "????-??-??.json"))
                if (DateTime.TryParseExact(Path.GetFileNameWithoutExtension(file), "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var d)) dates.Add(d.Date);
        if (Directory.Exists(LegacyMirrorDirectory))
            foreach (var file in Directory.GetFiles(LegacyMirrorDirectory, "Dukan-Hisab-????-??-??.txt"))
            {
                var name = Path.GetFileNameWithoutExtension(file).Replace("Dukan-Hisab-", "");
                if (DateTime.TryParseExact(name, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var d)) dates.Add(d.Date);
            }
        return dates.OrderBy(x => x);
    }
}
