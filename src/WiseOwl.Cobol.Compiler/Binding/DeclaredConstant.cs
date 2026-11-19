// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;

/// <summary>Where a constant entry stands in its bind: not yet reached, being evaluated (a re-entry is a
/// circular dependence), waiting for the later data item its length phrase names, or finished (bound or
/// rejected).</summary>
internal enum ConstantBindState { Declared, Evaluating, Postponed, Finished }

/// <summary>One constant entry of this unit or of one of its methods, collected before anything binds
/// (<see cref="ConstantEntryBinder.DeclareDataEntries"/>). <paramref name="entry"/> is the host entry (the diagnostic anchor); the
/// level-number, the optional name and the body are the three parts both hosts spell (§2023:13.10.2; kb/Work PB1226).
/// <paramref name="method"/> is the source element that declares it, whose table its value joins: the method whose name
/// scope it is, or null for the binder's own element (kb/Work PB3164).</summary>
internal sealed class DeclaredConstant(
    Antlr4.Runtime.ParserRuleContext entry, Core.LevelNumberContext level, Core.DataNameContext? nameCtx,
    Core.ConstantEntryBodyContext body, OoMethodDataScope? method)
{
    public Antlr4.Runtime.ParserRuleContext Entry { get; } = entry;
    public OoMethodDataScope? Method { get; } = method;
    public Core.LevelNumberContext Level { get; } = level;
    public Core.DataNameContext? NameCtx { get; } = nameCtx;
    /// <summary>The constant-name as written; only a named entry joins the symbol table.</summary>
    public string Name => NameCtx?.GetText() ?? throw new InvalidOperationException("an unnamed constant entry has no constant-name");
    public Core.ConstantEntryBodyContext Body { get; } = body;
    public ConstantBindState State { get; set; }
    /// <summary>The entry's §2023:13.10.2 shape checks and its user-word declaration ran (once, whatever the order).</summary>
    public bool Screened { get; set; }
    /// <summary>The cycle through this entry was reported, so a second reader of it does not report it again.</summary>
    public bool CycleReported { get; set; }
    /// <summary>What the entry stands for once it bound, or null while unbound or when it was refused (ISO §2023:13.10.4 GR1).</summary>
    public ConstantDef? Def { get; set; }
    /// <summary>The entry is a length phrase (§2023:13.10.2 BYTE-LENGTH OF / LENGTH OF) — its cycle is SR4's.</summary>
    public bool IsLengthPhrase => Body.constantValue() is { } cv
        && (cv.LENGTH() is not null || cv.constantByteLengthWord() is not null);
}
