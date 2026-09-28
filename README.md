# PeekPin

[Tiếng Việt](#tiếng-việt) · [English](#english)

## Tiếng Việt

PeekPin ghim một cửa sổ đang chạy lên trên các cửa sổ khác. Khi cửa sổ đó được thu nhỏ, một icon của chính app hiện trên màn hình. Icon kéo được, pixel trong suốt không chặn chuột. Đứng trên icon 0,5 giây thì cửa sổ hiện lại mà không lấy focus. Rê chuột ra thì cửa sổ thu nhỏ. Click icon thì giữ cửa sổ để dùng bình thường. Thoát PeekPin từ khay hệ thống, không tắt app đích và không đọc nội dung cửa sổ.

Phiên bản 1.0.0. Windows 10 64-bit và Windows 11 dùng cùng một bản. Ngôn ngữ giao diện mặc định là tiếng Việt; có thể đổi sang tiếng Anh trong Cài đặt.

### Cài đặt

Máy mới không tải package và không cần cài .NET. Bộ cài đã gồm app và runtime, nằm sẵn trong repo:

`release/PeekPin-1.0.0-win-x64.zip`

Giải nén, rồi chạy `Setup.cmd`. Không cần build. Windows sẽ hỏi quyền Administrator.

- File nằm ở `C:\Program Files\PeekPin`
- Shortcut trong Start Menu
- Mục gỡ trong Settings > Apps

Muốn tạo lại bộ cài trên máy build (cần [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)):

```powershell
.\installer\build.ps1
```

Lệnh này tạo `dist\PeekPin-1.0.0-win-x64\`. Nén thư mục đó thành `release\PeekPin-1.0.0-win-x64.zip` nếu cần cập nhật bản trong repo.

Gỡ: Settings > Apps > PeekPin > Uninstall, hoặc chạy `C:\Program Files\PeekPin\Uninstall.cmd`. Gỡ xóa file app, shortcut và mục chạy cùng Windows. Cấu hình trong `%AppData%\PeekPin` được giữ. Thêm `-RemoveUserData` khi gọi `Uninstall.ps1` nếu muốn xóa luôn cấu hình và log.

### Cách dùng

1. Mở PeekPin. Icon nằm ở khay hệ thống.
2. Click trái icon khay để mở bảng. Bấm **Thêm cửa sổ** và chọn cửa sổ đang mở. Tối đa 8 cửa sổ. Cửa sổ vừa chọn nổi lên trên, kích thước giữ nguyên.
3. Thu nhỏ cửa sổ đó (nút minimize của app, hoặc **Thu vào icon** trong bảng). Icon app xuất hiện, mặc định gần góc phải-dưới màn hình có con trỏ.
4. Kéo icon tới chỗ muốn. Vị trí được nhớ.
5. Đưa chuột lên icon và giữ khoảng 0,5 giây: cửa sổ hiện lại, app đang gõ không mất focus.
6. Rê chuột ra khỏi icon và ra khỏi cửa sổ: sau khoảng 0,25 giây cửa sổ thu nhỏ lại.
7. Click icon, hoặc click vào thân cửa sổ đang hiện tạm: cửa sổ được giữ. Dùng app như bình thường. Icon biến mất cho đến lần thu nhỏ sau.
8. Chuột phải icon: Hiện và giữ, Thu nhỏ, Bỏ theo dõi, Cài đặt. Không có Quit trên icon.
9. Chuột phải khay: Thêm cửa sổ, Tạm dừng / Tiếp tục, Cài đặt, Quit.
10. Trong **Cài đặt**, chọn **Ngôn ngữ**: Tiếng Việt hoặc English. Giao diện đổi ngay và được nhớ cho lần mở sau.
11. Tắt hẳn cửa sổ đang theo dõi thì PeekPin tự xóa dòng đó. Mở lại app đích sau đó không tự được theo dõi lại.

Quit chỉ tắt PeekPin. Cửa sổ đang xem tạm được thu nhỏ lại. Cửa sổ đang mở được giữ nguyên vị trí và hết always-on-top. App đích vẫn chạy.

### Cài đặt trong app

| Mục | Mặc định |
| --- | --- |
| Ngôn ngữ | Tiếng Việt (`vi`) |
| Độ trễ hover | 500 ms (100–3000) |
| Độ trễ ẩn | 250 ms (50–2000) |
| Cỡ icon | 48 (32, 48 hoặc 64) |
| Nhớ vị trí icon | Bật |
| Chạy cùng Windows | Tắt |
| Ẩn icon khi có app fullscreen | Bật |

Bấm **Lưu** thì cấu hình, danh sách đang theo dõi và vị trí icon được ghi vào `%AppData%\PeekPin\config.json`. Trước lúc bấm Lưu, những thay đổi chỉ nằm trong bộ nhớ và mất khi thoát. Log chỉ ghi khi có lỗi, tại `%AppData%\PeekPin\logs\peekpin.log`.

### Giới hạn

- Không hoạt động trên exclusive fullscreen (game DirectX chiếm màn hình). Cửa sổ fullscreen không viền trên desktop làm PeekPin ẩn icon và không tranh always-on-top.
- Cửa sổ chạy quyền Admin trong khi PeekPin không Admin được đánh dấu là không truy cập được, không làm crash.
- Một số app Electron có thể vẫn lấy focus khi hiện tạm. Chuẩn kiểm tra là cửa sổ WinForms.

### Build và test

```powershell
dotnet build PeekPin.sln -c Release
dotnet test PeekPin.sln -c Release
```

`dotnet test` là cổng trước khi gọi bản này là xong. Bộ test gồm logic thuần, HWND thật, và app thật (hover, click, kéo, click xuyên, pause, quit, một instance). Test app chiếm chuột vài giây. Trên máy không có desktop tương tác, các test đó không chạy; trên máy Windows thường thì chúng phải pass, không được skip.

### Cấu trúc

- `src/PeekPin.Core` — trạng thái, hover, config. Không gọi Win32.
- `src/PeekPin.Win32` — User32: placement, topmost, catalog, hook.
- `src/PeekPin.App` — WPF, icon nổi, khay.
- `tests/PeekPin.Core.Tests`, `tests/PeekPin.Win32.Tests`, `tests/PeekPin.App.Tests`.
- `installer` — bản cài per-user, self-contained, không tải gì trên máy đích.

Code viết bằng tiếng Anh. Chữ trên UI nằm trong `Strings.resx` (tiếng Việt) và `Strings.en.resx` (tiếng Anh).

### Xử lý sự cố

- Không thấy cửa sổ trong danh sách: cửa sổ không có tiêu đề, là tool window, hoặc đang bị ẩn.
- Icon không hiện: đang Tạm dừng, hoặc có app fullscreen và mục ẩn icon đang bật.
- Không nổi được: cửa sổ quyền cao hơn PeekPin. Dòng đó hiện cảnh báo.
- Lỗi khác: xem `peekpin.log` nếu file đã được tạo. Log chỉ ghi lỗi, gồm tên process, HWND và mã lỗi, không ghi nội dung cửa sổ.

## English

PeekPin pins a running window above the others. When that window is minimized, a small icon of the app itself stays on screen. The icon can be dragged, and transparent pixels do not block the mouse. Hover the icon for about 0.5 seconds and the window comes back without taking focus. Move the pointer away and it minimizes again. Click the icon to keep the window and use it normally. Quit PeekPin from the system tray. It does not close the target app and it does not read window contents.

Version 1.0.0. Windows 10 64-bit and Windows 11 use the same build. The interface defaults to Vietnamese and can be switched to English in Settings.

### Install

A new PC does not download packages and does not need .NET installed. The setup already contains the app and the runtime, and it is already in the repo:

`release/PeekPin-1.0.0-win-x64.zip`

Unzip it and run `Setup.cmd`. No build step. Windows asks for Administrator approval.

- Files go to `C:\Program Files\PeekPin`
- A Start Menu shortcut is created
- An entry appears in Settings > Apps

To rebuild the setup on a build machine (needs the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)):

```powershell
.\installer\build.ps1
```

That writes `dist\PeekPin-1.0.0-win-x64\`. Zip that folder to `release\PeekPin-1.0.0-win-x64.zip` when the copy in the repo should be updated.

Uninstall from Settings > Apps > PeekPin > Uninstall, or run `C:\Program Files\PeekPin\Uninstall.cmd`. Uninstall removes the app files, the shortcut, and the start-with-Windows entry. Settings in `%AppData%\PeekPin` stay. Pass `-RemoveUserData` to `Uninstall.ps1` to delete settings and logs as well.

### How to use

1. Start PeekPin. Its icon sits in the system tray.
2. Left-click the tray icon to open the panel. Click **Add window** and pick an open window. Up to 8 windows. The chosen window stays on top and keeps its size.
3. Minimize that window (its own minimize button, or **Dock to icon** in the panel). The app icon appears, by default near the bottom-right of the monitor under the pointer.
4. Drag the icon where you want it. The position is remembered.
5. Hover the icon for about 0.5 seconds: the window returns and the app you were typing in keeps focus.
6. Move the pointer off the icon and off the window: after about 0.25 seconds the window minimizes.
7. Click the icon, or click the body of the peeked window: the window stays. Use the app normally. The icon stays hidden until the next minimize.
8. Right-click the icon: Show and keep, Minimize, Stop watching, Settings. There is no Quit on the icon.
9. Right-click the tray: Add window, Pause / Resume, Settings, Quit.
10. In **Settings**, choose **Language**: Tiếng Việt or English. The interface switches immediately and the choice is kept for the next launch.
11. Closing a watched window removes that row. Opening the target app again does not watch it automatically.

Quit only closes PeekPin. A window that was being peeked is minimized. A window that was already open stays where it is and loses always-on-top. The target app keeps running.

### In-app settings

| Setting | Default |
| --- | --- |
| Language | Vietnamese (`vi`) |
| Hover delay | 500 ms (100–3000) |
| Hide delay | 250 ms (50–2000) |
| Icon size | 48 (32, 48, or 64) |
| Remember icon position | On |
| Start with Windows | Off |
| Hide icon during fullscreen | On |

Click **Save** to write settings, the watched list, and icon positions to `%AppData%\PeekPin\config.json`. Until then those changes stay in memory and are discarded on exit. The log is written only for errors, at `%AppData%\PeekPin\logs\peekpin.log`.

### Limits

- Exclusive fullscreen (a DirectX game that owns the display) is not supported. A borderless fullscreen window on the desktop makes PeekPin hide its icon and stop fighting for always-on-top.
- A window running as Administrator while PeekPin is not elevated is marked inaccessible. PeekPin does not crash.
- Some Electron apps may still take focus when peeked. The check used for this build is a WinForms window.

### Build and test

```powershell
dotnet build PeekPin.sln -c Release
dotnet test PeekPin.sln -c Release
```

`dotnet test` is the gate before this build is treated as done. The suite covers pure logic, real HWNDs, and the real app (hover, click, drag, click-through, pause, quit, single instance). The app tests move the mouse for a few seconds. On a machine without an interactive desktop those tests do not run; on a normal Windows machine they must pass and must not be skipped.

### Layout

- `src/PeekPin.Core` — state, hover, config. No Win32 calls.
- `src/PeekPin.Win32` — User32: placement, topmost, catalog, hook.
- `src/PeekPin.App` — WPF, float icon, tray.
- `tests/PeekPin.Core.Tests`, `tests/PeekPin.Win32.Tests`, `tests/PeekPin.App.Tests`.
- `installer` — per-user self-contained setup. The target PC downloads nothing.

Code is written in English. UI copy lives in `Strings.resx` (Vietnamese) and `Strings.en.resx` (English).

### Troubleshooting

- A window is missing from the list: it has no title, it is a tool window, or it is hidden.
- The icon does not appear: Pause is on, or a fullscreen app is active and hide-during-fullscreen is on.
- The window will not stay on top: it has higher privileges than PeekPin. That row shows a warning.
- Anything else: read `peekpin.log` if that file exists. It records errors only: process name, HWND, and error code. It does not record window contents.
