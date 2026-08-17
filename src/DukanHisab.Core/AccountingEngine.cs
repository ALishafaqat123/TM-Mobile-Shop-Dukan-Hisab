using System.Globalization;

namespace DukanHisab.Core;

public static class AccountingEngine
{
    public static decimal Money(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0m;
        var clean = raw.Replace(",", "").Trim();
        return decimal.TryParse(clean, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }

    public static bool TryMoney(string? raw, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var clean = raw.Replace(",", "").Trim();
        return decimal.TryParse(clean, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    public static string Format(decimal value) => value.ToString("#,##0.##", CultureInfo.InvariantCulture);

    public static bool IsAutoCommissionEligible(string primary, string direction, decimal amount, string counter, string tanzeemType = "")
    {
        if (amount <= 0) return false;
        if ((primary is Accounts.Jazz or Accounts.Easy or Accounts.EasyTanzeem) && counter == Accounts.Drawer) return true;
        if (primary == Accounts.Drawer && counter == Accounts.Tanzeem && (tanzeemType is "in" or "out")) return true;
        return false;
    }

    public static decimal AutoCommission(string primary, string direction, decimal amount, string counter, string tanzeemType = "")
    {
        if (!IsAutoCommissionEligible(primary, direction, amount, counter, tanzeemType)) return 0m;
        if (amount < 100m) return 0m;
        var steps = (decimal)Math.Ceiling((double)amount / 1000d);
        if (steps < 1) steps = 1;

        if (primary == Accounts.Drawer && counter == Accounts.Tanzeem)
        {
            if (tanzeemType == "in") return 20m * steps;
            if (tanzeemType == "out") return 10m * steps;
        }

        return direction == "in" ? 20m * steps : 10m * steps;
    }

    public static decimal TransactionCommission(TransactionRecord tx)
    {
        return tx.CommissionMode switch
        {
            "none" => 0m,
            "override" => Money(tx.CommissionOverride),
            "auto" => AutoCommission(tx.Primary, tx.Direction, tx.Amount, tx.Counter, tx.TanzeemType),
            _ => 0m
        };
    }

    public static IReadOnlyList<Leg> EffectiveLegs(TransactionRecord tx)
    {
        var legs = tx.Legs.Select(x => new Leg { Account = x.Account, Delta = x.Delta }).ToList();
        var commission = TransactionCommission(tx);
        if (commission <= 0) return legs;

        var drawer = legs.FirstOrDefault(x => x.Account == Accounts.Drawer);
        if (drawer is null)
            legs.Add(new Leg { Account = Accounts.Drawer, Delta = commission });
        else
            drawer.Delta += commission;
        return legs;
    }

    public static TransactionRecord BuildTransaction(string primary, string direction, decimal amount, string counter, string note = "", string commissionOverride = "", string? id = null, string? time = null)
    {
        var tanzeemType = "";
        var primaryDelta = direction == "in" ? amount : -amount;
        var legs = new List<Leg> { new() { Account = primary, Delta = primaryDelta } };

        if (Accounts.Internal.Contains(counter))
            legs.Add(new Leg { Account = counter, Delta = -primaryDelta });
        else if (counter == Accounts.Tanzeem)
            tanzeemType = direction == "out" ? "in" : "out";

        var eligible = IsAutoCommissionEligible(primary, direction, amount, counter, tanzeemType);
        var mode = eligible ? (string.IsNullOrWhiteSpace(commissionOverride) ? "auto" : "override") : "none";

        return new TransactionRecord
        {
            Id = id ?? Guid.NewGuid().ToString("N"),
            Time = time ?? DateTime.Now.ToString("hh:mm:ss tt", CultureInfo.InvariantCulture),
            Primary = primary,
            Direction = direction,
            Amount = amount,
            Counter = counter,
            Note = note ?? "",
            TanzeemType = tanzeemType,
            CommissionMode = mode,
            CommissionOverride = commissionOverride ?? "",
            Legs = legs
        };
    }

    public static TransactionRecord BuildAccessory(decimal amount, bool isReturn, string note = "", string? id = null, string? time = null)
    {
        var delta = isReturn ? -amount : amount;
        return new TransactionRecord
        {
            Id = id ?? Guid.NewGuid().ToString("N"),
            Time = time ?? DateTime.Now.ToString("hh:mm:ss tt", CultureInfo.InvariantCulture),
            Primary = Accounts.Drawer,
            Direction = isReturn ? "out" : "in",
            Amount = amount,
            Counter = Accounts.Accessories,
            Note = note ?? "",
            Category = "accessory",
            AccessoryType = isReturn ? "return" : "sale",
            TanzeemType = "",
            CommissionMode = "none",
            CommissionOverride = "",
            Legs = [new Leg { Account = Accounts.Drawer, Delta = delta }]
        };
    }

    public static TransactionRecord BuildTanzeem(string tanzeemType, decimal amount, string shopAccount, string note = "", string commissionOverride = "", string? id = null, string? time = null)
    {
        var direction = tanzeemType == "in" ? "out" : "in";
        var tx = BuildTransaction(shopAccount, direction, amount, Accounts.Tanzeem, note, commissionOverride, id, time);
        tx.TanzeemType = tanzeemType;
        var eligible = IsAutoCommissionEligible(tx.Primary, tx.Direction, amount, tx.Counter, tx.TanzeemType);
        tx.CommissionMode = eligible ? (string.IsNullOrWhiteSpace(commissionOverride) ? "auto" : "override") : "none";
        return tx;
    }

    public static AccountBalances CurrentBalances(DayRecord day)
    {
        var current = day.Opening.Clone();
        foreach (var tx in day.Transactions)
            foreach (var leg in EffectiveLegs(tx))
                if (Accounts.Internal.Contains(leg.Account)) current.Add(leg.Account, leg.Delta);
        return current;
    }

    public static TanzeemSnapshot TanzeemTotals(DayRecord day)
    {
        decimal tin = 0, tout = 0;
        var cin = 0;
        var cout = 0;
        foreach (var tx in day.Transactions)
        {
            if (tx.TanzeemType == "in") { tin += tx.Amount; cin++; }
            else if (tx.TanzeemType == "out") { tout += tx.Amount; cout++; }
        }
        return new TanzeemSnapshot { In = tin, Out = tout, InCount = cin, OutCount = cout };
    }

    public static decimal LiveProfit(DayRecord day)
    {
        var current = CurrentBalances(day);
        var tz = TanzeemTotals(day);
        return current.Total - day.Opening.Total + tz.In - tz.Out;
    }

    public static bool ClosingComplete(ClosingData closing)
        => Accounts.Internal.All(a => !string.IsNullOrWhiteSpace(closing.Get(a)));

    public static ClosingSummary GetClosingSummary(DayRecord day)
    {
        var book = CurrentBalances(day);
        var tz = TanzeemTotals(day);
        if (!ClosingComplete(day.Closing))
            return new ClosingSummary(false, null, 0m, null, book, book.Total, day.Opening.Total, tz, null);

        var actual = new AccountBalances
        {
            Drawer = Money(day.Closing.Drawer),
            Computer = Money(day.Closing.Computer),
            Easy = Money(day.Closing.Easy),
            EasyTanzeem = Money(day.Closing.EasyTanzeem),
            Jazz = Money(day.Closing.Jazz),
            Oneload = Money(day.Closing.Oneload)
        };
        var profit = actual.Total - day.Opening.Total + tz.In - tz.Out;
        return new ClosingSummary(true, actual, actual.Total, profit, book, book.Total, day.Opening.Total, tz, actual.Total - book.Total);
    }

    public static ProfitSources ProfitSourceSummary(DayRecord day, bool useClosing)
    {
        decimal jazz = 0, easy = 0, tanzeem = 0, sales = 0, returns = 0;
        foreach (var tx in day.Transactions)
        {
            var cm = TransactionCommission(tx);
            if (cm > 0)
            {
                if (tx.TanzeemType is "in" or "out") tanzeem += cm;
                else if (tx.Primary == Accounts.Jazz) jazz += cm;
                else if (tx.Primary is Accounts.Easy or Accounts.EasyTanzeem) easy += cm;
            }
            if (tx.Category == "accessory")
            {
                if (tx.AccessoryType == "return") returns += tx.Amount;
                else sales += tx.Amount;
            }
        }

        var current = CurrentBalances(day);
        var computer = current.Computer - day.Opening.Computer;
        var total = LiveProfit(day);
        if (useClosing)
        {
            var closing = GetClosingSummary(day);
            if (closing.Complete && closing.Actual is not null)
            {
                total = closing.ActualProfit ?? 0m;
                computer = closing.Actual.Computer - day.Opening.Computer;
            }
        }

        var accessories = sales - returns;
        var known = jazz + easy + tanzeem + accessories + computer;
        return new ProfitSources(jazz, easy, tanzeem, accessories, computer, known, total - known, total - computer, total);
    }

    public static AccountBalances OpeningFromPrevious(DayRecord previous, bool preferClosing = true)
    {
        if (preferClosing && ClosingComplete(previous.Closing))
        {
            return new AccountBalances
            {
                Drawer = Money(previous.Closing.Drawer) + Money(previous.Closing.Computer),
                Computer = 0m,
                Easy = Money(previous.Closing.Easy),
                EasyTanzeem = Money(previous.Closing.EasyTanzeem),
                Jazz = Money(previous.Closing.Jazz),
                Oneload = Money(previous.Closing.Oneload)
            };
        }

        var current = CurrentBalances(previous);
        return new AccountBalances
        {
            Drawer = current.Drawer + current.Computer,
            Computer = 0m,
            Easy = current.Easy,
            EasyTanzeem = current.EasyTanzeem,
            Jazz = current.Jazz,
            Oneload = current.Oneload
        };
    }

    public static bool SameOpening(AccountBalances a, AccountBalances b)
        => a.Drawer == b.Drawer && a.Computer == b.Computer && a.Easy == b.Easy && a.EasyTanzeem == b.EasyTanzeem && a.Jazz == b.Jazz && a.Oneload == b.Oneload;

    public static void Recalculate(DayRecord day)
    {
        day.Current = CurrentBalances(day);
        day.Tanzeem = TanzeemTotals(day);
        day.OpeningTotal = day.Opening.Total;
        day.CurrentTotal = day.Current.Total;
        day.Profit = day.CurrentTotal - day.OpeningTotal + day.Tanzeem.In - day.Tanzeem.Out;
    }

    public static AuditEntry Audit(string kind, string summary, string before = "", string after = "") => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        At = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt", CultureInfo.InvariantCulture),
        Kind = kind,
        Summary = summary,
        Before = before,
        After = after
    };

    public static string TransactionShort(TransactionRecord tx)
    {
        var dir = tx.Category == "accessory" ? (tx.AccessoryType == "return" ? "واپسی" : "فروخت") : (tx.Direction == "in" ? "اِن" : "آؤٹ");
        var account = tx.Category == "accessory" ? "موبائل اسیسریز" : Accounts.UrduName(tx.Primary);
        var counter = tx.Category == "accessory" ? "دوسرے گلے کی نقد" : Accounts.UrduName(tx.Counter);
        var text = $"{account} {dir} {Format(tx.Amount)}، دوسری طرف: {counter}";
        var cm = TransactionCommission(tx);
        if (cm > 0) text += $"، کمیشن {Format(cm)}";
        if (!string.IsNullOrWhiteSpace(tx.Note)) text += $"، نوٹ: {tx.Note}";
        return text;
    }
}
