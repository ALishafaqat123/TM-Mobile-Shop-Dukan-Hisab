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
            Save(imported, false);
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
            Save(item, false);
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

    public void Save(DayRecord day, bool writeAuditTimestamp = true)
    {
        AccountingEngine.Recalculate(day);
        if (writeAuditTimestamp) day.SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var path = NativePath(DateTime.ParseExact(day.Date, "yyyy-MM-dd", null));
        var tmp = path + ".tmp";
        var bak = path + ".bak";
        var json = JsonSerializer.Serialize(day, JsonOptions);
        File.WriteAllText(tmp, json, new UTF8Encoding(true));
        if (File.Exists(path)) File.Copy(path, bak, true);
        File.Move(tmp, path, true);
        _ = ReadJson(path) ?? throw new IOException("Saved day could not be read back.");
    }

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
