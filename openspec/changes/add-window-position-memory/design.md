## Context

全新專案，目標是 Windows 平台上的常駐視窗管理工具，記憶並還原應用程式視窗的位置/大小，解決重開程式或重開機後視窗跑位、多螢幕離屏的問題。技術選型已定：C# / .NET（WPF + Win32 API），系統列常駐。需求涵蓋四種辨識依據（執行檔路徑、標題模糊比對、視窗類別名、螢幕配置）與四種觸發機制（快捷鍵、定時儲存、新視窗還原、開機還原），且每項皆可開關、快捷鍵可自訂。

## Goals / Non-Goals

**Goals:**
- 穩定辨識同一應用程式視窗，並在指定時機自動還原位置/大小/狀態。
- 多螢幕安全：螢幕配置變動時不讓視窗落到不可見區域。
- 持久化佈局與設定，重開機後可用。
- 各觸發機制可獨立開關、快捷鍵可自訂，設定 UI 友善。

**Non-Goals:**
- 不做跨機器同步 / 雲端備份。
- 不做平鋪式 / 自動排版 window tiling（如 i3 風格的自動分割）。
- 不支援 UWP / 沙箱化或需提權才能操作的視窗（盡力而為，失敗則略過）。
- 不做虛擬桌面（virtual desktop）佈局管理（首版範圍外）。

## Decisions

### 技術堆疊：.NET (WPF) + Win32 P/Invoke
- 採用 .NET（建議 .NET 8 LTS）+ WPF 做設定 UI 與系統列，核心視窗操作以 P/Invoke 呼叫 user32.dll。
- 系統列圖示：WPF 無內建 `NotifyIcon`，採用 `H.NotifyIcon`（WPF 友善）或 Windows Forms 的 `NotifyIcon`（加入 `System.Windows.Forms` 參考）。傾向後者以減少額外相依。
- 替代方案：WinUI 3 較新但系統列與全域熱鍵整合較繁瑣；WinForms 純做 UI 較陽春。WPF 取得 UI 彈性與成熟生態的平衡。

### 視窗列舉與操作 API
- 列舉：`EnumWindows` + 過濾（可見、具標題列、非工具視窗、有標題文字）。
- 識別：`GetWindowThreadProcessId` → `OpenProcess` + `QueryFullProcessImageName` 取執行檔路徑；`GetClassName` 取類別名；`GetWindowText` 取標題。
- 位置：`GetWindowPlacement`（取得含最大化/最小化狀態與還原座標）優於單純 `GetWindowRect`；還原用 `SetWindowPlacement` 或 `SetWindowPos`。
- 多螢幕：以 .NET `System.Windows.Forms.Screen.AllScreens` 取得各螢幕工作區，組成螢幕配置簽章與夾限計算。

### 視窗識別與比對策略
- 主要鍵：執行檔路徑 + 視窗類別名（穩定）。次要：標題模糊比對（忽略大小寫、子字串 + 相似度如 Levenshtein 比例）。
- 螢幕配置簽章一致時加分；比對採加權分數 + 門檻，超過門檻才視為匹配，多候選取最高分。
- 理由：純標題易變（檔名、進度），純 process 無法區分同程式多視窗；綜合評分兼顧穩定與區辨。

### 持久化格式與位置
- JSON 檔，含 `schemaVersion`，存於 `%AppData%\WindowManager\`（`layouts.json`、`settings.json`）。
- 原子寫入：寫 `*.tmp` → `File.Replace`/`Move` 置換，避免毀損。
- 理由：JSON 易讀易除錯、版本欄位利於未來遷移。

### 全域熱鍵
- 使用 `RegisterHotKey`（搭配隱藏訊息視窗 / WPF `HwndSource` 處理 `WM_HOTKEY`）。
- 自訂熱鍵：設定變更時先 `UnregisterHotKey` 再重新註冊；註冊失敗（衝突）時回滾並提示。

### 新視窗偵測
- 採用 `SetWinEventHook`（`EVENT_OBJECT_SHOW` / `EVENT_SYSTEM_FOREGROUND`）監聽新視窗事件，較輪詢省資源；事件去抖（debounce）後比對與還原。
- 替代方案：定時輪詢 `EnumWindows`，實作簡單但較耗資源、延遲明顯，作為後備。

### 開機自動啟動
- 寫入 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`（免提權、僅當前使用者），開關即新增/移除機碼。

## Risks / Trade-offs

- [提權視窗無法操作] 以系統管理員身分執行的視窗，一般權限程式無法 `SetWindowPos` → 略過並記錄；必要時提示使用者以系統管理員執行本工具。
- [標題模糊比對誤判] 同程式多視窗可能配錯 → 以門檻 + 多鍵加權降低，並允許排除清單與門檻調整。
- [WinEvent 事件風暴] 視窗頻繁顯示造成大量事件 → debounce + 僅處理頂層具標題視窗。
- [螢幕配置簽章過嚴/過鬆] 過嚴則常觸發夾限、過鬆則錯放 → 簽章以螢幕數量+各螢幕工作區為準，並一律做離屏夾限作為安全網。
- [熱鍵衝突] 與其他軟體衝突導致註冊失敗 → 回滾舊設定並於 UI 明確提示。

## Migration Plan

- 全新專案，無資料遷移。佈局/設定檔以 `schemaVersion` 標示，未來格式變更時依版本做轉換或重建。
- 回滾策略：本工具為使用者層級常駐程式，移除開機啟動項並結束程式即可完全停用，不影響系統。

## Resolved Decisions

- 受管理範圍：**預設管理所有一般視窗 + 排除清單**（不採白名單）。
- 多套佈局 / 情境（profiles）：**首版不做，只維護單一套佈局**；資料結構保留 `schemaVersion` 以利日後擴充。
- 最小化視窗：**記錄並還原最小化狀態**（連同還原座標一起記，取消最小化時回到正確位置）。
