from pathlib import Path
import subprocess

BASE_BLOB = "e2e126c44b69df66aad7f23cdcce276eb8b3ff76"  # Minimal Preview 4 index.html
path = Path("app/src/main/assets/index.html")

# Recover the last known-good Minimal Preview 4 UI directly from git history.
s = subprocess.check_output(["git", "cat-file", "-p", BASE_BLOB], text=True)
orig = s

def replace_once(old, new, label):
    global s
    if old not in s:
        raise SystemExit(f"repair failed: {label} anchor not found")
    s = s.replace(old, new, 1)

# Custom account picker styles: avoids Android native <select> popup so Jameel font remains active.
needle = ".detailTabs{display:grid;grid-template-columns:1fr 1fr;gap:9px;margin:8px 0}.detailTab{border:2px solid #d6dfdb;background:#f8fcfa;border-radius:17px;padding:9px}.detailTab.active{background:#e7f3ed;border-color:#8bb5a4;color:#0c5c40;font-weight:700}\n"
replace_once(needle, needle + ".selectBtn{width:100%;border:1.5px solid #d5dfdb;background:#fbfefd;border-radius:16px;padding:9px 13px;text-align:right;font-family:'Jameel Noori Nastaleeq','Noto Nastaliq Urdu',serif;font-size:17px;min-height:55px}.pickerList{display:grid;gap:8px;margin-top:10px}.pickerOption{width:100%;border:1.5px solid #d7e0dc;background:#f9fdfb;border-radius:17px;padding:11px 14px;text-align:right;font-family:'Jameel Noori Nastaleeq','Noto Nastaliq Urdu',serif;font-size:19px;color:#153f32}.pickerOption.active{background:#e6f3ed;border-color:#72aa94;color:#0b583e;font-weight:700}.pickerOption:active{transform:scale(.99)}\n", "picker css")

replace_once('<div class="field"><label>دکان کا اکاؤنٹ</label><select id="tAccount"></select></div>', '<div class="field"><label>دکان کا اکاؤنٹ</label><button type="button" class="selectBtn" id="tAccountBtn" onclick="openAccountPicker(\'tAccount\')">دوسرے گلے کی کیش</button><input type="hidden" id="tAccount" value="drawer"></div>', "tanzeem selector")
replace_once('<div class="field"><label>دوسری طرف</label><select id="detailCounter" onchange="refreshDetailCommission()"></select></div>', '<div class="field"><label>دوسری طرف</label><button type="button" class="selectBtn" id="detailCounterBtn" onclick="openAccountPicker(\'detailCounter\')">دوسرے گلے کی کیش</button><input type="hidden" id="detailCounter" value="drawer"></div>', "detail selector")

needle = '<div class="overlay" id="auditOverlay"><div class="modal"><div class="modalHead"><h2>Smart Closing Check</h2><button class="closeX" onclick="closeOverlay(\'auditOverlay\')">×</button></div><div id="auditContent"></div><div class="modalActions"><button class="softBtn" onclick="closeOverlay(\'auditOverlay\')">دوبارہ دیکھیں</button><button class="mainBtn" onclick="confirmSaveClosing()">پھر بھی محفوظ کریں</button></div></div></div>\n'
replace_once(needle, needle + '\n<div class="overlay" id="accountPickerOverlay"><div class="modal"><div class="modalHead"><h2 id="accountPickerTitle">اکاؤنٹ منتخب کریں</h2><button class="closeX" onclick="closeOverlay(\'accountPickerOverlay\')">×</button></div><div class="pickerList" id="accountPickerList"></div></div></div>\n', "picker overlay")

replace_once("let state={version:2,days:{}};let currentPage='home';let currentDetailAccount='jazz';let detailDirection='receive';let tanzeemFlow='shop_to_tanzeem';let pendingClosing=null;", "let state={version:2,days:{}};let currentPage='home';let currentDetailAccount='jazz';let detailDirection='receive';let tanzeemFlow='shop_to_tanzeem';let pendingClosing=null;let accountPickerTarget=null;", "picker state")

# Profit split fix: opening/reference computer cash is NOT today's computer profit.
replace_once("function profitParts(actual){const d=day(),total=closingProfit(d,actual),computer=n(actual.computer)-n(d.opening.computer),shop=total-computer;return{shop,computer,total}}", "function computerIncomeForDay(d=day()){return(d.transactions||[]).filter(tx=>tx.category==='computer').reduce((sum,tx)=>sum+n(tx.amount),0)}\nfunction profitParts(actual){const d=day(),total=closingProfit(d,actual),computer=computerIncomeForDay(d),shop=total-computer;return{shop,computer,total}}", "profit split")

old_fill = "function fillSelects(){const options=ACCOUNTS.filter(x=>x[0]!=='computer').map(([k,l])=>`<option value=\"${k}\">${l}</option>`).join('');const t=document.getElementById('tAccount');if(t&&!t.options.length)t.innerHTML=options;const c=document.getElementById('detailCounter');if(c&&!c.options.length)c.innerHTML=ACCOUNTS.map(([k,l])=>`<option value=\"${k}\">${l}</option>`).join('')}"
new_fill = "function pickerAccounts(target){return target==='tAccount'?ACCOUNTS.filter(x=>x[0]!=='computer'):ACCOUNTS}\nfunction syncPickerButton(target){const input=document.getElementById(target),btn=document.getElementById(target+'Btn');if(input&&btn)btn.textContent=accountName(input.value||'drawer')}\nfunction fillSelects(){for(const target of ['tAccount','detailCounter']){const input=document.getElementById(target);if(input&&!input.value)input.value='drawer';syncPickerButton(target)}}\nfunction openAccountPicker(target){accountPickerTarget=target;const input=document.getElementById(target),current=input?input.value:'drawer';document.getElementById('accountPickerTitle').textContent=target==='tAccount'?'دکان کا اکاؤنٹ منتخب کریں':'دوسری طرف منتخب کریں';document.getElementById('accountPickerList').innerHTML=pickerAccounts(target).map(([k,l])=>`<button type=\"button\" class=\"pickerOption ${k===current?'active':''}\" onclick=\"chooseAccount('${k}')\">${l}</button>`).join('');openOverlay('accountPickerOverlay')}\nfunction chooseAccount(value){if(!accountPickerTarget)return;const input=document.getElementById(accountPickerTarget);if(input)input.value=value;syncPickerButton(accountPickerTarget);const target=accountPickerTarget;accountPickerTarget=null;closeOverlay('accountPickerOverlay');if(target==='detailCounter')refreshDetailCommission()}"
replace_once(old_fill, new_fill, "fill selectors")

old_open = "function openDetail(account){currentDetailAccount=account;detailDirection='receive';document.getElementById('detailTitle').textContent=accountName(account)+' — مکمل تفصیل';document.getElementById('detailAmount').value='';document.getElementById('detailCommission').value='';document.getElementById('detailNote').value='';setDetailDirection('receive');const c=document.getElementById('detailCounter');if(c)c.value='drawer';renderDetailList();refreshDetailCommission();openOverlay('detailOverlay')}"
new_open = "function openDetail(account){currentDetailAccount=account;detailDirection='receive';document.getElementById('detailTitle').textContent=accountName(account)+' — مکمل تفصیل';document.getElementById('detailAmount').value='';document.getElementById('detailCommission').value='';document.getElementById('detailNote').value='';setDetailDirection('receive');const c=document.getElementById('detailCounter');if(c)c.value='drawer';syncPickerButton('detailCounter');renderDetailList();refreshDetailCommission();openOverlay('detailOverlay')}"
replace_once(old_open, new_open, "open detail")

old_comp = "function saveComputerIncome(){const amount=n(document.getElementById('computerIncome').value);if(amount<=0)return toast('رقم درج کریں');const note=document.getElementById('computerNote').value.trim();day().transactions.push({id:'t'+Date.now(),time:nowTime(),category:'computer',amount,note});day().audit.push({at:nowTime(),kind:'computer',summary:'کمپیوٹر آمدن '+fmt(amount)});saveState();closeOverlay('computerOverlay');toast('کمپیوٹر آمدن محفوظ ہوگئی')}"
new_comp = "function saveComputerIncome(){const amount=n(document.getElementById('computerIncome').value);if(amount<=0)return toast('رقم درج کریں');const note=document.getElementById('computerNote').value.trim();day().transactions.push({id:'t'+Date.now(),time:nowTime(),category:'computer',amount,note});day().audit.push({at:nowTime(),kind:'computer',summary:'کمپیوٹر آمدن '+fmt(amount)});saveState();closeOverlay('computerOverlay');updateProfitPreview();toast('کمپیوٹر آمدن محفوظ ہوگئی')}"
replace_once(old_comp, new_comp, "computer refresh")

old_hist = "function renderHistory(){const list=Object.values(state.days).filter(d=>d.closing&&d.closing.saved).sort((a,b)=>b.date.localeCompare(a.date));document.getElementById('historyList').innerHTML=list.length?list.map(d=>{const c=d.closing,p=n(c.profit),cp=c.computerProfit!=null?n(c.computerProfit):n(c.actual&&c.actual.computer)-n(d.opening&&d.opening.computer),sp=c.shopProfit!=null?n(c.shopProfit):p-cp;return `<div class=\"historyItem\"><div class=\"histHead\"><b>${urduDate(d.date)}</b><span class=\"histProfit\" style=\"color:${p<0?'var(--bad)':'var(--good)'}\">${p<0?'-':''}${fmt(Math.abs(p))}</span></div><div class=\"meta\">دکان ${fmt(sp)} • کمپیوٹر ${fmt(cp)} • کل موجود ${fmt(balanceTotal(c.actual||emptyBalances()))}</div><div class=\"histActions\"><button class=\"softBtn\" onclick=\"shareDayReport('${d.date}')\">رپورٹ</button></div></div>`}).join(''):'<div class=\"empty\">ابھی کوئی حساب محفوظ نہیں</div>'}"
new_hist = "function renderHistory(){const list=Object.values(state.days).filter(d=>d.closing&&d.closing.saved).sort((a,b)=>b.date.localeCompare(a.date));document.getElementById('historyList').innerHTML=list.length?list.map(d=>{const c=d.closing,p=n(c.profit),cp=computerIncomeForDay(d),sp=p-cp;return `<div class=\"historyItem\"><div class=\"histHead\"><b>${urduDate(d.date)}</b><span class=\"histProfit\" style=\"color:${p<0?'var(--bad)':'var(--good)'}\">${p<0?'-':''}${fmt(Math.abs(p))}</span></div><div class=\"meta\">دکان ${fmt(sp)} • کمپیوٹر ${fmt(cp)} • کل موجود ${fmt(balanceTotal(c.actual||emptyBalances()))}</div><div class=\"histActions\"><button class=\"softBtn\" onclick=\"shareDayReport('${d.date}')\">رپورٹ</button></div></div>`}).join(''):'<div class=\"empty\">ابھی کوئی حساب محفوظ نہیں</div>'}"
replace_once(old_hist, new_hist, "history split")

old_report = "function reportText(d){const c=d.closing&&d.closing.saved?d.closing:null,actual=c?c.actual:emptyBalances(),t=tanzeemTotals(d),p=c?n(c.profit):0,cp=c&&c.computerProfit!=null?n(c.computerProfit):n(actual.computer)-n(d.opening.computer),sp=c&&c.shopProfit!=null?n(c.shopProfit):p-cp;let s=`TM Mobile Shop\\n${urduDate(d.date)}\\n\\n`;for(const [k,l] of ACCOUNTS)s+=`${l}: ${fmt(actual[k])} روپے\\n`;s+=`\\nدکان سے تنظم: ${fmt(t.shopTo)} روپے\\nتنظم سے دکان: ${fmt(t.toShop)} روپے\\n\\nدکان کا منافع: ${fmt(sp)} روپے\\nکمپیوٹر: ${fmt(cp)} روپے\\nٹوٹل منافع: ${fmt(p)} روپے\\n`;return s}"
new_report = "function reportText(d){const c=d.closing&&d.closing.saved?d.closing:null,actual=c?c.actual:emptyBalances(),t=tanzeemTotals(d),p=c?n(c.profit):0,cp=computerIncomeForDay(d),sp=p-cp;let s=`TM Mobile Shop\\n${urduDate(d.date)}\\n\\n`;for(const [k,l] of ACCOUNTS)s+=`${l}: ${fmt(actual[k])} روپے\\n`;s+=`\\nدکان سے تنظم: ${fmt(t.shopTo)} روپے\\nتنظم سے دکان: ${fmt(t.toShop)} روپے\\n\\nدکان کا منافع: ${fmt(sp)} روپے\\nکمپیوٹر: ${fmt(cp)} روپے\\nٹوٹل منافع: ${fmt(p)} روپے\\n`;return s}"
replace_once(old_report, new_report, "report split")

if s == orig:
    raise SystemExit("repair made no changes")
if "<select" in s:
    raise SystemExit("native select remains")
if "computer=n(actual.computer)-n(d.opening.computer)" in s:
    raise SystemExit("old computer profit formula remains")
path.write_text(s, encoding="utf-8")
print("Android UI repaired for Preview 5")
