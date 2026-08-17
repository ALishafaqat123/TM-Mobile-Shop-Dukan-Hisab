using DukanHisab.Core;

static void Eq(decimal expected, decimal actual, string name)
{
    if (expected != actual) throw new Exception($"{name}: expected {expected}, got {actual}");
    Console.WriteLine($"PASS {name}: {actual}");
}

static DayRecord NewDay() => new()
{
    Date = "2026-08-13",
    Opening = new AccountBalances { Drawer = 74220, Computer = 0, Jazz = 32492, Easy = 11492, EasyTanzeem = 0, Oneload = 2392 },
    Closing = new ClosingData(),
    Transactions = []
};

{
    var d = NewDay();
    d.Transactions.Add(AccountingEngine.BuildTransaction(Accounts.Jazz, "out", 2000, Accounts.Easy));
    Eq(0, AccountingEngine.LiveProfit(d), "internal transfer profit neutral");
}
{
    var d = NewDay();
    d.Opening.EasyTanzeem = 5000;
    d.Transactions.Add(AccountingEngine.BuildTransaction(Accounts.EasyTanzeem, "out", 1200, Accounts.Jazz));
    Eq(0, AccountingEngine.LiveProfit(d), "Easypaisa Tanzeem internal transfer neutral");
    Eq(3800, AccountingEngine.CurrentBalances(d).EasyTanzeem, "Easypaisa Tanzeem balance updates");
}
{
    var d = NewDay();
    d.Transactions.Add(AccountingEngine.BuildTanzeem("out", 10000, Accounts.OneLoad));
    Eq(0, AccountingEngine.LiveProfit(d), "Tanzeem Out funding neutral");
}
{
    var d = NewDay();
    d.Transactions.Add(AccountingEngine.BuildTanzeem("in", 2040, Accounts.Drawer, commissionOverride: "40"));
    var c = AccountingEngine.CurrentBalances(d);
    Eq(72220, c.Drawer, "2040 Tanzeem drawer delta");
    Eq(40, AccountingEngine.LiveProfit(d), "2040 Tanzeem profit");
}
{
    var d = NewDay();
    var auto = AccountingEngine.BuildTanzeem("in", 50000, Accounts.Drawer);
    d.Transactions.Add(auto);
    Eq(1000, AccountingEngine.TransactionCommission(auto), "50000 auto commission");
    Eq(1000, AccountingEngine.LiveProfit(d), "50000 auto live profit");
    d.Transactions.Clear();
    var zero = AccountingEngine.BuildTanzeem("in", 50000, Accounts.Drawer, commissionOverride: "0");
    d.Transactions.Add(zero);
    Eq(0, AccountingEngine.TransactionCommission(zero), "manual zero overrides auto");
    Eq(0, AccountingEngine.LiveProfit(d), "manual zero live profit");
}
{
    var prev = NewDay();
    prev.Closing = new ClosingData { Drawer = "71000", Computer = "3050", Jazz = "32492", Easy = "11492", EasyTanzeem = "0", Oneload = "2392" };
    var next = AccountingEngine.OpeningFromPrevious(prev, true);
    Eq(74050, next.Drawer, "rollover drawer + computer");
    Eq(0, next.Computer, "rollover computer reset");
    Eq(0, next.EasyTanzeem, "rollover Easypaisa Tanzeem");
    Eq(120426, next.Total, "rollover total");
}
{
    var prev = NewDay();
    prev.Closing = new ClosingData { Drawer = "1000", Computer = "500", Jazz = "2000", Easy = "3000", EasyTanzeem = "4500", Oneload = "6000" };
    var next = AccountingEngine.OpeningFromPrevious(prev, true);
    Eq(4500, next.EasyTanzeem, "Easypaisa Tanzeem closing carries forward");
    Eq(17000, next.Total, "extended rollover total");
}
{
    Eq(10, AccountingEngine.AutoCommission(Accounts.Jazz, "out", 780, Accounts.Drawer), "send 780");
    Eq(20, AccountingEngine.AutoCommission(Accounts.Jazz, "out", 1500, Accounts.Drawer), "send 1500");
    Eq(20, AccountingEngine.AutoCommission(Accounts.Easy, "in", 500, Accounts.Drawer), "withdraw 500");
    Eq(40, AccountingEngine.AutoCommission(Accounts.Easy, "in", 1500, Accounts.Drawer), "withdraw 1500");
    Eq(40, AccountingEngine.AutoCommission(Accounts.EasyTanzeem, "in", 1500, Accounts.Drawer), "Easypaisa Tanzeem receive 1500");
}

Console.WriteLine("ALL REGRESSION CHECKS PASSED");
