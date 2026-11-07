// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.Messaging;

/// <summary>
/// ⛔ WHAT A HOST DECIDES ABOUT A RUN UNIT'S 2023 MESSAGING (docs/rearchitecture/DESIGN-message-control.md §5.5, §7.2): HOST CONFIGURATION like
/// <see cref="RunUnit.Clock"/> (<see cref="RunUnit.HostConfiguration"/>), so two run units of one process can differ. A member left unset reads its
/// environment variable when it is first asked, never before: a run unit that sends no message reads none.
/// <list type="bullet">
/// <item><see cref="Directory"/>: the MCS directory (<see cref="Mcs.McsDirectory.Resolve(string?)"/> reads <c>WISEOWL_COBOL_MCS</c> when this is null or empty).</item>
/// <item><see cref="ServedName"/>: the server name this run unit serves (<c>WISEOWL_COBOL_MCS_SERVER</c>), or null when it serves none.</item>
/// <item><see cref="NotifyPath"/>: the notify socket of the requestor that started this server (<c>WISEOWL_COBOL_MCS_NOTIFY</c>), connected to once the
/// listener is published (design 5.4.5).</item>
/// <item><see cref="Time"/>: the time source a CONTINUE AFTER deadline is armed on (design 5.4.4). It is never the run unit's <see cref="RunUnit.Clock"/>,
/// which <c>WISEOWL_COBOL_CLOCK</c> may freeze: a frozen clock must not stop a deadline.</item>
/// <item><see cref="Definitions"/>: what <c>mcs.json</c> defines for a normalized server name.</item>
/// </list>
/// </summary>
public sealed class MessagingSettings
{
    /// <summary>The variable that makes a run unit serve a name (design 5.5).</summary>
    internal const string ServerVariable = "WISEOWL_COBOL_MCS_SERVER";

    /// <summary>The variable that names the notify socket of the requestor that started this server (design 5.4.5).</summary>
    internal const string NotifyVariable = "WISEOWL_COBOL_MCS_NOTIFY";

    private string? _served, _notify;
    private bool _servedSet, _notifySet;

    /// <summary>The MCS directory; null or empty = <c>WISEOWL_COBOL_MCS</c>, else the current directory.</summary>
    public string? Directory { get; init; }

    /// <summary>The server name this run unit serves, as the program spells it, or null; unset = <c>WISEOWL_COBOL_MCS_SERVER</c> (blank = none).</summary>
    public string? ServedName
    {
        get => _servedSet ? _served : _served = NonBlank(Environment.GetEnvironmentVariable(ServerVariable));
        init { _served = NonBlank(value); _servedSet = true; }
    }

    /// <summary>The notify socket's path, or null; unset = <c>WISEOWL_COBOL_MCS_NOTIFY</c> (blank = none).</summary>
    public string? NotifyPath
    {
        get => _notifySet ? _notify : _notify = NonBlank(Environment.GetEnvironmentVariable(NotifyVariable));
        init { _notify = NonBlank(value); _notifySet = true; }
    }

    /// <summary>The time source of a CONTINUE AFTER deadline.</summary>
    public TimeProvider Time { get; init; } = TimeProvider.System;

    /// <summary>What <c>mcs.json</c> defines for a normalized server name, or null when it defines none. The default defines nothing.</summary>
    internal Func<string, ServerDefinition?> Definitions { get; init; } = static _ => null;

    private static string? NonBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
