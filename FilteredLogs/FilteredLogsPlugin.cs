using BepInEx;
using BepInEx.Logging;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using static FilteredLogs.API;

namespace FilteredLogs;

[BepInAutoPlugin(id: "io.github.flibber-hk.filteredlogs")]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public partial class FilteredLogsPlugin : BaseUnityPlugin
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
{
    internal static ManualLogSource InstanceLogger { get; private set; }

    private static readonly List<Hook> hooks = [];

    private const BindingFlags InstanceFlags = 
        BindingFlags.Public 
        | BindingFlags.NonPublic 
        | BindingFlags.Instance 
        | BindingFlags.DeclaredOnly;

    private void Awake()
    {
        InstanceLogger = this.Logger;

        hooks.Add(
            new Hook(typeof(ConsoleLogListener).GetMethod(nameof(ILogListener.LogEvent), InstanceFlags),
            ConsoleListenerHook
            ));

        hooks.Add(
            new Hook(typeof(DiskLogListener).GetMethod(nameof(ILogListener.LogEvent), InstanceFlags),
            DiskLogListenerHook
            ));

        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
    }

    private void ConsoleListenerHook(
        Action<ConsoleLogListener, object, LogEventArgs> orig,
        ConsoleLogListener self,
        object sender,
        LogEventArgs e)
    {
        if (ShouldLogConsole(e))
        {
            orig(self, sender, e);
        }
    }

    private void DiskLogListenerHook(
        Action<DiskLogListener, object, LogEventArgs> orig,
        DiskLogListener self,
        object sender,
        LogEventArgs e)
    {
        if (ShouldLogDisk(self, e))
        {
            orig(self, sender, e);
        }
    }

    internal static void ApplyFilterInternal(Func<LogEventArgs, bool> selector, FilterTargets filterTargets, string note)
    {
        if (FilteredLogsPlugin.InstanceLogger is null)
        {
            throw new InvalidOperationException($"{nameof(FilteredLogsPlugin)} has not initialized yet! " +
                $"Declare {FilteredLogsPlugin.Id} as a {nameof(BepInDependency)}");
        }

        FilteredLogsPlugin.InstanceLogger.LogInfo($"Applying filter to {filterTargets}: {note}");

        if (filterTargets.HasFlag(FilterTargets.Console))
        {
            ConsoleSelector = selector;
        }
        if (filterTargets.HasFlag(FilterTargets.Disk))
        {
            DiskSelector = selector;
        }
        if (filterTargets.HasFlag(FilterTargets.CreateDisk))
        {
            if (CreatedListener == null)
            {
                string logPath = Path.Combine(Paths.BepInExRootPath, $"FilteredLogOutput.txt");
                CreatedListener = new(logPath, displayedLogLevel: LogLevel.All, appendLog: true, includeUnityLog: false);
                BepInEx.Logging.Logger.Listeners.Add(CreatedListener);
            }

            CreatedDiskSelector = selector;
        }
    }

    internal static DiskLogListener? CreatedListener { get; set; }

    internal static Func<LogEventArgs, bool>? ConsoleSelector { get; private set; }
    internal static Func<LogEventArgs, bool>? DiskSelector { get; private set; }
    internal static Func<LogEventArgs, bool>? CreatedDiskSelector { get; private set; }

    private static bool ShouldLogConsole(LogEventArgs e)
    {
        if (ReferenceEquals(e.Source, InstanceLogger) && e.Data is string s && s.StartsWith("Applying filter"))
        {
            return true;
        }

        if (ConsoleSelector is null) return true;

        return ConsoleSelector.SafeInvoke(e);
    }

    private static bool ShouldLogDisk(DiskLogListener self, LogEventArgs e)
    {
        if (ReferenceEquals(e.Source, InstanceLogger) && e.Data is string s && s.StartsWith("Applying filter"))
        {
            return true;
        }

        if (CreatedListener is not null && ReferenceEquals(CreatedListener, self))
        {
            return CreatedDiskSelector?.SafeInvoke(e) ?? true;
        }

        return DiskSelector?.SafeInvoke(e) ?? true;
    }
}

file static class Extensions
{
    public static bool SafeInvoke<T>(this Func<T, bool> func, T arg)
    {
        try
        {
            return func(arg);
        }
        catch (Exception)
        {
            return true;
        }
    }
}
