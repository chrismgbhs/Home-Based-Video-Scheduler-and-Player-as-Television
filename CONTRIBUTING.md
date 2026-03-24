# Contributing to Home TV Station

Thank you for your interest in contributing! Here's everything you need to know.

## Development Setup

1. Clone the repo and open the `.sln` in Visual Studio 2026
2. NuGet packages restore automatically on first build
3. Run in **Debug** mode — the app will write data to `%APPDATA%\HomeTVStation\`

## Project Conventions

**MVVM** — all logic lives in ViewModels. Views are markup only except for code-behind that requires direct WPF/Win32 access (Player, OverlayWindow, CommercialBreakView timeline drawing).

**Singletons** — `VideoStore`, `CommercialStore`, `CommercialBreakStore`, `AppSettings`, `SchedulerService`, `GapFillerService`, and `PlayerViewModel` are all singletons. Use `Instance` to access them.

**Persistence** — all data files go through `DataFolder.File("name.csv")`. Never hardcode file paths.

**Threading** — LibVLC events fire on a background thread. Always marshal back to the UI thread via `Dispatcher.InvokeAsync()` before touching UI or ViewModel state.

**Timers** — use `DispatcherPriority.Background` for all `DispatcherTimer` instances that don't need immediate UI response. This keeps playback smooth.

## Key Files to Know

| File | What it does |
|------|-------------|
| `PlayerViewModel.cs` | Core playback logic — read this first |
| `SchedulerService.cs` | The 1-second heartbeat that drives everything |
| `CommercialBreakStore.cs` | `GetTrueEndTime()` and `GetBreakEndOffset()` — used everywhere |
| `GapFillerService.cs` | Weighted random commercial selection |
| `OverlayWindow.xaml.cs` | Win32 tricks for drawing over LibVLC |

## Reporting Bugs

Please include:
- Steps to reproduce
- Expected vs actual behaviour
- Any exception message and stack trace from Visual Studio output
- Your video format (mp4, mkv, etc.) if it's playback-related
