using System.Runtime.InteropServices;
using SevenKeyboard;

internal static partial class Program
{
    private static int _checks;

    private static void Main()
    {
        DefaultsAndInvalidIndices();
        RefreshOrdering();
        NativeReadOnly();
        Console.WriteLine($"PASS: {_checks} utility checks (deterministic lookups and read-only native queries).");
    }

    private static DisplayMonitorInfo Info(int handle, uint flags = 0, string? name = null) =>
        new(handle, flags, name ?? "monitor-" + handle, new(-200, -50, 800, 950), new(-200, -50, 800, 900));

    private static void DefaultsAndInvalidIndices()
    {
        IReadOnlyList<DisplayMonitorInfo> list = Array.AsReadOnly(new[] { Info(10), Info(20, 8), Info(30, 1) });
        int scaleCalls = 0;
        var query = new MonitorQueries(_ => list, handle => { scaleCalls++; return handle == 20 ? 175 : null; });
        Check(query.GetInfo(null, false) == list[1], "any nonzero flags qualify, first in enumeration order");
        Check(query.GetPrimary(false) == 2, "primary is one-based and uses any nonzero flags");
        Check(query.GetInfo(1, false) == list[0], "one-based explicit lookup");
        Check(query.GetRectangle(null, false, false) == new MonitorRectangleResult(2, list[1].Monitor), "rectangle has selected index");
        Check(query.GetRectangle(1, false, true) == new MonitorRectangleResult(1, list[0].WorkArea), "work-area uses its own rectangle");
        Check(query.GetScaleFactor(null, 100, false) == 175, "scale uses first primary");
        Check(query.GetScaleFactor(1, 123, false) == 123, "failed native scale uses caller default");
        foreach (int index in new[] { int.MinValue, -1, 0, 4, int.MaxValue })
        {
            Check(query.GetInfo(index, false) is null, "invalid index returns null");
            Check(query.GetRectangle(index, false, true) is null, "invalid rectangle returns null");
            int before = scaleCalls;
            Check(query.GetScaleFactor(index, -7, false) == -7 && scaleCalls == before, "invalid scale index skips native query");
        }

        list = Array.AsReadOnly(new[] { Info(40) });
        Check(query.GetInfo(null, false) == list[0], "missing primary falls back to first for info");
        Check(query.GetRectangle(null, false, false)?.Index == 1, "missing primary falls back to first for rect");
        Check(query.GetPrimary(false) == 0, "no primary returns zero");
        int prior = scaleCalls;
        Check(query.GetScaleFactor(null, 42, false) == 42 && prior == scaleCalls, "scale does not fall back to first");
        list = Array.Empty<DisplayMonitorInfo>();
        Check(query.GetInfo(null, false) is null && query.GetRectangle(null, false, false) is null, "empty snapshot returns null");
        Check(query.GetPrimary(false) == 0 && query.GetScaleFactor(null, 100, false) == 100, "empty primary and scale fallbacks");

        list = Array.AsReadOnly(new[] { Info(1, 1, "") });
        Check(query.GetInfo(null, false)?.DeviceName == "", "empty device name is distinct from missing info");
        var zeroScale = new MonitorQueries(_ => list, _ => 0);
        Check(zeroScale.GetScaleFactor(null, 100, false) == 0, "successful native scale is returned without sanitization");
    }

    private static void RefreshOrdering()
    {
        IReadOnlyList<DisplayMonitorInfo> oldList = Array.AsReadOnly(new[] { Info(10), Info(20, 1) });
        IReadOnlyList<DisplayMonitorInfo> newList = Array.AsReadOnly(new[] { Info(30, 1), Info(40) });
        var calls = new List<bool>();
        var query = new MonitorQueries(force => { calls.Add(force); return force ? newList : oldList; }, handle => (int)handle);
        Check(query.GetInfo(null, true) == newList[1], "default index is resolved on old cache before force");
        Check(calls.SequenceEqual(new[] { false, true }), "default info call order");
        calls.Clear();
        Check(query.GetRectangle(null, true, true) == new MonitorRectangleResult(2, newList[1].WorkArea), "rectangle preserves old resolved index");
        Check(calls.SequenceEqual(new[] { false, true }), "default rectangle call order");
        calls.Clear();
        Check(query.GetInfo(1, true) == newList[0] && calls.SequenceEqual(new[] { true }), "explicit index makes only requested read");
        calls.Clear();
        Check(query.GetScaleFactor(null, 100, true) == 30 && calls.SequenceEqual(new[] { true }), "scale resolves primary after force");
        Check(query.GetPrimary(true) == 1, "primary lookup uses refreshed list");

        newList = Array.AsReadOnly(new[] { Info(30, 1) });
        Check(query.GetInfo(null, true) is null, "old primary index missing after refresh stays missing");
        Check(query.GetRectangle(null, true, false) is null, "rectangle does not retarget refreshed primary");
        oldList = Array.Empty<DisplayMonitorInfo>();
        Check(query.GetInfo(null, true) == newList[0], "empty old list resolves default index one before force");
    }

    private static void NativeReadOnly()
    {
        Check(MonitorExGetUtils.GetInfo(0) is null, "public invalid info");
        Check(MonitorExGetUtils.Get(0) is null && MonitorExGetUtils.GetWorkArea(0) is null, "public invalid rectangles");
        Check(MonitorExGetUtils.GetName(0) is null, "public invalid name");
        Check(MonitorExGetUtils.GetScaleFactor(0, 137) == 137, "public scale default");
        var snapshot = MonitorExGetUtils.GetInfoList(force: true);
        Check(ReferenceEquals(snapshot, DisplayMonitorInfoManager.GetList()), "utilities share manager snapshot");
        Check(MonitorExGetUtils.GetCount() == snapshot.Count, "public count");
        int primary = 0;
        for (int i = 0; i < snapshot.Count; i++)
        {
            int number = i + 1;
            var info = snapshot[i];
            if (primary == 0 && info.Flags != 0) primary = number;
            Check(MonitorExGetUtils.GetInfo(number) == info, "public indexed info matches cache");
            Check(MonitorExGetUtils.GetName(number) == info.DeviceName, "public device name");
            Check(MonitorExGetUtils.Get(number) == new MonitorRectangleResult(number, info.Monitor), "public monitor rectangle");
            Check(MonitorExGetUtils.GetWorkArea(number) == new MonitorRectangleResult(number, info.WorkArea), "public work area");
        }
        Check(MonitorExGetUtils.GetPrimary() == primary, "public primary index");
        int actualScale = MonitorExGetUtils.GetScaleFactor();
        if (primary != 0)
        {
            int hr = GetScaleFactorForMonitor(snapshot[primary - 1].HMonitor, out int nativeScale);
            Check(actualScale == (hr == 0 ? nativeScale : 100), "public scale matches native HRESULT/fallback");
            Console.WriteLine($"Read-only primary scale: {actualScale}%; native HRESULT=0x{hr:X8}, scale={nativeScale}, DPI awareness={GetAwarenessFromDpiAwarenessContext(GetThreadDpiAwarenessContext())}; {snapshot.Count} monitor(s).");
        }
        else
            Check(actualScale == 100, "native missing-primary fallback");
    }

    [LibraryImport("shcore.dll")]
    private static partial int GetScaleFactorForMonitor(nint monitor, out int scale);
    [LibraryImport("user32.dll")]
    private static partial nint GetThreadDpiAwarenessContext();
    [LibraryImport("user32.dll")]
    private static partial int GetAwarenessFromDpiAwarenessContext(nint context);

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }
}