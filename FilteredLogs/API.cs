using BepInEx;
using BepInEx.Logging;
using System;
using System.IO;

// Alias because Conditional is a type in the global namespace
using SD = System.Diagnostics;

namespace FilteredLogs;

/// <summary>
/// Entrypoint for the FilteredLogs API.
/// </summary>
public static class API
{
    /// <summary>
    /// Enum for the types of log to filter.
    /// </summary>
    [Flags]
    public enum FilterTargets
    {
        /// <summary>
        /// Filter console logs.
        /// </summary>
        Console = 1 << 0,

        /// <summary>
        /// Filter disk logs.
        /// </summary>
        Disk = 1 << 1,

        /// <summary>
        /// Create a separate log file on disk with the filter applied.
        /// </summary>
        CreateDisk = 1 << 2,
    }

    /// <summary>
    /// Remove all logs whose source does not begin with the supplied prefix.
    ///
    /// This method does not apply if the caller has been built in release mode.
    /// </summary>
    /// <param name="prefix">The prefix to check for.</param>
    /// <param name="filterTargets">Log listeners to apply to.</param>
    [SD.Conditional("DEBUG")]
    public static void ApplyFilter(
        string prefix,
        FilterTargets filterTargets = FilterTargets.Console
        )
    {
        bool eventSelector(LogEventArgs e) => e.Source.SourceName.StartsWith(prefix);

        FilteredLogsPlugin.ApplyFilterInternal(eventSelector, filterTargets, $"Prefix: {prefix}");
    }

    /// <summary>
    /// Remove all logs whose source name do not match the supplied predicate.
    /// 
    /// This method does not apply if the caller has been built in release mode.
    /// </summary>
    /// <param name="selector">The prefix to check for.</param>
    /// <param name="filterTargets">Log listeners to apply to.</param>
    [SD.Conditional("DEBUG")]
    public static void ApplyFilter(
        Func<string, bool> selector,
        FilterTargets filterTargets = FilterTargets.Console
        )
    {
        string note;
        if (selector.Method is not null)
        {
            note = $"Matches {selector.Method.Name}";
        }
        else
        {
            note = $"Matches selector";
        }

        bool eventSelector(LogEventArgs e) => selector(e.Source.SourceName);

        FilteredLogsPlugin.ApplyFilterInternal(eventSelector, filterTargets, note);
    }

    /// <summary>
    /// Remove all log events which do not match the supplied predicate.
    /// 
    /// This method does not apply if the caller has been built in release mode.
    /// </summary>
    /// <param name="selector">The log events to include.</param>
    /// <param name="filterTargets">Log listeners to apply to.</param>
    [SD.Conditional("DEBUG")]
    public static void ApplyFilter(
        Func<LogEventArgs, bool> selector,
        FilterTargets filterTargets = FilterTargets.Console
        )
    {
        string note;
        if (selector.Method is not null)
        {
            note = $"Matches {selector.Method.Name}";
        }
        else
        {
            note = $"Matches selector";
        }

        FilteredLogsPlugin.ApplyFilterInternal(selector, filterTargets, note);
    }
}
