// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Frontend.Generated;

using WiseOwl.Cobol.Compiler.Oo;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>The shared spine of the whole-group middle-end passes (P6 Step 3): the parse ROOT (the terminal
/// <c>VersionConformancePass</c>'s parse-tree arm walks it — carrying it on the GROUP context puts no parse context
/// on any bound NODE), the collected units + class units, and the group-bind session (turn state, class table,
/// edition, uid bands). Deliberately does NOT carry the OO bind driver — no group pass needs the
/// emitter-hosted OO bind bodies (they all run BEFORE the tail), so a pass reaching for emit state is a compile
/// error by construction. <see cref="SkeletonClasses"/> / <see cref="SkeletonInterfaceData"/> are the unexpanded
/// PARAMETERIZED definitions' forests (OO deep-dive D12 determination 6): bound for their own rules, never emitted, so
/// they are deliberately OUTSIDE <see cref="AllBinders"/> (no producing pass runs over them) and are read only by the
/// edition pass, which gates their data and walks their bound method bodies (kb/Work PB2937).
/// <para>⛔ <see cref="Texts"/>, NOT <see cref="Tree"/>, IS WHAT A SCREEN OF THE SOURCE TEXT WALKS (kb/Work PB4483). The written tree
/// does not hold the text of an expansion of a skeleton read from the repository (it is lexed from the recorded text and parsed
/// apart, <c>OoExpansion</c>), so a parse-tree screen that walked only <see cref="Tree"/> never asked it: the edition gates of the
/// parse-tree arm (COBOL0903 for NEXT SENTENCE), the expression-formation, declined-facility and closed-format screens. An expansion in
/// an assembly of its own got them from its own compilation and one in the group's assembly got none. <see cref="Tree"/> stays for
/// the one walk that is about the written SOURCE and not its text: the <c>&gt;&gt;FLAG</c> directives bracket lines the user wrote,
/// and the recorded state of a skeleton does not carry them (<c>CompileState</c>), so its expansion's own compilation flags nothing
/// either (<c>FlagConformancePass</c>). <c>GroupTextScreenDriftTests</c> holds every other pass to <see cref="Texts"/>.</para></summary>
/// <param name="Tree">The written compilation unit.</param>
/// <param name="Texts">Every text this compilation screens: <paramref name="Tree"/> first, then each expansion of a skeleton read from
/// the repository whose recorded state binds as the group's (<c>BinderDriver.CollectUnits</c>).</param>
internal sealed record GroupBindContext(
    CobolParserCore.CompilationUnitContext Tree,
    IReadOnlyList<Antlr4.Runtime.ParserRuleContext> Texts,
    IReadOnlyList<BoundUnit> Units,
    IReadOnlyList<OoClassUnit> Classes,
    IReadOnlyDictionary<OoInterfaceSymbol, DataBinder> InterfaceData,
    IReadOnlyList<OoClassUnit> SkeletonClasses,
    IReadOnlyDictionary<OoInterfaceSymbol, DataBinder> SkeletonInterfaceData,
    BindSession Session)
{
    /// <summary>Every DataBinder of the group, in the fused pipeline's order: class OBJECT + FACTORY forests,
    /// then the program units. THE one group-forest enumerator — the pass bodies and the watermark
    /// advance/require loops all ride it. (The INTERFACE forests are deliberately separate —
    /// <see cref="InterfaceData"/> — they carry prototype formals only and joined the pass inputs at P5.7 so the
    /// storage-level harmonize can classify both sides of an implements pair.)</summary>
    public IEnumerable<DataBinder> AllBinders() =>
        Classes.SelectMany(c => new[] { Session.Binders.ObjectOf(c).Data, Session.Binders.FactoryOf(c).Data })
            .Concat(Units.Select(u => Session.Binders.Of(u).Data));

    /// <summary>The group forests PLUS the interface prototype forests (the storage-form pass's full domain).</summary>
    public IEnumerable<DataBinder> AllBindersAndInterfaces() => AllBinders().Concat(InterfaceData.Values);
}

/// <summary>
/// One declared GROUP pass of the bind pipeline (P6 Step 3): the <see cref="IPassInfo"/> contract plus the work it
/// runs over the WHOLE compilation (<see cref="GroupBindContext"/>). These are the middle-end passes that need
/// every unit's BOUND tree — procedure binding, whole-group usage collection, the storage-form computation — and,
/// terminally, the <c>VersionConformancePass</c>. The manifest is <c>BindPipeline.GroupTail</c>; the runner is
/// <c>BinderDriver.Bind</c>; the order is validated together with the per-unit prefix by
/// <c>BindPipeline.ValidateFullChainOnce</c>.
/// </summary>
internal sealed record GroupBindPass(string Name, PassPhase Requires, PassPhase Produces, Action<GroupBindContext> Body)
    : IPassInfo
{
    /// <inheritdoc/>
    public PassViews Views { get; init; }

    public void Run(GroupBindContext ctx) => Body(ctx);
}
