// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Cst;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// ⛔ THE OPERANDS OF A STATEMENT THE COMPILER ACCEPTS INERT ARE STILL IDENTIFIERS (kb/Work PB3568). A DECLINED facility's STATEMENT — VALIDATE (§2023:14.9.50,
/// Annex A.4.14) — binds to a no-op, so the facility does nothing; but declining a facility licenses its BEHAVIOUR to be absent, never its SYNTAX RULES: an
/// undefined name, a reference-modified operand and a pointer operand compiled clean because nothing ever resolved them (GnuCOBOL refused the first, and
/// was right). This collaborator is the ONE place the declined statements' identifiers are read, so the next declined statement is one more method here,
/// never one more unbound parse subtree. It builds no node and stores nothing: the resolution is for its DIAGNOSTICS.
/// <para>SEND and RECEIVE left this class when the asynchronous messaging facility was implemented (<see cref="MessagingBinder"/>, kb/Work PB4743); they
/// are still routed through the declined-statement seam until the exchange they build is rendered (kb/Work PB4744).</para>
/// <para>⚠ A DEFERRED reference shape (<see cref="RefOutcome.Deferred"/>: a legal reference whose storage place this compiler has not built) is taken back
/// off the unbuilt-operand ledger by the seam that calls this class (<c>StatementBinder.BindDeclinedStatement</c>). The ledger exists so the statement
/// funnel can announce "not implemented — reaching this statement aborts the run unit"; an inert statement never reaches the storage, so the announcement
/// would be false, and a legal operand is not refused for a place nothing reads.</para>
/// </summary>
internal sealed class DeclinedFacilityBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind the operands of <c>VALIDATE { identifier-1 } …</c> (ISO §2023:14.9.50.2): each identifier-1 is
    /// resolved and screened against §2023:14.9.50.3 SR1, SR2, SR4 and SR5. SR6 (more than one identifier in an
    /// exception-checking PERFORM) is the EC binder's (COBOL1607) and SR3 (a VALIDATE-STATUS clause referencing the
    /// operand) reads a data-division clause the declined facility refuses by name (COBOL1708), so neither is
    /// asked here.</summary>
    public void BindValidate(Core.ValidateFacilityStatementContext v)
    {
        foreach (var dref in v.dataReference()) ScreenValidateOperand(dref);
    }

    /// <summary>One identifier-1 of VALIDATE against §2023:14.9.50.3. The reference-modifier (SR1) is read off the written
    /// reference, before resolution, so a numeric item's reference modification — a shape the resolver defers — is
    /// refused too; the rest ask the resolved place. An unresolvable reference was reported by the resolver.</summary>
    private void ScreenValidateOperand(Core.DataReferenceContext dref)
    {
        string written = DataBinder.WrittenText(dref);
        if (ReferenceResolver.ReadWritten(dref).IsReferenceModified)
        {
            ctx.Edition.Error(DiagnosticCatalog.ValidateOperandForm,
                $"VALIDATE '{written}' — identifier-1 shall not be reference-modified (ISO §2023:14.9.50.3 SR1)");
        }

        // An unresolvable reference was reported by the resolver (an undefined name, a subscript on a non-table item);
        // a deferred shape has no place to screen and is no error (the class remark on a deferred shape).
        if (host.Expr.ResolveSending(dref).Place is not { } place) return;

        if (place.DenotedItem is not { } item) return;   // a reference-modified place: SR1 spoke above

        if (item.Section is null)
        {
            ctx.Edition.Error(DiagnosticCatalog.ValidateOperandForm,
                $"VALIDATE '{written}' — identifier-1 shall reference a data item described in the file, linkage, "
                + "local-storage, or working-storage section (ISO §2023:14.9.50.3 SR1)");
        }

        if (IntrinsicArgumentRules.ClassOfPlace(place) is CobolClass.Index or CobolClass.MessageTag or CobolClass.Object or CobolClass.Pointer)
        {
            ctx.Edition.Error(DiagnosticCatalog.ValidateOperandClass,
                $"VALIDATE '{written}' — identifier-1 is of class {IntrinsicArgumentRules.ClassNameOf(place)}, and "
                + "VALIDATE shall not reference a data item of class index, message-tag, object, or pointer "
                + "(ISO §2023:14.9.50.3 SR2)");
        }

        if (item.Level == 66)
        {
            ctx.Edition.Error(DiagnosticCatalog.ValidateOperandDescription,
                $"VALIDATE '{written}' — identifier-1 shall not reference a 66-level entry (ISO §2023:14.9.50.3 SR4)");
        }

        if (item.IsAnyLength)
        {
            ctx.Edition.Error(DiagnosticCatalog.ValidateOperandDescription,
                $"VALIDATE '{written}' — identifier-1 shall not be described with the ANY LENGTH clause "
                + "(ISO §2023:14.9.50.3 SR5)");
        }
    }
}
