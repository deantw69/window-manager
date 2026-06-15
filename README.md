# 視窗管理員 (Window Manager)

Windows 用的常駐視窗管理工具，記憶各應用程式視窗的位置/大小，並在指定時機自動還原，解決「重開程式」或「重開機/登入」後視窗跑位、多螢幕離屏的問題。

## 功能

- **綜合辨識視窗**：以執行檔路徑 + 視窗類別名為主鍵、視窗標題模糊比對為加權、螢幕配置簽章一致加分。
- **累加記憶佈局**：以「執行檔路徑 + 視窗類別名 + 視窗標題」為鍵 upsert 合併，**保留目前沒開著的視窗記錄**（不會因單次儲存而遺失）；位置/大小/狀態（一般、最大化、最小化）存於 `%AppData%\WindowManager\layouts.json`，重開機後可用；原子寫入避免毀損；可設保留上限與「清除所有記錄」。
- **多螢幕安全還原**：螢幕配置改變或座標離屏時，自動夾限到最接近的可見螢幕工作區。
- **四種觸發機制，皆可獨立開關**：
  - 全域快捷鍵手動儲存 / 還原（快捷鍵可自訂）
  - 自動定時儲存（可設定間隔）
  - 偵測到新視窗時自動還原
  - 開機 / 登入後自動還原
- **系統列常駐 + 設定 UI**：右鍵選單、設定視窗、開機自動啟動、排除清單。

## 技術

- .NET 8（`net8.0-windows`）、WPF + Windows Forms（系統列）。
- Win32 API：`EnumWindows`、`GetWindowPlacement`/`SetWindowPlacement`、`SetWindowPos`、`RegisterHotKey`、`SetWinEventHook` 等。

## 建置與執行

```powershell
cd src\WindowManager
dotnet build
dotnet run
```

或直接執行建置後的 `bin\Debug\net8.0-windows\WindowManager.exe`。程式啟動後常駐於系統列，雙擊圖示或右鍵「設定…」開啟設定視窗。

### 命令列一次性模式（腳本化 / 驗證用）

```powershell
WindowManager.exe --save      # 擷取目前視窗並「累加合併」存檔後結束
WindowManager.exe --restore   # 依已存佈局還原匹配視窗後結束
WindowManager.exe --clear     # 清除所有已記憶的佈局
```

## 測試

```powershell
dotnet test
```

## 打包發佈

用打包腳本一次產出兩種 self-contained 版本（皆免安裝 .NET），輸出至 `publish\`：

```powershell
powershell -ExecutionPolicy Bypass -File scripts\package.ps1
# 跳過測試： -SkipTests ；指定版本／環境： -Version 1.2.0 -Runtime win-x64
```

產出：
- `WindowManager-v<版本>-win-x64.zip`（**建議分享給他人**）：一般資料夾形式，防毒較不易誤判。
- `WindowManager.exe`（單檔）：方便，但自解壓型態部分防毒較敏感。

版本號取自 `src\WindowManager\WindowManager.csproj` 的 `<Version>`。

> 若需手動執行，等同於：
> ```powershell
> # 資料夾版
> dotnet publish src\WindowManager\WindowManager.csproj -c Release -r win-x64 --self-contained true `
>   -p:PublishSingleFile=false -p:DebugType=none -o publish\win-x64
> # 單檔版
> dotnet publish src\WindowManager\WindowManager.csproj -c Release -r win-x64 --self-contained true `
>   -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish\single
> ```

### 分享與防毒注意

未做數位簽章的 exe 可能被 SmartScreen 或防毒擋下：
- SmartScreen：「其他資訊」→「仍要執行」。
- 右鍵檔案 → 內容 → 勾「解除封鎖」→ 確定（對 ZIP 解封鎖再解壓，可一次解掉整包標記）。
- 優先分享上述 **ZIP 版**並透過可信來源（GitHub Release / 雲端硬碟）連結下載，避免直接傳 `.exe`。

## 預設行為

- 預設管理所有一般視窗，可在設定的「排除清單」排除特定執行檔或視窗類別名。
- 首版維護單一套佈局（資料保留 `schemaVersion` 以利日後擴充多情境）。
- 預設快捷鍵：儲存 `Ctrl+Alt+S`、還原 `Ctrl+Alt+D`（可於設定變更；還原鍵避開常被佔用的 `Ctrl+Alt+R`）。
- 自動定時儲存、新視窗自動還原、開機後自動還原預設為**關閉**，可於設定開啟。

## 已知限制

- 以系統管理員權限執行的視窗，一般權限的本程式無法移動，會自動略過並記錄；如需管理這類視窗，請以系統管理員身分執行本程式。
- 不支援 UWP / 沙箱化視窗、虛擬桌面佈局（首版範圍外）。

## 規格

採規格驅動開發（OpenSpec），規格與設計文件位於 `openspec/changes/add-window-position-memory/`。
