## 1. 專案骨架

- [x] 1.1 建立 .NET 8 WPF 專案（系統列常駐應用），設定為單一實例執行
- [x] 1.2 加入相依：`System.Windows.Forms`（NotifyIcon / Screen）、JSON 序列化（System.Text.Json）
- [x] 1.3 建立基本資料夾結構（Interop、Core、Persistence、Triggers、UI）
- [x] 1.4 設定 app manifest（DPI 感知 per-monitor v2、可選的 requestedExecutionLevel）

## 2. Win32 Interop 層

- [x] 2.1 P/Invoke 宣告：`EnumWindows`、`GetWindowText`、`GetClassName`、`GetWindowThreadProcessId`
- [x] 2.2 P/Invoke 宣告：`GetWindowPlacement`、`SetWindowPlacement`、`SetWindowPos`、`IsWindowVisible`
- [x] 2.3 取執行檔路徑：`OpenProcess` + `QueryFullProcessImageName`（含權限失敗的安全處理）
- [x] 2.4 視窗列舉與過濾工具（可見、具標題列、排除工具視窗）

## 3. 視窗識別與比對（window-identity）

- [x] 3.1 定義視窗識別資料模型（路徑、標題、類別名、PID、螢幕配置簽章）
- [x] 3.2 實作螢幕配置簽章（依 `Screen.AllScreens` 數量/工作區）
- [x] 3.3 實作比對評分（路徑+類別名主鍵、標題模糊比對、螢幕簽章加分、門檻判定）
- [x] 3.4 實作排除清單（依路徑/類別名）並套用於擷取與還原
- [x] 3.5 單元測試：唯一匹配、同程式多視窗、無匹配、排除（WindowMatcherTests）

## 4. 佈局擷取與持久化（layout-persistence）

- [x] 4.1 定義 `WindowLayout` / `LayoutSet` 資料模型（位置、大小、視窗狀態、識別資料、schemaVersion）
- [x] 4.2 實作擷取：列舉視窗 → 取 placement → 組成 LayoutSet
- [x] 4.3 實作原子寫入（tmp → File.Replace）至 `%AppData%\WindowManager\layouts.json`
- [x] 4.4 實作讀取與版本/毀損處理（毀損則保留原檔、以空佈局繼續）
- [x] 4.5 單元測試：序列化往返、原子寫入、毀損/版本不符的降級（PersistenceTests）
- [x] 4.6 累加記憶：upsert 合併（路徑+類別名+標題為鍵）、保留未開啟視窗、上限淘汰、清除記錄（LayoutMerger + LayoutMergerTests，並經真實視窗驗證）

## 5. 位置還原（position-restore）

- [x] 5.1 實作套用位置/大小/狀態（一般、最大化先設還原座標再最大化、最小化）
- [x] 5.2 實作多螢幕安全：離屏偵測與夾限到最近可見工作區
- [x] 5.3 實作略過/失敗處理（無匹配、視窗已關閉、SetWindowPos 失敗）與結果記錄
- [x] 5.4 單元/整合測試：離屏夾限（ScreenAndModelTests）+ 端到端移動後還原驗證

## 6. 觸發機制（restore-triggers）

- [x] 6.1 全域熱鍵：`RegisterHotKey` + `HwndSource` 處理 `WM_HOTKEY`，儲存/還原各一組
- [x] 6.2 自訂熱鍵：變更時 Unregister/Register、衝突回滾與提示
- [x] 6.3 自動定時儲存（可開關、可設間隔，DispatcherTimer）
- [x] 6.4 新視窗自動還原：`SetWinEventHook`（SHOW）+ debounce + 比對還原
- [x] 6.5 開機/登入後自動還原：啟動讀取佈局後依開關套用
- [x] 6.6 四機制獨立開關狀態的讀取/套用/持久化
- [~] 6.7 測試：已實機驗證快捷鍵儲存/還原、新視窗自動還原、設定持久化與自訂快捷鍵生效；熱鍵衝突回滾經 Ctrl+Alt+R 實例佐證；定時器啟停的自動化測試待補

## 7. 系統列與設定 UI（tray-app）

- [x] 7.1 NotifyIcon 系統列圖示與右鍵選單（立即儲存/還原、設定、結束）
- [x] 7.2 設定資料模型與持久化（`settings.json`，原子寫入）
- [x] 7.3 設定視窗：各觸發機制開關、儲存/還原快捷鍵、定時間隔、排除清單、開機自動啟動
- [x] 7.4 設定變更即時生效（重註冊熱鍵、啟停定時器、套用排除清單）
- [x] 7.5 開機自動啟動：寫入/移除 `HKCU\...\Run`
- [x] 7.6 結束時清理（Unregister 熱鍵、移除 WinEventHook、釋放 NotifyIcon）

## 8. 整合與驗證

- [~] 8.1 端到端測試：已驗證快捷鍵「儲存→移動→還原」、新視窗自動還原（記事本，皆 PASS）；重開機/登入還原待手動驗證
- [ ] 8.2 多螢幕情境測試：拔除外接螢幕後的離屏夾限
- [ ] 8.3 提權視窗略過行為驗證與記錄檢視
- [x] 8.4 撰寫 README（安裝、設定、已知限制）
