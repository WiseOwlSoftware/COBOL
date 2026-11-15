// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Common;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;
using WiseOwl.Cobol.Runtime;

// ⛔ THE PADDING CHARACTER CLAUSE'S OPERAND RULES, IN ONE PLACE (kb/Work PB5070): CapturePadding and
// ScreenPaddingDataName. The clause exists at COBOL-85
// (§1985:VII.2.7) and COBOL-2002 (§2002:12.3.4.10, obsolete there) and is deleted from 2014 (the version gate,
// VersionConformancePass.VisitPaddingCharacterClause). WiseOwl COBOL has no blocking model, so the padding character
// changes no I-O (§1985:VII.2.7.4 GR2: "If the PADDING CHARACTER clause is not applicable to the device type to which the
// file is assigned, the creation or recognition of padding characters does not occur"); the clause's syntax rules are
// still rules of the source program and are enforced, and the clause as written is part of an external file connector's
// entry identity (FileEntryIdentity, kb/Work PB5069).
// - Literal-1: §1985:VII.2.7.3 SR1 "Literal-1 must be a one-character nonnumeric literal", §2002:12.3.4.10.2 SR1 "a
// one-character alphanumeric literal". A figurative constant stands for one character here (its length is "determined by
// the compiler from context"); an ALL literal does only when its literal is one character.
// - Data-name-1: §1985:VII.2.7.3 SR3 "a one-character data item of the category alphanumeric" that "must not be defined
// in the Communication Section, the File Section, or the Report Section"; §2002:12.3.4.10.2 SR3 "defined in the
// working-storage, local-storage, or linkage section". The two name the same sections (COBOL-85 has no local-storage
// section). Its resolution (an undefined or ambiguous name) is the one clause-operand resolver's.
public sealed partial class FileControlBinder
{
    /// <summary>Capture the clause as written and screen literal-1 (data-name-1 is screened once resolved,
    /// <see cref="ScreenPaddingDataName"/>).</summary>
    private PaddingCharacterSpec CapturePadding(Core.PaddingCharacterClauseContext clause)
    {
        if (clause.dataReference() is { } dref)
        {
            using var _ = Edition.At(dref);
            // A constant-name stands where the format specifies literal-1 (§2023:13.10.3 SR2; constant entries begin at 2002):
            // the word is literal-1 when the program declares it a constant, data-name-1 otherwise.
            var reading = _host.LiteralEnv.ReadConstant(dref.GetText());
            if (reading.Kind is ConstantReadingKind.Refused)
                return new PaddingCharacterSpec { Literal = dref.GetText(), At = Edition.Cursor };
            if (reading.Kind is not ConstantReadingKind.NotAConstant)
            {
                string? one = reading.Value.Category is PicCategory.Alphanumeric && reading.Value.Value.Length == 1
                    ? reading.Value.Value : null;
                if (one is null) ReportPaddingLiteral(dref.GetText(), $"the constant-name '{dref.GetText()}' stands for literal-1, ");
                return new PaddingCharacterSpec { Literal = one ?? dref.GetText(), At = Edition.Cursor };
            }
            // A symbolic-character is a figurative constant (§1985:IV.4.2.2.2.3 GR7; §2023:8.3.3.6.3 SR4), so it stands where the
            // format specifies literal-1 and is read before the word is tried as data-name-1 (kb/Work PB5420).
            if (SymbolicCharacter(dref, dref.GetText(), _host.LiteralEnv) is { } symbolic)
                return new PaddingCharacterSpec { Literal = symbolic, At = Edition.Cursor };
            if (_host.LiteralEnv.IsSymbolicCharacter(dref.GetText()))   // a national symbolic-character: literal-1, but not alphanumeric
            {
                ReportPaddingLiteral(dref.GetText(), "");
                return new PaddingCharacterSpec { Literal = dref.GetText(), At = Edition.Cursor };
            }
            var (name, quals) = _host.ClauseDataName(dref, "PADDING CHARACTER");
            return new PaddingCharacterSpec { DataName = name, Qualifiers = quals, At = Edition.Cursor };
        }
        var lit = clause.literal();
        using var at = Edition.At(lit);
        if (lit.nonNumericLiteral()?.predefinedNull() is not null)
        {
            PredefinedNullRule.Report(Edition, "literal-1 of the PADDING CHARACTER clause, which shall be a one-character "
                + "alphanumeric literal (ISO §1985:VII.2.7.3 SR1),");
            return new PaddingCharacterSpec { Literal = lit.GetText(), At = Edition.Cursor };
        }
        string? value = OneCharacter(lit, _host.LiteralEnv);
        if (value is null) ReportPaddingLiteral(ConcatFolder.Spelling(lit), "");
        return new PaddingCharacterSpec { Literal = value ?? lit.GetText(), At = Edition.Cursor };
    }

    /// <summary>The ONE report of literal-1's rule (COBOL6848), at the cursor the caller set; <paramref name="via"/> names a
    /// constant-name that stands for the literal.</summary>
    private void ReportPaddingLiteral(string written, string via) =>
        Edition.Error(DiagnosticCatalog.PaddingCharacterOperand, $"PADDING CHARACTER IS {written}: {via}literal-1 is not a "
            + "one-character " + (Edition.DialectLevel < 2002
                ? "nonnumeric literal (ISO §1985:VII.2.7.3 SR1: \"Literal-1 must be a one-character nonnumeric literal.\")"
                : "alphanumeric literal (ISO §2002:12.3.4.10.2 SR1: \"Literal-1 shall be a one-character alphanumeric literal.\")"));

    /// <summary>The one character literal-1 stands for, or null when it is not a one-character alphanumeric literal.</summary>
    private static string? OneCharacter(Core.LiteralContext lit, LiteralEnvironment env)
    {
        if (lit.nonNumericLiteral() is not { } nn) return null;   // a numeric literal
        if (nn.figurativeConstant() is { } fig)
        {
            // Format 6, ALL literal-1 or ALL constant-name (§2023:8.3.3.6.3 SR2), is read by its one reader and stands for one
            // character when its literal is one alphanumeric character.
            // A numeric constant-name is no literal-1 of ALL (§2023:8.3.3.6.3 SR2): FoldAllLiteral's best-effort value for it must not stand.
            if (fig.ALL() is not null && fig.cobolWord()?.GetText() is { } k && env.Constant(k) is { Category: not (PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean) })
                return null;
            if (ConcatFolder.FoldAllLiteral(fig, env, report: null) is { } folded)
                return folded.Category is PicCategory.Alphanumeric && folded.Value.Length == 1 ? folded.Value : null;
            // Format 7, ALL symbolic-character-1, one or more of the character the symbolic-character names (§1985:IV.4.2.2.2.3
            // GR7): one character here, so the ALL is redundant.
            if (fig.ALL() is not null && fig.cobolWord()?.GetText() is { } word)
                return SymbolicCharacter(fig, word, env);
            return ConcatFolder.FigurativeChar(fig, PicCategory.Alphanumeric, env) is { } c ? c.ToString() : null;
        }
        // NULL is an identifier, not a literal (§2023:8.4.3.10.3 SR1): the one refusal, not the literal-shape verdict.
        if (nn.predefinedNull() is not null) return null;
        if (nn.STRINGLIT() is null && nn.HEXLIT() is null) return null;   // a concatenation, national or boolean literal
        string token = nn.GetText();
        return CobolLiteral.ClassOf(token) == LiteralClass.Alphanumeric && CobolLiteral.Decode(token) is { Length: 1 } v ? v : null;
    }

    /// <summary>The one character <paramref name="word"/> stands for when it names an alphanumeric symbolic-character bound at
    /// <paramref name="site"/>, else null (a symbolic-character is a figurative constant, §1985:IV.4.2.2.2.3 GR7; a national
    /// one is no one-character alphanumeric literal, §2002:12.3.4.10.2 SR1).</summary>
    private static string? SymbolicCharacter(Antlr4.Runtime.RuleContext site, string word, LiteralEnvironment env) =>
        env.IsSymbolicCharacter(word) && env.SymbolicOf(site, word) is { National: false } sym ? sym.Value : null;

    /// <summary>Screen a resolved data-name-1 (an unresolved one was reported by its resolution).</summary>
    private void ScreenPaddingDataName(PaddingCharacterSpec pad)
    {
        if (pad.Item is not { } item) return;
        // A table element is not nameable here (kb/Work PB5499): "Data-name-1 may be qualified" is the clause's only reference form
        // (§1985:VII.2.7.3 SR2), no subscript is written (CapturePadding refuses one, ScreenClauseOperandShape), and a table element
        // reference "must be subscripted" (§1985:IV.4.3.8.2.3 SR4; §2002:8.4.1.2.2 SR5): an item under an OCCURS clause has no reading.
        bool table = RecordLayout.IsSubjectToOccurs(item);
        string? fault = table
            ? "is subject to an OCCURS clause, so every reference to it is a table element reference that shall be subscripted, "
              + "yet the clause's data-name-1 may be qualified and takes no subscript "
              + (Edition.DialectLevel < 2002 ? "(ISO §1985:IV.4.3.8.2.3 SR4 and §1985:VII.2.7.3 SR2)" : "(ISO §2002:8.4.1.2.2 SR5)")
            : !ItemCategory.IsAlphanumeric(item) ? $"is {ItemCategory.Face(item)}, not of the category alphanumeric"
            : item.ByteWidth != 1 || DataItem.HasOdoOnOrBeneath(item)
                ? $"is {item.ByteWidth} character positions long, not a one-character data item"
            : item.Section is { } s and not (EntrySection.WorkingStorage or EntrySection.LocalStorage or EntrySection.Linkage)
                ? $"is defined in the {DataBinder.SectionWords(s)}"
            : null;
        if (fault is null) return;
        using var _ = Edition.At(pad.At);
        Edition.Error(DiagnosticCatalog.PaddingCharacterOperand, $"PADDING CHARACTER IS "
            + $"{DataBinder.WrittenQualified(pad.DataName!, pad.Qualifiers)}: data-name-1 {fault} "
            + (table ? "" : Edition.DialectLevel < 2002
                ? "(ISO §1985:VII.2.7.3 SR3: \"Data-name-1 must be defined in the Data Division as a one-character data item of "
                  + "the category alphanumeric and must not be defined in the Communication Section, the File Section, or the "
                  + "Report Section.\")"
                : "(ISO §2002:12.3.4.10.2 SR3: \"Data-name-1 shall be a one-character data item of category alphanumeric, "
                  + "defined in the working-storage, local-storage, or linkage section.\")"));
    }
}
