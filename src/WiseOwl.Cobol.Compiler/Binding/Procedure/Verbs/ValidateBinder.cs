// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Cst;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// ⛔ THE VALIDATE STATEMENT (§2023:14.9.50; ISO/IEC 1989:2002 §14.8.46; docs/rearchitecture/DESIGN-validate-facility.md). Each identifier-1 is resolved,
/// screened against the statement's syntax rules (§2023:14.9.50.3 SR1, SR2, SR4, SR5; SR3 reads the VALIDATE-STATUS clause, built in stage S5; SR6 is the
/// exception-checking PERFORM's, <c>EcBinder.ExceptionPerform</c>) and expanded at bind time into a <see cref="BoundValidate"/> over "the elements of the
/// operand" (GR2), as INITIALIZE expands into its stores. A statement naming several identifiers is one node per identifier-1 in a
/// <see cref="BoundImplicitSeries"/> (GR3), so a RESUME NEXT STATEMENT from a declarative lands at the next implicit VALIDATE.
/// <para>Stage one (GR6 a) is the only stage built so far: each elementary item is checked for compatibility with its PICTURE, SIGN and USAGE through
/// the class condition the standard's own class tests define (§2023:8.8.4.4.4 GR3 n), b), e)), bound here as a <see cref="BoundClassCondition"/> so the
/// emitter renders the one condition renderer's answer. A table below identifier-1 is a loop over its occurrences (GR7 b); a part of the operand whose storage a cursor cannot reach is refused by
/// name (COBOL7363): a validation covering part of the operand would call a record valid that the standard calls invalid.</para>
/// </summary>
internal sealed class ValidateBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind <c>VALIDATE { identifier-1 } …</c> (§2023:14.9.50.2).</summary>
    public BoundStatement Bind(Core.ValidateFacilityStatementContext v)
    {
        var members = new List<BoundStatement>();
        foreach (var dref in v.dataReference())
            if (BindOperand(dref) is { } node) members.Add(node);
        return BoundImplicitSeries.Of(members);
    }

    /// <summary>One identifier-1: screened, then expanded into its elements. Null when the operand was refused (the diagnostic is reported).</summary>
    private BoundValidate? BindOperand(Core.DataReferenceContext dref)
    {
        if (ScreenValidateOperand(dref) is not { } place) return null;
        string written = DataBinder.WrittenText(dref);
        if (PlaceCursor.Over(place) is not { } cursor)
        {
            Unbuilt(written, "its storage form is one this stage cannot reach");
            return null;
        }
        var steps = new List<ValidateStep>();
        return Expand(cursor, steps, written, OccurrenceKey(place)) ? new BoundValidate(place, steps) : null;
    }

    /// <summary>The occurrence numbers identifier-1's own reference already pins (<see cref="PlaceCursor.OccurrencePositions"/>) — the subscripts a
    /// REDEFINES view below it is addressed by (<c>PlaceBuilder.PlaceForItem</c>) — or null when the place has no structural path to read them from
    /// (a view's window) and sits in a table, where no count of them is known.</summary>
    private static IReadOnlyList<Position>? OccurrenceKey(Place place) =>
        PlaceCursor.OccurrencePositions(place) ?? (place.DenotedItem is { SubscriptArity: 0 } ? [] : null);

    private int _loopVar;   // program-unique loop-counter ordinal

    /// <summary>The next per-occurrence loop counter of the unit, by its identity (kb/Work PB4321): the backend names it.</summary>
    private MethodLocal NextLoopVar() => new StatementLocal(StatementLocalRole.ValidateOccurrence, _loopVar++);

    /// <summary>The cursor at <paramref name="child"/>, an immediate subordinate of <paramref name="cur"/>'s item, with <paramref name="key"/> the
    /// occurrence numbers of the tables above it (its own dimension included when it is the table). A REDEFINES view that is not its class's canonical
    /// member has no structural cursor (<c>MemberCursor.Child</c> stays null for it: INITIALIZE skips a redefinition, §2023:14.9.20.4 GR5 a 3), so it is
    /// built the way a reference to it is: through <c>PlaceBuilder.PlaceForItem</c> at those occurrence numbers.</summary>
    private PlaceCursor? ChildCursor(PlaceCursor cur, DataItem child, IReadOnlyList<Position>? key)
    {
        if (cur.Child(child) is { } structural) return structural;
        if (child.Class is null || key is null) return null;
        return ctx.Refs.Places.PlaceForItem(child, key) is { } place ? PlaceCursor.Over(place) : null;
    }

    /// <summary>The elements of the operand in definition order (§2023:14.9.50.4 GR2), each with its stage-one test. A REDEFINES entry is an element
    /// like any other: §2002:13.16.42.3 GR3 "each definition is used by the VALIDATE statement independently of the others, subjecting the same
    /// data locations to more than one set of checks" (the PRESENT WHEN clause, stage S4, selects among them). A table is a loop over its
    /// occurrences (GR7 b). False when part of the operand is one this stage does not build (reported as COBOL7363).</summary>
    private bool Expand(PlaceCursor cur, List<ValidateStep> steps, string written, IReadOnlyList<Position>? key)
    {
        var item = cur.Item;
        if (item.IsElementary)
        {
            var place = cur.ToPlace();
            if (IntrinsicArgumentRules.ClassOfPlace(place) is not (CobolClass.Index or CobolClass.Object or CobolClass.Pointer or CobolClass.MessageTag)
                && FormatTest(place, item) is { } test)
                steps.Add(new ValidateElement(place, test));
            return true;
        }
        if (!item.IsGroup) return true;
        foreach (var child in item.Children)
        {
            if (!(child.IsGroup || child.IsElementary) || child.Renames is not null) continue;   // no storage of its own (a 66-level entry)
            string name = child.CobolName ?? "FILLER";
            if (!child.IsTable)
            {
                if (ChildCursor(cur, child, key) is not { } childCursor)
                {
                    Unbuilt(written, $"the storage of '{name}' is one this stage cannot reach");
                    return false;
                }
                if (!Expand(childCursor, steps, written, key)) return false;
                continue;
            }
            // GR7 b: each occurrence is processed independently - one loop over the table's current occurrences (GR7 c: the DEPENDING item's value).
            var v = NextLoopVar();
            Position occurrence = new PositionLocal(v);
            IReadOnlyList<Position>? inner = key is null ? null : [.. key, occurrence];
            var tableCursor = cur.Child(child);
            PlaceCursor? element = tableCursor is null ? ChildCursor(cur, child, inner)
                : child.IsDynamicTable ? tableCursor.DynamicElement(occurrence)
                : tableCursor.Indexed(occurrence);
            if (element is null || OccurrenceCounts.Current(child, tableCursor?.StoragePath, ctx.Refs.Places) is not { } count)
            {
                Unbuilt(written, $"the occurrences of the table '{name}' cannot be reached or counted here");
                return false;
            }
            var body = new List<ValidateStep>();
            if (!Expand(element, body, written, inner)) return false;
            if (body.Count > 0) steps.Add(new ValidateLoop(v, count, body));
        }
        return true;
    }

    /// <summary>Stage one's test for one elementary item (§2023:14.9.50.4 GR6 a: "compatibility with the PICTURE, SIGN, and USAGE clauses"): the content
    /// a numeric item holds is a number (the numeric class test, §2023:8.8.4.4.4 GR3 n), an alphabetic item holds only letters and spaces (GR3 b) and a
    /// boolean item only 0 and 1 (GR3 e). Null for an item every content suits.</summary>
    private static BoundCondition? FormatTest(Place place, DataItem item)
    {
        if (item.Pic is not { } pic) return null;
        char? kind = pic.Category switch
        {
            PicCategory.Numeric when pic.IsClassNumeric => 'N',
            PicCategory.Boolean => 'B',
            PicCategory.Alphanumeric when pic.IsAlphabetic => 'A',
            _ => null,
        };
        return kind is { } k ? new BoundClassCondition(new BoundFieldOperand(place), k, Negated: false) : null;
    }

    private void Unbuilt(string written, string why) =>
        ctx.Edition.Error(DiagnosticCatalog.ValidateOperandUnbuilt,
            $"VALIDATE '{written}': {why}; the VALIDATE facility is built in stages (docs/rearchitecture/DESIGN-validate-facility.md section 6) and a "
            + "validation of part of the operand would report as valid a record the standard calls invalid (ISO §2023:14.9.50.4 GR7)");

    /// <summary>One identifier-1 of VALIDATE against §2023:14.9.50.3. The reference-modifier (SR1) is read off the written
    /// reference, before resolution, so a numeric item's reference modification — a shape the resolver defers — is
    /// refused too; the rest ask the resolved place. An unresolvable reference was reported by the resolver. The resolved place,
    /// or null when the operand is refused.</summary>
    private Place? ScreenValidateOperand(Core.DataReferenceContext dref)
    {
        string written = DataBinder.WrittenText(dref);
        bool ok = true;
        if (ReferenceResolver.ReadWritten(dref).IsReferenceModified)
        {
            ctx.Edition.Error(DiagnosticCatalog.ValidateOperandForm,
                $"VALIDATE '{written}' — identifier-1 shall not be reference-modified (ISO §2023:14.9.50.3 SR1)");
            ok = false;
        }

        // An unresolvable reference was reported by the resolver (an undefined name, a subscript on a non-table item).
        if (host.Expr.ResolveSending(dref).Place is not { } place) return null;

        if (place.DenotedItem is not { } item) return null;   // a reference-modified place: SR1 spoke above

        if (item.Section is null)
        {
            ctx.Edition.Error(DiagnosticCatalog.ValidateOperandForm,
                $"VALIDATE '{written}' — identifier-1 shall reference a data item described in the file, linkage, "
                + "local-storage, or working-storage section (ISO §2023:14.9.50.3 SR1)");
            ok = false;
        }

        if (IntrinsicArgumentRules.ClassOfPlace(place) is CobolClass.Index or CobolClass.MessageTag or CobolClass.Object or CobolClass.Pointer)
        {
            ctx.Edition.Error(DiagnosticCatalog.ValidateOperandClass,
                $"VALIDATE '{written}' — identifier-1 is of class {IntrinsicArgumentRules.ClassNameOf(place)}, and "
                + "VALIDATE shall not reference a data item of class index, message-tag, object, or pointer "
                + "(ISO §2023:14.9.50.3 SR2)");
            ok = false;
        }

        if (item.Level == 66)
        {
            ctx.Edition.Error(DiagnosticCatalog.ValidateOperandDescription,
                $"VALIDATE '{written}' — identifier-1 shall not reference a 66-level entry (ISO §2023:14.9.50.3 SR4)");
            ok = false;
        }

        if (item.IsAnyLength)
        {
            ctx.Edition.Error(DiagnosticCatalog.ValidateOperandDescription,
                $"VALIDATE '{written}' — identifier-1 shall not be described with the ANY LENGTH clause "
                + "(ISO §2023:14.9.50.3 SR5)");
            ok = false;
        }

        return ok ? place : null;
    }
}
