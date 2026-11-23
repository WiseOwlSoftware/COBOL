// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;

namespace WiseOwl.Cobol.Frontend.Diagnostics;

/// <summary>
/// ⛔ THE ONE RULE OF <c>--suppress</c> (kb/Work PB2652; owner decision D20): which warnings a compilation does not report. A name
/// is a diagnostic's code (<c>COBOL2187</c>), a catalog descriptor's stable <see cref="Editions.Diagnostics.DiagnosticDescriptor.Id"/>
/// (<c>directive-in-exception-checking-perform</c>) or its family key (<see cref="Editions.Diagnostics.DiagnosticDescriptor.ResolvedSuppressKey"/>,
/// <c>recognized-not-implemented</c>); a warning whose code, or whose descriptor's id or key, is named is not recorded at all, so it
/// is in neither the printed lines nor the listing's records. ISO §2023:4.2.2 makes the warning mechanism one "that optionally
/// may be invoked by the user at compile time", and this is the user's choice of which of its warnings to see.
/// <para>⛔ AN ERROR IS NEVER MUTED (kb/Work PB2652 decision addendum): an error refuses the program, and a compilation that fails with
/// its reason muted fails with no reason. A name that also names an error is accepted, and mutes only the warnings it names (a removed
/// construct's code is an error strictly and a warning under <c>--permissive</c>).</para>
/// <para>Both sinks ask it, each before it records a warning: the front end's <see cref="DiagnosticBag"/> and the binder's
/// <c>EditionContext</c>; an expansion's own compilation is given the same suppression.</para>
/// </summary>
public sealed class WarningSuppression
{
    /// <summary>No name: every warning is reported (the default).</summary>
    public static WarningSuppression None { get; } = new([]);

    private readonly HashSet<string> _names;

    private WarningSuppression(IEnumerable<string> names) => _names = new(names, StringComparer.OrdinalIgnoreCase);   // not a COBOL word: a diagnostic code, id or suppress key

    /// <summary>The suppression of <paramref name="names"/>, each of which <see cref="Problem"/> accepts; a name it refuses is a fault
    /// of the caller (the command line refuses it first, with the problem's words).</summary>
    public static WarningSuppression Of(IEnumerable<string> names)
    {
        var list = names.ToList();
        if (list.Count == 0) return None;
        foreach (string name in list)
            if (Problem(name) is { } problem) throw new ArgumentException(problem, nameof(names));
        return new WarningSuppression(list);
    }

    /// <summary>The names, in no order (the listing's options section and the drift tests read them).</summary>
    public IReadOnlyCollection<string> Names => _names;

    /// <summary>Is <paramref name="diagnostic"/> a warning (or information) this suppression names?</summary>
    public bool Mutes(Diagnostic diagnostic) =>
        _names.Count != 0 && !diagnostic.IsError
        && (_names.Contains(diagnostic.Code)
            || diagnostic.Descriptor is { } descriptor
               && (_names.Contains(descriptor.Id) || _names.Contains(descriptor.ResolvedSuppressKey)));

    /// <summary>Why <paramref name="name"/> names no diagnostic of this compiler, or null when it names one. An unknown name is refused
    /// rather than ignored: a misspelled name that muted nothing would look like a warning the compiler failed to mute.</summary>
    public static string? Problem(string name) =>
        Known.Contains(name) ? null
            : $"'{name}' is not a diagnostic code, id or suppress key of this compiler (docs/DIAGNOSTICS.md lists them)";

    /// <summary>Every name a diagnostic of this compiler answers to: the catalog's codes, ids and family keys, the construct
    /// register's codes (the edition flags), and the front end's own descriptors' codes.</summary>
    private static readonly HashSet<string> Known = BuildKnown();

    private static HashSet<string> BuildKnown()
    {
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);   // not a COBOL word: a diagnostic code, id or suppress key
        foreach (var d in DiagnosticCatalog.All)
        {
            known.Add(d.Code);
            known.Add(d.Id);
            known.Add(d.ResolvedSuppressKey);
        }
        foreach (var row in ConstructRegistry.Entries) known.Add(row.DiagnosticCode);
        foreach (var field in typeof(DiagnosticDescriptors).GetFields(BindingFlags.Public | BindingFlags.Static))
            if (field.GetValue(null) is DiagnosticDescriptor frontEnd) known.Add(frontEnd.Code);
        return known;
    }
}
