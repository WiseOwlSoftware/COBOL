// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;

/// <summary>The source element whose procedure division header is bound: what decides the ANY LENGTH placement rules of
/// ISO §2023:13.18.2.3 SR2-SR4, the only rules that differ between the header arms.</summary>
internal enum ProcedureHeaderOwner
{
    /// <summary>An outermost program: no ANY LENGTH item (§2023:13.18.2.3 SR2 and its NOTE).</summary>
    OutermostProgram,
    /// <summary>A contained program: a BY REFERENCE formal or the RETURNING item (SR3).</summary>
    ContainedProgram,
    /// <summary>A function: a BY REFERENCE formal only (SR4).</summary>
    Function,
    /// <summary>A class's factory or instance data: no ANY LENGTH item (SR2).</summary>
    ClassData,
    /// <summary>A method that is not a property method: a BY REFERENCE formal or the RETURNING item (SR3).</summary>
    Method,
    /// <summary>A GET or SET property method: no ANY LENGTH item (SR2).</summary>
    PropertyMethod,
}

/// <summary>One formal parameter as the header binds it, before an arm names its carrier: the LINKAGE item, its 0-based
/// position, its passing mode (§2023:14.2.3 GR4), the OPTIONAL phrase (§2023:14.2.1) and whether it is carrier-resident
/// (<see cref="LinkageBinder.FormalIsCarrierResident"/>).</summary>
internal readonly record struct HeaderFormal(DataItem Item, int Position, bool ByValue, bool Optional, bool CarrierResident);

/// <summary>A bound procedure division header: its formals in order, and its RETURNING item or null.</summary>
internal sealed record ProcedureHeader(IReadOnlyList<HeaderFormal> Formals, DataItem? Returning)
{
    /// <summary>A source element with no procedure division.</summary>
    public static readonly ProcedureHeader None = new([], null);
}

/// <summary>
/// ⛔ THE ONE BINDING OF A PROCEDURE DIVISION HEADER (kb/Work PB3213). The USING and RETURNING phrases of a program, a
/// function and a method follow the same rules, so they are bound here once and each arm (<c>LinkageBinder.CallBindLinkage</c>,
/// <c>OoDataBinder.BindMethodData</c>) only names the carrier of what this returns:
/// <list type="bullet">
/// <item>the using-phrase modes, BY REFERENCE / BY VALUE transitive across the parameters that follow until the other
/// phrase appears and BY REFERENCE assumed before the first (ISO §2023:14.2.3 GR4), with OPTIONAL a BY REFERENCE word
/// (§2023:14.2.1);</item>
/// <item>each operand a level-01/77 LINKAGE entry of the element (§2023:14.2.2 SR1, COBOL0888 from this one site), screened
/// by <see cref="ProcedureHeaderScreen"/> (SR1, SR2, SR5, SR6);</item>
/// <item>which formals are carrier-resident (§2023:14.2.3 GR8; <see cref="LinkageBinder.FormalIsCarrierResident"/>);</item>
/// <item>the ANY LENGTH placement sweep (§2023:13.18.2.3 SR2-SR4, COBOL1542 and its subject rule from one site each), whose
/// only per-arm input is the <see cref="ProcedureHeaderOwner"/>.</item>
/// </list>
/// Until PB3213 each arm wrote all of this itself, and the area formal (PB2087), OPTIONAL (PB757) and BY VALUE (PB1051) each
/// landed once per arm.
/// </summary>
internal static class ProcedureHeaderBinder
{
    /// <summary>Bind the USING and RETURNING phrases of <paramref name="pd"/> against the element's
    /// <paramref name="linkageRoots"/>. <paramref name="addressedNames"/> is asked only when the header has formals (kb/Work
    /// PB4302: the scan of every ADDRESS OF is wasted on a header with none). <paramref name="where"/> names the element in the
    /// screen's messages; <paramref name="method"/> selects the method arm's spelling of the SR1 messages.</summary>
    internal static ProcedureHeader Bind(DataBinder data, Core.ProcedureDivisionContext pd, IReadOnlyList<DataItem> linkageRoots,
        Func<SymbolTable.AddressedNames> addressedNames, string where, bool method)
    {
        var edition = data.Edition;
        var header = new ProcedureHeaderScreen(edition, where);   // §2023:14.2.2 SR1/SR2/SR5/SR6 — the ONE screen (kb/Work PB1145)
        string prefix = method ? where + ": " : "";
        string owner = method ? " of the method" : "";
        var parameters = pd.usingClause()?.usingParameter() ?? [];
        var addressed = parameters.Length == 0 ? SymbolTable.AddressedNames.None : addressedNames();
        var formals = new List<HeaderFormal>(parameters.Length);
        int pos = 0;
        bool byValue = false;
        foreach (var prm in parameters)
        {
            using var _ = edition.At(prm);
            Core.DataReferenceContext dref;
            bool optional = false;
            if (prm.usingByValue() is { } vb) { byValue = true; dref = vb.dataReference(); }
            else if (prm.usingByReference() is { } rb)
            {
                byValue = false;
                dref = rb.dataReference();
                optional = rb.OPTIONAL() is not null;
            }
            else { dref = prm.dataReference(); optional = prm.OPTIONAL() is not null; }

            string pname = dref.GetText();
            var item = data.Symbols.IdentifyHeaderOperand(linkageRoots, dref);
            if (item is null)
                edition.Error(DiagnosticCatalog.ProcedureHeaderItemNotLinkageRoot,
                    $"{prefix}PROCEDURE DIVISION USING parameter '{WrittenSource.Of(dref)}' is not a level-01/77 LINKAGE SECTION "
                    + $"item{owner} ({edition.Edition.CiteRule("§1985:X.5.1", "§2023:14.2.2 SR1")})");
            // SR1: uniqueness, and no REDEFINES / BASED clause. A REPEATED formal is not admitted a second time (`USING A A`
            // reached the backend as a duplicate carrier member).
            else if (header.AdmitFormal(pname, item))
            {
                if (optional && byValue) header.OptionalNeedsByReference(pname);   // §2023:14.2.1: OPTIONAL is a BY REFERENCE word
                if (byValue)
                {
                    // §2023:14.2.2 SR2: a BY VALUE formal is of class numeric, message-tag, object or pointer, the carried legs
                    // of the §2023:14.2.3 GR10 value copy.
                    data.CompleteDescriptionAhead(item);   // a TYPE-typed formal has no class until its subject is composed (kb/Work PB3222)
                    header.ByValueClass(pname, item);
                }
                formals.Add(new HeaderFormal(item, pos, byValue, optional,
                    LinkageBinder.FormalIsCarrierResident(item, linkageRoots, addressed)));
            }
            pos++;
        }

        DataItem? returning = null;
        if (pd.returningClause()?.dataReference() is { } rref)
        {
            using var returningAt = edition.At(rref);   // the RETURNING item's own position, as each USING parameter's (kb/Work PB2034)
            // §2023:14.2.3 GR6: the returning item's storage is allocated IN THE ACTIVATED element; its value transfers out at
            // termination (GR7).
            returning = data.Symbols.IdentifyHeaderOperand(linkageRoots, rref);
            if (returning is null)
                edition.Error(DiagnosticCatalog.ProcedureHeaderItemNotLinkageRoot,
                    $"{prefix}PROCEDURE DIVISION RETURNING item '{WrittenSource.Of(rref)}' is not a level-01/77 LINKAGE SECTION "
                    + $"item{owner} (ISO §2023:14.2.2 SR1)");
            else
                header.CheckReturning(rref.GetText(), returning);   // SR5 / SR6
        }
        return new ProcedureHeader(formals, returning);
    }

    /// <summary>The ANY LENGTH placement sweep (ISO §2023:13.18.2.3 SR2/SR3/SR4) over <paramref name="candidates"/>, once the
    /// element's sections and header are bound. SR2: an elementary level-1 LINKAGE entry (<paramref name="linkageRoots"/>) of a
    /// function, a contained program or a method that is not a property method: never an outermost program (a prototype-less
    /// CALL cannot associate arguments with an ANY LENGTH formal, the NOTE) and never a class's factory or instance data. SR3
    /// (contained program, method): referenced in the header as a formal WITH THE BY REFERENCE PHRASE (stated or implied; a
    /// BY VALUE formal does not qualify, SR3a) or as the RETURNING item. SR4 (function): as a BY REFERENCE formal only.
    /// A violation clears <see cref="DataItem.IsAnyLength"/> (the IsBased discipline: the item binds as its ordinary
    /// one-character shape under an already-failed compile, never a half-varying state). <paramref name="prefix"/> names the
    /// element in each message ("" for a program or function).</summary>
    internal static void SweepAnyLength(EditionContext edition, IEnumerable<DataItem> candidates, IReadOnlyList<DataItem> linkageRoots,
        ProcedureHeaderOwner owner, ProcedureHeader header, string prefix)
    {
        foreach (var root in candidates)
        {
            if (!root.IsAnyLength) continue;
            using var _ = edition.At(root);   // the entry's own position (kb/Work PB2034)
            string where = $"{prefix}data item '{root.CobolName ?? "FILLER"}'";
            if (!linkageRoots.Contains(root))
                edition.Error(DiagnosticCatalog.AnyLengthPlacement, $"{where}: the ANY LENGTH clause may be specified only in an "
                    + "elementary level 1 entry in the LINKAGE SECTION (ISO §2023:13.18.2.3 SR2)");
            else if (root.IsGroup)
                edition.Error(DiagnosticCatalog.AnyLengthPlacement, $"{where}: the subject of an ANY LENGTH clause shall be "
                    + "ELEMENTARY — this entry has subordinate items (ISO §2023:13.18.2.3 SR2)");
            else if (owner == ProcedureHeaderOwner.ClassData)
                edition.Error(DiagnosticCatalog.AnyLengthPlacement, $"{where}: the ANY LENGTH clause may be specified only in the "
                    + "linkage section of a function, a contained program, or a method that is not a property "
                    + "method — not in a factory or instance definition (ISO §2023:13.18.2.3 SR2)");
            else if (owner == ProcedureHeaderOwner.OutermostProgram)
                edition.Error(DiagnosticCatalog.AnyLengthPlacement, $"{where}: the ANY LENGTH clause may not be specified in an "
                    + "OUTERMOST program — only in a function, a contained program, or a method "
                    + "(ISO §2023:13.18.2.3 SR2 and its NOTE)");
            else if (owner == ProcedureHeaderOwner.PropertyMethod)
                edition.Error(DiagnosticCatalog.AnyLengthPlacement, $"{where}: the ANY LENGTH clause may not be specified in a PROPERTY "
                    + "method (ISO §2023:13.18.2.3 SR2 — a method that is not a property method)");
            else
            {
                bool formal = header.Formals.Any(f => ReferenceEquals(f.Item, root) && !f.ByValue);   // SR3a/SR4: BY REFERENCE only
                bool returning = ReferenceEquals(header.Returning, root);
                if (owner == ProcedureHeaderOwner.Function ? !formal : !(formal || returning))
                    edition.Error(DiagnosticCatalog.AnyLengthSubjectNotInHeader, owner == ProcedureHeaderOwner.Function
                        ? $"{where}: in a FUNCTION the subject of an ANY LENGTH clause shall be referenced in "
                          + "the procedure division header as a formal parameter with the BY REFERENCE phrase "
                          + "(ISO §2023:13.18.2.3 SR4)"
                        : $"{where}: the subject of an ANY LENGTH clause shall be referenced in the procedure division header "
                          + "as a BY REFERENCE formal parameter or as the RETURNING item (ISO §2023:13.18.2.3 SR3)");
                else
                    continue;   // conformant — keep the flag. A RETURNING item (SR3b) takes the ACTIVATOR's receiver length at
                                // activation entry (§2023:13.18.2.4 GR1b; ProgramEmitter.EmitCallMethod, OoEmitter.OoSignatureOf).
            }
            root.IsAnyLength = false;
        }
    }
}
