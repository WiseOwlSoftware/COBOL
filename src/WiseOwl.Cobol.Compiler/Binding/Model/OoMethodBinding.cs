// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>
/// The AFTER-DATA-BIND half of a method's description (P9 R7 — phase-explicit): the resolved formal list,
/// RETURNING item, data roots, and (stamped later still, at class-BODY bind) the pc range. Attached to
/// <see cref="OoMethodSymbol.Binding"/> by <c>OoDataBinder.BindMethodData</c> once the method's data has
/// bound — reading it earlier is a null-deref (a TYPE-level ordering fact), never a silent <c>-1</c>
/// sentinel mis-read. (<c>OverrideOf</c> stays on the SYMBOL: it is an identity fact of the roster, which
/// <c>OoClassTable.ResolveOverrides</c> marks — with the ClrName adoption — once every roster is complete, before any
/// signature is compared or any body binds; kb/Work PB1274.)
/// </summary>
public sealed class OoMethodBinding
{
    /// <summary>The ordered PD USING formals (§2023:14.9.23.4 GR3 — positional correspondence; every formal is
    /// BY REFERENCE, the header BY VALUE phrase being an unparsed grammar extension).</summary>
    public List<OoFormal> Formals { get; } = [];

    /// <summary>The C# parameter name for the formal <paramref name="item"/> at <paramref name="position"/>: the TAGGED family
    /// <see cref="NamingConvention.FormalParameterName"/> (<c>__formal_</c> + the sanitized COBOL name), uniquified among the
    /// formals already added. The <c>__</c> head keeps it off the item's own <see cref="DataItem.ClrName"/> (a user word never
    /// sanitizes to a leading <c>__</c>), which becomes the capturable LOCAL the body reads and writes (a C# local may not
    /// shadow a parameter — CS0136); the non-empty TAG keeps it off the emitter's fixed <c>__X</c> members — the untagged
    /// <c>"__" + word</c> shape turned a formal named N into <c>__N</c>, the paragraph-count constant (kb/Work PB973). The one
    /// naming of a method's formals: the binder names a method written in COBOL's, the repository importer an imported one's.</summary>
    public string FormalName(DataItem item, int position)
    {
        string name = NamingConvention.FormalParameterName(item.CobolName ?? $"P{position}");
        while (Formals.Any(f => f.ParamName == name)) name += "_";
        return name;
    }

    /// <summary>The PD RETURNING item (a LINKAGE 01/77 — §2023:14.2.3 GR6: callee-allocated; the method's C# return
    /// value delivers it, §2023:14.9.23.4 GR8), or null for a void method.</summary>
    public DataItem? Returning { get; set; }

    /// <summary>The items the signature describes: each formal parameter's, then the RETURNING item's when there is one.</summary>
    public IEnumerable<DataItem> SignatureItems =>
        Returning is { } returning ? Formals.Select(formal => formal.Item).Append(returning) : Formals.Select(formal => formal.Item);

    /// <summary>The §2023:9.3.6 description of each formal, with its OPTIONAL and BY VALUE phrases, in <see cref="Formals"/>
    /// order, each null for a formal with no universal crossing form: what the universal dispatch switch's case compares an
    /// argument with (§2023:14.9.23.4 GR7 b), c)). Built by the binder's one description builder (<c>ActivationDescriptions</c>), the
    /// builder the invoking side's descriptions come from too, at the end of the bind (<c>BackendAnswers</c>; kb/Work PB2403)
    /// for a method of one of the group's class units that has a case in that switch (not an override, no BY VALUE formal); null for
    /// any other method.</summary>
    public IReadOnlyList<WiseOwl.Cobol.Runtime.ActivationDescription?>? FormalDescriptions { get; set; }

    /// <summary>The §2023:9.3.6 description of the <see cref="Returning"/> item, written with <see cref="FormalDescriptions"/>; null
    /// when the method has none, the item has no crossing form, or the method is not described.</summary>
    public WiseOwl.Cobol.Runtime.ActivationDescription? ReturningDescription { get; set; }

    /// <summary>The classes and interfaces of the method's PROCEDURE DIVISION header RAISING phrase, resolved (ISO §2023:14.6.13.1.5
    /// items 1 and 3; <see cref="RaisingObject"/>), written at the end of the bind for a method of one of the group's class units;
    /// empty otherwise.</summary>
    public IReadOnlyList<RaisingObject> RaisingObjects { get; set; } = [];

    /// <summary>The method's LINKAGE roots (ALL of them — formals, the RETURNING item, and any unattached
    /// entry): each becomes a capturable C# LOCAL of the emitted method.</summary>
    public List<DataItem> LinkageRoots { get; } = [];

    /// <summary>LOCAL-STORAGE roots → C# locals, re-initialized on every activation (§2023:8.6.4).</summary>
    public List<DataItem> LocalRoots { get; } = [];

    /// <summary>The method's contiguous pc range in its class's one dispatch space — assigned by
    /// <c>StatementBinder.BindClassBody</c> (the exit-bounded range IS the fall-through guard: running past
    /// the last paragraph returns from the method, never into a sibling's paragraphs — the legacy trap #4).
    /// Entry &gt; End ⇔ an empty method body.</summary>
    public int EntryPc { get; set; } = -1;
    public int EndPc { get; set; } = -2;   // Entry > End ⇔ an empty method body

    /// <summary>The first pc of the method's slice: its declarative sections occupy
    /// <c>[DeclStartPc .. EntryPc−1]</c> (empty — DeclStartPc == EntryPc — for a method with none), reached only
    /// through the method's USE selection or an explicit PERFORM (ISO §2023:14.9.49.3 SR4; §2023:14.2.3 GR1). The emitted
    /// <c>__MDispatch</c> carries cases for <c>[DeclStartPc .. EndPc]</c>; the activation runs
    /// <c>[EntryPc .. EndPc]</c> (kb/Work PB1010).</summary>
    public int DeclStartPc { get; set; } = -1;

    /// <summary>The method's contiguous sub-range of the class's appended Format-3 (exception-checking) PERFORM
    /// handler pc-space (ISO §2023:14.9.28.4 GR17; design SSOT §9.10). The class's handler pcs
    /// <c>[F3HandlerBasePc .. F3HandlerBasePc+H−1]</c> partition into per-method sub-ranges in method order — a
    /// method's handlers are all appended while that method's paragraphs bind (<c>StatementBinder.BindMethodRoster</c>'s
    /// pc-order second loop). <see cref="HandlerCount"/> == 0 for a method with no F3 PERFORM (the byte-identical
    /// default); the emitter includes <c>[HandlerStartPc .. HandlerStartPc+HandlerCount−1]</c> as extra cases in this
    /// method's <c>__MDispatch</c> and emits the method-local F3 machinery. Stamped by <c>BindMethodRoster</c>.</summary>
    public int HandlerStartPc { get; set; }
    public int HandlerCount { get; set; }
}
