using System.Collections.Concurrent;

namespace CobolSharp.Runtime;

/// <summary>
/// Manages implementor-defined external switches (COBOL-85 §4.4.1).
/// Switch state is per-process, shared across all programs.
/// Initial state can be set via environment variables: COBOL_SWITCH_1=ON, etc.
/// </summary>
public static class SwitchRuntime
{
    private static readonly ConcurrentDictionary<string, bool> _switches =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Get the current state of a named switch (true = ON).</summary>
    public static bool GetSwitchState(string implementorName)
    {
        if (_switches.TryGetValue(implementorName, out var state))
            return state;

        // Check environment variable: COBOL_SWITCH_1=ON etc.
        var envName = "COBOL_" + implementorName.Replace("-", "_").ToUpperInvariant();
        var envValue = Environment.GetEnvironmentVariable(envName);
        if (envValue != null)
        {
            state = envValue.Equals("ON", StringComparison.OrdinalIgnoreCase)
                 || envValue == "1"
                 || envValue.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            _switches[implementorName] = state;
            return state;
        }

        return false; // Default: OFF
    }

    /// <summary>Set the state of a named switch.</summary>
    public static void SetSwitchState(string implementorName, bool isOn)
        => _switches[implementorName] = isOn;

    /// <summary>Reset all switches (for testing).</summary>
    public static void Reset() => _switches.Clear();
}
