## Context

在既有的四種觸發機制之外，新增「螢幕配置變更自動還原」。核心零件多已存在：`ScreenSignature.Capture()`（螢幕配置簽章）、`LayoutRestoreService.RestoreAll`（含 `ScreenClamp` 離屏夾限）、以及上一版為打斷自我回授迴圈加入的 `RestoreCooldownTracker`。本變更主要是新增一個事件來源並接線。

## Goals / Non-Goals

**Goals:**
- 螢幕插拔 / 解析度 / 多螢幕排列改變時，自動把視窗還原到記錄位置。
- 與既有觸發並列、可獨立開關、預設關閉、設定持久化。
- 不引入新的回授迴圈（還原搬窗本身不可再次觸發本機制成為無限迴圈）。

**Non-Goals:**
- 不做「依不同螢幕配置記住不同佈局」（那屬於未來的多情境佈局；本變更只在配置變更時還原「目前單一套佈局」）。
- 不處理 DPI 變更以外的顯示事件（如色彩設定）。

## Decisions

### 事件來源：`SystemEvents.DisplaySettingsChanged`
- 採 `Microsoft.Win32.SystemEvents.DisplaySettingsChanged`（WinForms 已引用，免額外相依），可靠涵蓋解析度 / 螢幕數量 / 排列變更。
- 替代方案：在現有 `MessageWindow` 監聽 `WM_DISPLAYCHANGE`。較底層但需自管訊息；`SystemEvents` 較簡潔，故為首選。
- 注意：`SystemEvents` 事件可能在背景執行緒觸發，需用 `Dispatcher` 切回 UI 執行緒（比照 `WindowEventWatcher`）。

### 去抖 + 簽章比對，避免空跑與抖動
- 配置變更常連發多次事件（插拔過程），以 `DispatcherTimer` 去抖（預設約 800ms，比視窗事件略長，等配置穩定）。
- 去抖後 `ScreenSignature.Capture()` 與上次記錄的簽章比較：**相同則不動作**；不同才觸發還原並更新記錄簽章。
- 理由：避免同一次插拔重複還原，也避免事件誤報造成不必要的搬窗。

### 重用還原管線與冷卻機制
- 觸發時呼叫既有 `RestoreNow(silent: true)` → `RestoreAll`，沿用離屏夾限與匹配評分。
- `RestoreAll` 已將還原成功的視窗 `Mark` 進 `RestoreCooldownTracker`，因此即使同時開著「新視窗自動還原」，被搬動的視窗在冷卻期內不會反過來再觸發單窗還原，不會形成迴圈。
- 本機制自身不靠視窗顯示事件觸發，靠的是顯示設定事件，所以「還原搬窗」不會回頭觸發本機制；無新增迴圈。

### 設定與開關
- `AppSettings` 新增 `DisplayChangeRestoreEnabled`（預設 `false`）。
- `AppController.ApplySettings` 依此 `SetEnabled` 啟停 `DisplayChangeWatcher`，與其他觸發一致。
- 設定 UI 新增一個勾選項。

## Risks / Trade-offs

- [事件在非 UI 執行緒] `SystemEvents` 回呼可能在其他執行緒 → 一律 `Dispatcher.BeginInvoke` 切回，且記得在 Dispose 時取消訂閱（`SystemEvents` 為靜態事件，未取消會洩漏）。
- [去抖時間取捨] 太短會在插拔過程多次觸發、太長則使用者等待感明顯 → 預設 800ms，必要時可設定化。
- [簽章過鬆/過嚴] 沿用既有 `ScreenSignature`（螢幕數量 + 各工作區），與既有還原一致；離屏夾限作為最終安全網。
- [喚醒/休眠] 部分情境喚醒會發 display 事件 → 視為一次配置確認，簽章未變則不動作，行為安全。

## Migration Plan

- `settings.json` 僅新增一個布林欄位，舊檔缺欄位時以預設（false）讀取，無遷移需求。
- 回滾：移除/關閉該開關即停用；不影響其他觸發與既有佈局資料。
