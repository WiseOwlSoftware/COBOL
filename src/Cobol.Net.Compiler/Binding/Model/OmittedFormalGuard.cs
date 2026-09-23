// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

namespace CobolNet.Binding.Model;

/// <summary>
/// ⛔ <b>THE REFERENCE-TIME *-ARG-OMITTED FACT OF A FORMAL PARAMETER</b> (kb/Work PB971) — carried by the formal's
/// root <see cref="DataItem"/> (<see cref="DataItem.OmittedGuard"/>) and copied onto the ROOT segment of every
/// <see cref="AccessPath"/> built from that item, so every reference the backend renders through the path is
/// checked, whatever verb or operand position produced it. ISO §14.9.4.4 GR12 (program), §8.4.3.2.4 GR8 (function)
/// and §14.9.23.4 GR10 (method) each set the kind's condition to exist when "a parameter for which the
/// omitted-argument condition is true is referenced … except as an argument or in the omitted-argument
/// condition"; the two exemptions never render the path (the condition reads <see cref="OmittedProbe"/>; a
/// forwarded whole formal forwards its carrier), so they need no list here.
/// <para>Set only when the compilation group can enable the kind's condition at all
/// (<c>TurnState.AnyEnabledFor</c>): with no such >>TURN the reference renders exactly as before — the
/// zero-scaffolding invariant — because the raise could never fire.</para>
/// </summary>
/// <param name="Presence">The C# boolean expression, valid in any class that can reference the formal, that is
/// TRUE when the argument was omitted: a program/function formal's <c>__omit{Uid}</c> member (a Uid-keyed name, so
/// a contained program's GLOBAL bridge of it cannot collide with its own formals), a method formal's
/// <c>__omittedN</c> presence parameter.</param>
/// <param name="Kind">The activated element that owns the formal — which of the three conditions is raised.</param>
/// <param name="FormalName">The COBOL name, for the diagnostic detail.</param>
/// <param name="CarrierPrefix">Non-null when the root segment's field text BEGINS with a reference-type carrier
/// that is passed through the guard rather than taken by <c>ref</c>: a carrier-resident formal's
/// <c>__lnkpN</c> (its field text is <c>__lnkpN.Value</c>) or a cell-backed class's <c>StorageCell</c>.</param>
public sealed record OmittedFormalGuard(string Presence, ActivatedElementKind Kind, string FormalName,
    string? CarrierPrefix = null)
{
    /// <summary>The program/function arm's presence member name for <paramref name="formal"/>.</summary>
    public static string PresenceMember(DataItem formal) => $"__omit{formal.Uid}";

    /// <summary>The guard a root segment over <paramref name="root"/> carries: the item's own, else — for a
    /// Tier-A/Tier-B view of a REDEFINES class whose canonical is the formal — the canonical's (a redefinition
    /// of the formal names the formal's storage).</summary>
    public static OmittedFormalGuard? Of(DataItem root) => root.OmittedGuard ?? root.Class?.Canonical.OmittedGuard;

    /// <summary>⛔ THE ONE RENDERING of a guarded root (kb/Work PB971): <paramref name="rootText"/> — the root's
    /// field text, possibly <c>__outer.</c>-prefixed — wrapped in the runtime guard, which raises the kind's
    /// condition when the argument was omitted and yields the SAME storage (by <c>ref</c>, or the carrier passed
    /// through), so the wrapped text is still an lvalue for a store, a member access or a table accessor.
    /// <para>It lives on the model rather than in <c>PlaceRenderer</c> because TWO renderers emit root text: the
    /// structural <c>PlaceRenderer.RenderPath</c> and the resolver's D10 transitional string paths (subscript /
    /// ref-mod positions, whole-table paths) — the same bind-time C# text the resolver already spells for
    /// <c>CobolTable.Occ</c>. One method, so the two cannot drift; it moves to the renderer with the rest of the
    /// string carrier at PHASE 15.</para></summary>
    public string Render(string rootText)
    {
        string tail = $"{Presence}, {nameof(ActivatedElementKind)}.{Kind}, \"{FormalName}\")";
        if (CarrierPrefix is not { } carrier)
            return $"{nameof(OmittedFormal)}.{nameof(OmittedFormal.Ref)}(ref {rootText}, {tail}";
        // The carrier is the leading reference-type part of the root text ("__lnkp0" of "__lnkp0.Value", after
        // any "__outer." re-anchoring); the rest ("…Value") applies to what the guard hands back.
        int at = rootText.IndexOf(carrier, System.StringComparison.Ordinal);
        if (at < 0)
            throw new System.InvalidOperationException(
                $"OmittedFormalGuard: root text '{rootText}' does not contain its carrier '{carrier}' (kb/Work PB971)");
        int end = at + carrier.Length;
        return $"{nameof(OmittedFormal)}.{nameof(OmittedFormal.Carrier)}({rootText[..end]}, {tail}{rootText[end..]}";
    }
}
