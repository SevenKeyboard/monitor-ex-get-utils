// Copyright SevenKeyboard Ltd. (2026). MIT License.
using System.Runtime.InteropServices;

namespace SevenKeyboard;

/// <summary>A rectangle and its one-based index in the monitor snapshot.</summary>
public readonly record struct MonitorRectangleResult(int Index, MonitorRectangle Rectangle);

/// <summary>Nullable monitor lookups using one-based monitor numbers.</summary>
public static partial class MonitorExGetUtils
{
    private static readonly MonitorQueries Queries = new(DisplayMonitorInfoManager.GetList, ReadScale);

    public static IReadOnlyList<DisplayMonitorInfo> GetInfoList(bool force = false) =>
        DisplayMonitorInfoManager.GetList(force);

    public static DisplayMonitorInfo? GetInfo(int? index = null, bool force = false) =>
        Queries.GetInfo(index, force);

    public static int GetScaleFactor(int? index = null, int defaultValue = 100, bool force = false) =>
        Queries.GetScaleFactor(index, defaultValue, force);

    public static MonitorRectangleResult? Get(int? index = null, bool force = false) =>
        Queries.GetRectangle(index, force, workArea: false);

    public static MonitorRectangleResult? GetWorkArea(int? index = null, bool force = false) =>
        Queries.GetRectangle(index, force, workArea: true);

    public static string? GetName(int? index = null, bool force = false) =>
        Queries.GetInfo(index, force)?.DeviceName;

    public static int GetPrimary(bool force = false) => Queries.GetPrimary(force);

    public static int GetCount(bool force = false) => GetInfoList(force).Count;

    private static int? ReadScale(nint monitor) =>
        GetScaleFactorForMonitor(monitor, out int scale) == 0 ? scale : null;

    [LibraryImport("shcore.dll")]
    private static partial int GetScaleFactorForMonitor(nint monitor, out int scale);
}

// The same query implementation is used with native snapshots and deterministic test snapshots.
internal sealed class MonitorQueries(
    Func<bool, IReadOnlyList<DisplayMonitorInfo>> getList,
    Func<nint, int?> getScale)
{
    internal DisplayMonitorInfo? GetInfo(int? index, bool force)
    {
        int number = ResolveDefaultIndex(index);
        return At(getList(force), number);
    }

    internal MonitorRectangleResult? GetRectangle(int? index, bool force, bool workArea)
    {
        int number = ResolveDefaultIndex(index);
        return At(getList(force), number) is { } info
            ? new MonitorRectangleResult(number, workArea ? info.WorkArea : info.Monitor)
            : null;
    }

    internal int GetScaleFactor(int? index, int defaultValue, bool force)
    {
        IReadOnlyList<DisplayMonitorInfo> list = getList(force);
        // Unlike info/rect getters, null selects a primary AFTER the refresh,
        // and does not fall back to the first monitor if no primary exists.
        int number = index ?? FindPrimary(list);
        return At(list, number) is { } info ? getScale(info.HMonitor) ?? defaultValue : defaultValue;
    }

    internal int GetPrimary(bool force) => FindPrimary(getList(force));

    private int ResolveDefaultIndex(int? index)
    {
        if (index.HasValue)
            return index.Value;
        int primary = FindPrimary(getList(false));
        return primary == 0 ? 1 : primary;
    }

    private static int FindPrimary(IReadOnlyList<DisplayMonitorInfo> list)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i].Flags != 0)
                return i + 1;
        return 0;
    }

    private static DisplayMonitorInfo? At(IReadOnlyList<DisplayMonitorInfo> list, int number) =>
        number > 0 && number <= list.Count ? list[number - 1] : null;
}