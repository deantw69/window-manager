## Why

筆電插拔外接螢幕、切換解析度、或喚醒後螢幕配置改變時，視窗最容易跑位或被丟到不再存在的座標。目前的觸發機制（快捷鍵、定時儲存、新視窗還原、開機還原）都無法針對「螢幕配置變動」這個最關鍵的時機自動反應——使用者得手動按還原鍵。

本變更新增第五種觸發機制：**偵測到螢幕配置變更時自動還原佈局**。這比「新視窗自動還原」更貼近真實痛點，且重用既有的還原管線（`RestoreAll` + `ScreenClamp` 離屏夾限）與冷卻機制（`RestoreCooldownTracker`），實作風險低。

## What Changes

- 在常駐程式新增監聽螢幕配置變更的能力（`SystemEvents.DisplaySettingsChanged` / `WM_DISPLAYCHANGE`）。
- 事件去抖（debounce）後，比對前後 `ScreenSignature`；確實改變才觸發一次「還原目前佈局」。
- 新增一個可獨立開關的觸發設定（預設關閉），與既有四種觸發並列、互不影響、開關持久化。
- 還原動作重用既有 `LayoutRestoreService.RestoreAll` 與 `RestoreCooldownTracker`，避免與「新視窗自動還原」互相重觸發。

## Capabilities

### New Capabilities
<!-- 無新增 capability -->

### Modified Capabilities
- `restore-triggers`: 新增第五種觸發機制「螢幕配置變更自動還原」，並將「觸發機制獨立開關」需求由四種擴充為涵蓋本機制。

## Impact

- 受影響程式：`AppController`（接線新觸發）、新增 `Triggers/DisplayChangeWatcher`、`AppSettings`（新增開關欄位 `DisplayChangeRestoreEnabled`）、設定 UI（新增勾選項）。
- 重用既有：`LayoutRestoreService.RestoreAll`、`ScreenClamp`、`ScreenSignature`、`RestoreCooldownTracker`。
- 技術依賴：`Microsoft.Win32.SystemEvents.DisplaySettingsChanged`（已透過 WinForms 引用可用）或在現有 `MessageWindow` 監聽 `WM_DISPLAYCHANGE`。
- 設定檔：`settings.json` 新增一個布林欄位；舊檔缺欄位時以預設（關閉）讀取，無需遷移。
- 預設關閉，不改變既有使用者行為。
