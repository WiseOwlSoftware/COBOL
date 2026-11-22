// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace WiseOwl.Cobol;

/// <summary>
/// ⛔ TEXT THE COMPILER GENERATES IS FORMATTED UNDER THE INVARIANT CULTURE (kb/Work PB3239). An interpolated number in emitted
/// C# (<c>$"…{slot}…"</c>) and a record's synthesized <c>ToString</c> format through <see cref="CultureInfo.CurrentCulture"/>,
/// and a host whose culture writes a negative number with U+2212 MINUS SIGN (sv-SE on .NET 10) put a character in the generated
/// source that is no C# token (CS1056), so a legal program failed to compile on that host alone. The scope sets the culture
/// for the work inside it and restores the host's after; the culture is per thread and flows into the tasks the work starts,
/// so a run unit hosted in the same process keeps its own.
/// <para>Two users: <c>CompilerDriver.Compile</c> around the backend's emission (every emitter's interpolation at once), and
/// <c>StrongTypeModel</c>'s record text (kb/Work PB3257).</para>
/// </summary>
internal readonly struct InvariantCultureScope : IDisposable
{
    private readonly CultureInfo _host;

    private InvariantCultureScope(CultureInfo host) => _host = host;

    /// <summary>Set the invariant culture until the returned scope is disposed.</summary>
    public static InvariantCultureScope Enter()
    {
        var scope = new InvariantCultureScope(CultureInfo.CurrentCulture);
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        return scope;
    }

    public void Dispose() => CultureInfo.CurrentCulture = _host;
}
