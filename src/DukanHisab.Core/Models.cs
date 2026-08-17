using System.Text.Json.Serialization;

namespace DukanHisab.Core;

public static class Accounts
{
    public const string Drawer = "drawer";
    public const string Computer = "computer";
    public const string Easy = "easy";
    public const string EasyTanzeem = "easy_tanzeem";
    public const string Jazz = "jazz";
    public const string OneLoad = "oneload";
    public const string Tanzeem = "tanzeem";
    public const string Accessories = "accessories";
    public const string External = "external";

    public static readonly string[] Internal = [Drawer, Computer, Easy, EasyTanzeem, Jazz, OneLoad];

    public static string UrduName(string key) => key switch
    {
        Drawer => "دوسرے گلے کی کیش",
        Computer => "کمپیوٹر کیش",
        Easy => "ایزی پیسہ شفاقت",
        EasyTanzeem => "ایزی پیسہ تنظم",
        Jazz => "جیزکیش ٹی ایم موبائل",
        OneLoad => "ون لوڈ",
        Tanzeem => "تنظم",
        Accessories => "موبائل اسیسریز",
        External => "دیگر / نامعلوم",
        _ => key
    };
}

public sealed class AccountBalances
{
    public decimal Drawer { get; set; }
    public decimal Computer { get; set; }
    public decimal Easy { get; set; }
    public decimal EasyTanzeem { get; set; }
    public decimal Jazz { get; set; }
    public decimal Oneload { get; set; }

    [JsonIgnore]
    public decimal Total => Drawer + Computer + Easy + EasyTanzeem + Jazz + Oneload;

    public decimal Get(string key) => key switch
    {
        Accounts.Drawer => Drawer,
        Accounts.Computer => Computer,
        Accounts.Easy => Easy,
        Accounts.EasyTanzeem => EasyTanzeem,
        Accounts.Jazz => Jazz,
        Accounts.OneLoad => Oneload,
        _ => 0m
    };

    public void Set(string key, decimal value)
    {
        switch (key)
        {
            case Accounts.Drawer: Drawer = value; break;
            case Accounts.Computer: Computer = value; break;
            case Accounts.Easy: Easy = value; break;
            case Accounts.EasyTanzeem: EasyTanzeem = value; break;
            case Accounts.Jazz: Jazz = value; break;
            case Accounts.OneLoad: Oneload = value; break;
        }
    }

    public void Add(string key, decimal delta) => Set(key, Get(key) + delta);

    public AccountBalances Clone() => new()
    {
        Drawer = Drawer,
        Computer = Computer,
        Easy = Easy,
        EasyTanzeem = EasyTanzeem,
        Jazz = Jazz,
        Oneload = Oneload
    };
}

public sealed class Leg
{
    public string Account { get; set; } = "";
    public decimal Delta { get; set; }
}

public sealed class TransactionRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Time { get; set; } = "";
    public string Primary { get; set; } = "";
    public string Direction { get; set; } = "";
    public decimal Amount { get; set; }
    public string Counter { get; set; } = "";
    public string Note { get; set; } = "";
    public string? Category { get; set; }
    public string? AccessoryType { get; set; }
    public string TanzeemType { get; set; } = "";
    public string CommissionMode { get; set; } = "none";
    public string CommissionOverride { get; set; } = "";
    public List<Leg> Legs { get; set; } = [];
}

public sealed class AuditEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string At { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Before { get; set; } = "";
    public string After { get; set; } = "";
}

public sealed class ChecksData
{
    public string DrawerActual { get; set; } = "";
    public string DrawerCheckedAt { get; set; } = "";
}

public sealed class ClosingData
{
    public string Drawer { get; set; } = "";
    public string Computer { get; set; } = "";
    public string Easy { get; set; } = "";

    // Default 0 keeps already-saved pre-feature closing records complete when this
    // new account did not exist yet. New days explicitly reset this to blank.
    public string EasyTanzeem { get; set; } = "0";

    public string Jazz { get; set; } = "";
    public string Oneload { get; set; } = "";
    public string CheckedAt { get; set; } = "";

    public string Get(string key) => key switch
    {
        Accounts.Drawer => Drawer,
        Accounts.Computer => Computer,
        Accounts.Easy => Easy,
        Accounts.EasyTanzeem => EasyTanzeem,
        Accounts.Jazz => Jazz,
        Accounts.OneLoad => Oneload,
        _ => ""
    };

    public void Set(string key, string value)
    {
        switch (key)
        {
            case Accounts.Drawer: Drawer = value; break;
            case Accounts.Computer: Computer = value; break;
            case Accounts.Easy: Easy = value; break;
            case Accounts.EasyTanzeem: EasyTanzeem = value; break;
            case Accounts.Jazz: Jazz = value; break;
            case Accounts.OneLoad: Oneload = value; break;
        }
    }

    public ClosingData Clone() => new()
    {
        Drawer = Drawer,
        Computer = Computer,
        Easy = Easy,
        EasyTanzeem = EasyTanzeem,
        Jazz = Jazz,
        Oneload = Oneload,
        CheckedAt = CheckedAt
    };
}

public sealed class TanzeemSnapshot
{
    public decimal In { get; set; }
    public decimal Out { get; set; }
    public int InCount { get; set; }
    public int OutCount { get; set; }
}

public sealed class DayRecord
{
    public string Version { get; set; } = "next-0.1";
    public string Date { get; set; } = "";
    public string BaselineTime { get; set; } = "";

    // Non-empty means the Opening snapshot was manually counted for this date.
    // Previous-day repair/sync must not replace this user-selected base.
    public string ManualReferenceAt { get; set; } = "";

    public AccountBalances Opening { get; set; } = new();
    public AccountBalances Current { get; set; } = new();
    public List<TransactionRecord> Transactions { get; set; } = [];
    public List<AuditEntry> Audit { get; set; } = [];
    public ChecksData Checks { get; set; } = new();
    public ClosingData Closing { get; set; } = new();
    public TanzeemSnapshot Tanzeem { get; set; } = new();
    public decimal OpeningTotal { get; set; }
    public decimal CurrentTotal { get; set; }
    public decimal Profit { get; set; }
    public string SavedAt { get; set; } = "";
}

public sealed record ProfitSources(
    decimal Jazz,
    decimal Easy,
    decimal Tanzeem,
    decimal Accessories,
    decimal Computer,
    decimal Known,
    decimal Unexplained,
    decimal ShopWithoutComputer,
    decimal Total);

public sealed record ClosingSummary(
    bool Complete,
    AccountBalances? Actual,
    decimal ActualTotal,
    decimal? ActualProfit,
    AccountBalances Book,
    decimal BookTotal,
    decimal OpeningTotal,
    TanzeemSnapshot Tanzeem,
    decimal? TotalDiff);
