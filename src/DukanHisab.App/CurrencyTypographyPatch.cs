using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DukanHisab.App;

internal static class CurrencyTypographyPatch
{
    private const string CurrencyMarker = " روپے";
    private static readonly FontFamily NumericFont = new("Segoe UI");
    private static readonly FontFamily UrduFont = new("Jameel Noori Nastaleeq");
    private static readonly DependencyPropertyDescriptor TextDescriptor =
        DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock))!;
    private static readonly HashSet<TextBlock> AttachedBlocks = [];
    private static readonly HashSet<TextBlock> UpdatingBlocks = [];

    public static void AttachFor(MainWindow window)
    {
        Attach(
            window.DashDrawer,
            window.DashJazz,
            window.DashEasy,
            window.DashOneLoad,
            window.DashComputer,
            window.DashCurrentTotal,
            window.DashLiveProfit,
            window.DashClosingProfit,
            window.DashTIn,
            window.DashTOut,
            window.DashJazzCommission,
            window.DashEasyCommission,
            window.DashTanzeemCommission,
            window.DashAccessories,
            window.DashUnexplained,
            window.AccountOpening,
            window.AccountCurrent,
            window.TInTotal,
            window.TOutTotal,
            window.AccSales,
            window.AccReturns,
            window.AccNet,
            window.CloseOpenComputer,
            window.CloseBookComputer,
            window.CloseOpenDrawer,
            window.CloseBookDrawer,
            window.CloseOpenJazz,
            window.CloseBookJazz,
            window.CloseOpenEasy,
            window.CloseBookEasy,
            window.CloseOpenOneLoad,
            window.CloseBookOneLoad,
            window.CloseOpeningTotal,
            window.CloseTIn,
            window.CloseTOut,
            window.CloseActualTotal,
            window.CloseProfit);
    }

    private static void Attach(params TextBlock[] blocks)
    {
        foreach (var block in blocks)
        {
            if (!AttachedBlocks.Add(block)) continue;
            TextDescriptor.AddValueChanged(block, OnTextChanged);
            Apply(block);
        }
    }

    private static void OnTextChanged(object? sender, EventArgs e)
    {
        if (sender is TextBlock block) Apply(block);
    }

    private static void Apply(TextBlock block)
    {
        if (UpdatingBlocks.Contains(block)) return;

        var text = block.Text;
        var markerIndex = text.IndexOf(CurrencyMarker, StringComparison.Ordinal);
        if (markerIndex < 0) return;

        var amountText = text[..markerIndex];
        var suffixText = text[(markerIndex + CurrencyMarker.Length)..];

        UpdatingBlocks.Add(block);
        try
        {
            block.Inlines.Clear();

            if (amountText.Length > 0)
                block.Inlines.Add(new Run(amountText) { FontFamily = NumericFont });

            block.Inlines.Add(new Run(CurrencyMarker) { FontFamily = UrduFont });

            if (suffixText.Length > 0)
                block.Inlines.Add(new Run(suffixText) { FontFamily = UrduFont });
        }
        finally
        {
            UpdatingBlocks.Remove(block);
        }
    }
}
