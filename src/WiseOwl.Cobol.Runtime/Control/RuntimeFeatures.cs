// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// The runtime's feature switches (DESIGN-external-repository §15.5; kb/Work PB2101): what an application's trimmer or
/// native-AOT compiler may remove from the runtime.
/// </summary>
internal static class RuntimeFeatures
{
    /// <summary>The name of the switch, as an application states it in its <c>runtimeconfig.json</c>
    /// (<c>configProperties</c>) or, to have the trimmer act on it, as a <c>RuntimeHostConfigurationOption</c> with
    /// <c>Trim="true"</c>.</summary>
    internal const string DynamicProbeSwitch = "WiseOwl.Cobol.Runtime.DynamicProbe";

    /// <summary>Whether the run unit may LOCATE a program the registered modules do not carry by loading a sibling module
    /// from the application directory (<c>ProgramTable.ProbeSiblingModule</c>, DESIGN-external-repository §11.2): the ONE
    /// reflection the runtime holds (<c>AssemblyLoadContext.LoadFromAssemblyPath</c>, <c>Assembly.GetType(string)</c> and the
    /// <c>EnsureRegistered</c> member's lookup are the trim analyzer's IL2026 and IL2075 sites), and so the one thing a trimmed
    /// or native-AOT application removes. On by default for a framework-dependent run unit (a trimmed publish that keeps it
    /// roots the whole runtime: a callee found at run time may use any member of it); an application that publishes
    /// trimmed or ahead of time and composes its run unit itself (§11.4) switches it off and the probe is removed, so a CALL
    /// no registered module answers is EC-PROGRAM-NOT-FOUND with the reason. A native-AOT application has no dynamic code to
    /// load, so its default is off.</summary>
    [FeatureSwitchDefinition(DynamicProbeSwitch)]
    internal static bool DynamicProbe =>
        AppContext.TryGetSwitch(DynamicProbeSwitch, out bool configured) ? configured : RuntimeFeature.IsDynamicCodeSupported;
}
