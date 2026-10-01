package com.tmmobileshop.hisab;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.net.Uri;
import android.os.Bundle;
import android.webkit.JavascriptInterface;
import android.webkit.ValueCallback;
import android.webkit.WebChromeClient;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.Toast;

public class MainActivity extends Activity {
    private WebView webView;
    private SharedPreferences prefs;

    /*
     * Android-only accounting correction.
     *
     * Business rule:
     * - Home "Computer" is TODAY'S computer income.
     * - The Computer amount in a manual reference is not subtracted from today's
     *   Computer income when profit is presented.
     * - Shop profit is calculated from all non-Computer balances.
     * - Total profit = shop profit + today's Home Computer amount.
     * - index.html already carries yesterday's Computer amount into the next
     *   day's Drawer and resets next-day Computer to zero; that rule is kept.
     *
     * This is injected after the page loads so existing Preview data can be
     * migrated safely without touching the separate Windows application.
     */
    private static final String ACCOUNTING_FIX_JS = """
        (function(){
          if (window.__tmComputerProfitSemanticsV4) {
            if (typeof migrateComputerProfitSemantics === 'function') migrateComputerProfitSemantics();
            if (typeof renderAll === 'function') renderAll();
            return;
          }
          window.__tmComputerProfitSemanticsV4 = true;

          window.profitPartsForDayPatched = function(d, actual) {
            d = d || (typeof day === 'function' ? day() : null);
            actual = actual || (typeof emptyBalances === 'function' ? emptyBalances() : {});
            if (!d) return {shop:0, computer:0, total:0};
            var opening = d.opening || (typeof emptyBalances === 'function' ? emptyBalances() : {});
            var t = typeof tanzeemTotals === 'function' ? tanzeemTotals(d) : {shopTo:0,toShop:0};
            var shopOpening = balanceTotal(opening) - n(opening.computer);
            var shopActual = balanceTotal(actual) - n(actual.computer);
            var shop = shopActual - shopOpening + n(t.shopTo) - n(t.toShop);
            var computer = n(actual.computer);
            return {shop:shop, computer:computer, total:shop + computer};
          };

          window.closingProfit = function(d, actual) {
            return profitPartsForDayPatched(d, actual).total;
          };

          window.profitParts = function(actual) {
            return profitPartsForDayPatched(day(), actual);
          };

          var originalComputerIncomeForDay = window.computerIncomeForDay;
          window.computerIncomeForDay = function(d) {
            d = d || (typeof day === 'function' ? day() : null);
            if (d && d.closing && d.closing.saved && d.closing.computerProfit != null) {
              return n(d.closing.computerProfit);
            }
            return originalComputerIncomeForDay ? originalComputerIncomeForDay(d) : 0;
          };

          window.mismatchScore = function(actual, book) {
            return MONEY_ACCOUNTS
              .filter(function(k){ return k !== 'computer'; })
              .reduce(function(sum,k){ return sum + Math.abs(n(actual[k]) - n(book[k])); }, 0);
          };

          var originalSmartAudit = window.smartAudit;
          window.smartAudit = function(d, actual, book, profit) {
            var normalizedBook = clone(book || emptyBalances());
            normalizedBook.computer = n(actual && actual.computer);
            var items = originalSmartAudit ? originalSmartAudit(d, actual, normalizedBook, profit) : [];

            var detailedComputer = (d && d.transactions ? d.transactions : [])
              .filter(function(tx){ return tx.category === 'computer'; })
              .reduce(function(sum,tx){ return sum + n(tx.amount); }, 0);
            if (detailedComputer > 0 && Math.round(detailedComputer) !== Math.round(n(actual && actual.computer))) {
              items.push('کمپیوٹر کی تفصیلی آمدن ' + fmt(detailedComputer) + ' ہے مگر ہوم میں ' + fmt(actual.computer) + ' لکھی ہے۔');
            }
            return items;
          };

          window.migrateComputerProfitSemantics = function() {
            if (typeof state === 'undefined' || !state || !state.days) return;
            var changed = n(state.version) < 4;
            Object.keys(state.days).forEach(function(key){
              var d = state.days[key];
              if (!d || !d.closing || !d.closing.saved || !d.closing.actual) return;
              d.opening = Object.assign(emptyBalances(), d.opening || {});
              d.closing.actual = Object.assign(emptyBalances(), d.closing.actual || {});
              var p = profitPartsForDayPatched(d, d.closing.actual);
              if (n(d.closing.profit) !== p.total ||
                  n(d.closing.shopProfit) !== p.shop ||
                  n(d.closing.computerProfit) !== p.computer) {
                d.closing.profit = p.total;
                d.closing.shopProfit = p.shop;
                d.closing.computerProfit = p.computer;
                changed = true;
              }
            });
            state.version = 4;
            if (changed && typeof saveState === 'function') saveState();
          };

          migrateComputerProfitSemantics();

          var computerLabel = document.querySelector('#computerProfitCard .label');
          if (computerLabel) computerLabel.textContent = 'کمپیوٹر آمدن';

          if (typeof renderAll === 'function') renderAll();
        })();
        """;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        prefs = getSharedPreferences("tm_mobile_shop_store", MODE_PRIVATE);

        // Preview 10 data-safety rule:
        // Keep the exact same package and SharedPreferences store/key so an
        // in-place Android update continues to use the existing shop history.
        // Before page migration runs, keep one untouched local snapshot.
        if (!prefs.contains("state_json_backup_pre_preview10")) {
            String existingState = prefs.getString("state_json", "");
            if (existingState != null && !existingState.isEmpty()) {
                prefs.edit()
                    .putString("state_json_backup_pre_preview10", existingState)
                    .apply();
            }
        }

        // One more untouched snapshot before the reconciliation/party update.
        // This does not replace or rewrite the live state_json.
        if (!prefs.contains("state_json_backup_pre_reconcile")) {
            String existingState = prefs.getString("state_json", "");
            if (existingState != null && !existingState.isEmpty()) {
                prefs.edit()
                    .putString("state_json_backup_pre_reconcile", existingState)
                    .apply();
            }
        }

        webView = new WebView(this);
        webView.setBackgroundColor(0xffeffbf8);
        WebSettings s = webView.getSettings();
        s.setJavaScriptEnabled(true);
        s.setDomStorageEnabled(true);
        s.setAllowFileAccess(true);
        s.setAllowContentAccess(false);
        s.setDatabaseEnabled(true);
        s.setBuiltInZoomControls(false);
        s.setDisplayZoomControls(false);
        s.setTextZoom(100);

        webView.addJavascriptInterface(new StoreBridge(this), "AndroidStore");
        webView.setWebChromeClient(new WebChromeClient());
        webView.setWebViewClient(new WebViewClient() {
            @Override
            public boolean shouldOverrideUrlLoading(WebView view, String url) {
                if (url != null && (url.startsWith("http://") || url.startsWith("https://"))) {
                    try { startActivity(new Intent(Intent.ACTION_VIEW, Uri.parse(url))); } catch (Exception ignored) {}
                    return true;
                }
                return false;
            }

            @Override
            public void onPageFinished(WebView view, String url) {
                super.onPageFinished(view, url);
                if (url != null && url.endsWith("/index.html")) {
                    view.evaluateJavascript(ACCOUNTING_FIX_JS, null);
                }
            }
        });
        setContentView(webView);
        webView.loadUrl("file:///android_asset/index.html");
    }

    @Override
    public void onBackPressed() {
        if (webView == null) {
            super.onBackPressed();
            return;
        }
        webView.evaluateJavascript("(window.appBack?window.appBack():false)", new ValueCallback<String>() {
            @Override public void onReceiveValue(String value) {
                if (!"true".equals(value)) MainActivity.super.onBackPressed();
            }
        });
    }

    public class StoreBridge {
        private final Context context;
        StoreBridge(Context context) { this.context = context; }

        @JavascriptInterface
        public String loadState() {
            return prefs.getString("state_json", "");
        }

        @JavascriptInterface
        public void saveState(String json) {
            if (json == null) return;
            prefs.edit().putString("state_json", json).apply();
        }

        @JavascriptInterface
        public void toast(final String text) {
            runOnUiThread(() -> Toast.makeText(context, text, Toast.LENGTH_SHORT).show());
        }

        @JavascriptInterface
        public void shareText(final String title, final String text) {
            runOnUiThread(() -> {
                Intent send = new Intent(Intent.ACTION_SEND);
                send.setType("text/plain");
                send.putExtra(Intent.EXTRA_SUBJECT, title);
                send.putExtra(Intent.EXTRA_TEXT, text);
                startActivity(Intent.createChooser(send, title));
            });
        }

        @JavascriptInterface
        public void shareWhatsApp(final String text) {
            runOnUiThread(() -> {
                Intent send = new Intent(Intent.ACTION_SEND);
                send.setType("text/plain");
                send.putExtra(Intent.EXTRA_TEXT, text);
                send.setPackage("com.whatsapp");
                try {
                    startActivity(send);
                } catch (Exception e) {
                    send.setPackage(null);
                    startActivity(Intent.createChooser(send, "رپورٹ Share کریں"));
                }
            });
        }
    }
}
