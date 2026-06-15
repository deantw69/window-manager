## 1. 事件來源：DisplayChangeWatcher

- [x] 1.1 新增 `Triggers/DisplayChangeWatcher`：訂閱 `SystemEvents.DisplaySettingsChanged`，提供 `SetEnabled(bool)`，事件以 `Dispatcher.BeginInvoke` 切回 UI 執行緒
- [x] 1.2 去抖處理（`DispatcherTimer`，預設約 800ms），去抖後比對前後 `ScreenSignature`，僅簽章改變時觸發 `DisplayChanged` 事件（判定抽成 `DisplayChangeDetector`）
- [x] 1.3 Dispose 時取消訂閱靜態 `SystemEvents` 事件並停止計時器（避免洩漏）

## 2. 設定與接線

- [x] 2.1 `AppSettings` 新增 `DisplayChangeRestoreEnabled`（預設 `false`）
- [x] 2.2 `AppController` 建立並接線 `DisplayChangeWatcher`，`DisplayChanged` → `RestoreNow(silent: true)`
- [x] 2.3 `AppController.ApplySettings` 依設定 `SetEnabled` 啟停（與既有觸發一致）
- [x] 2.4 確認還原重用 `RestoreAll` + `RestoreCooldownTracker`，不與「新視窗自動還原」互相重觸發

## 3. 設定 UI

- [x] 3.1 設定視窗新增「螢幕配置變更時自動還原」勾選項，綁定 `DisplayChangeRestoreEnabled`
- [x] 3.2 README 功能說明補上第五種觸發機制

## 4. 測試與驗證

- [x] 4.1 單元測試：去抖後「簽章相同不觸發、簽章不同才觸發」的判定邏輯（`DisplayChangeDetectorTests`，5 個）
- [x] 4.2 單元測試：開關持久化（settings 往返、舊檔缺欄位以預設讀取）
- [ ] 4.3 實機驗證：插拔外接螢幕 / 切換解析度後自動還原；與「新視窗自動還原」同開時不產生迴圈（待實機）
