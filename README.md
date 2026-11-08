# MonitorExGetUtils

C# monitor-information helpers. Version **2.0.0**, namespace `SevenKeyboard`,
targeting `net10.0-windows`.

```csharp
using SevenKeyboard;

int primaryScale = MonitorExGetUtils.GetScaleFactor(); // percentage; default 100
DisplayMonitorInfo? primary = MonitorExGetUtils.GetInfo();
MonitorRectangleResult? area = MonitorExGetUtils.GetWorkArea();
```

## API

| Method | Return |
| --- | --- |
| `GetInfoList(bool force = false)` | `IReadOnlyList<DisplayMonitorInfo>` |
| `GetInfo(int? index = null, bool force = false)` | `DisplayMonitorInfo?` |
| `GetScaleFactor(int? index = null, int defaultValue = 100, bool force = false)` | `int` percentage |
| `Get(int? index = null, bool force = false)` | `MonitorRectangleResult?` |
| `GetWorkArea(int? index = null, bool force = false)` | `MonitorRectangleResult?` |
| `GetName(int? index = null, bool force = false)` | `string?` |
| `GetPrimary(bool force = false)` | one-based `int`, or 0 |
| `GetCount(bool force = false)` | `int` |

Monitor-number arguments and `MonitorRectangleResult.Index` are **one-based**.
The result is a readonly record struct with `int Index` and
`MonitorRectangle Rectangle`. The returned .NET list uses zero-based indexing.
Missing info, rectangles, and names return null. An empty device name remains
an empty string. Version 2.0.0 replaces AHK output variables/ErrorLevel with
nullable structured results.

## Preserved source behavior

- Any nonzero `Flags` qualifies as primary; the first qualifying entry wins.
- Null indices for info/rectangle/name resolve against the existing cache
  **before** an optional force refresh, falling back to index 1 when no primary
  exists. If that index disappears after refresh, the result is null.
- Scale selection resolves against the optionally refreshed list. A missing
  primary/index or any native result other than `S_OK` returns `defaultValue`.
  It does not fall back to the first monitor or use native failure output.
- Scale is queried on the caller thread each time. No DPI context is changed.
- The shared manager initializes automatically and preserves enumeration order,
  skipped bad records, and partial enumeration results.

The manager owns its hidden top-level message window and background thread;
no application UI integration is required. Its only automatic refresh trigger is
`WM_SETTINGCHANGE` with `SPI_SETWORKAREA`.

## Dependencies

The project references the sibling source project:

`../../../../display-monitor-info-manager/main-cs/src/SevenKeyboard.DisplayMonitorInfoManager/SevenKeyboard.DisplayMonitorInfoManager.csproj`

It imports the sibling `library-build/main-cs/CheckLibraryVersions.targets`.
The manager reference requires the same major version and at least **2.0.0**.
Override `DisplayMonitorInfoManagerProjectPath` and `LibraryBuildTargetsPath`
as MSBuild properties when using a different checkout layout. No dependency
source files are copied into this repository.

## Build and test

From this repository in PowerShell:

```powershell
$artifacts = Join-Path $PWD ('.artifacts/check-' + [guid]::NewGuid().ToString('N'))
dotnet build tests/MonitorExGetUtils.Tests/MonitorExGetUtils.Tests.csproj -c Release --artifacts-path $artifacts
dotnet "$artifacts/bin/MonitorExGetUtils.Tests/release/MonitorExGetUtils.Tests.dll"
pwsh -File tests/Test-VersionReference.ps1
```

Tests cover nullable returns, index validation, primary ordering, source fallback
differences, pre-refresh index resolution, and read-only native queries through
the public API. Version-reference tests use isolated fixtures under `.artifacts`.
No system settings or mouse input are changed.


To run the x86 checks, use a fresh artifacts directory, add
`-p:PlatformTarget=x86` to the build command, and run the DLL with
`C:/Program Files (x86)/dotnet/dotnet.exe`. For x64 use
`-p:PlatformTarget=x64` and the x64 runtime. NativeAOT x64 can be checked with
`dotnet publish tests/MonitorExGetUtils.Tests/MonitorExGetUtils.Tests.csproj -c Release -r win-x64 -p:PublishAot=true --artifacts-path $artifacts`,
then run the executable under `$artifacts/publish/MonitorExGetUtils.Tests/release_win-x64`.
Source: the original AHK1.1 branch of this repository. Copyright SevenKeyboard
Ltd. (2025–2026). MIT License.