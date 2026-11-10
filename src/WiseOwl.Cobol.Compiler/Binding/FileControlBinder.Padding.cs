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
            var (name, quals) = _host.ClauseDataName(dref, "PADDING CHARACTER");
            using var _ = Edition.At(dref);
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
        if (value is null)
            Edition.Error(DiagnosticCatalog.PaddingCharacterOperand, $"PADDING CHARACTER IS {lit.GetText()}: literal-1 is not a "
                + "one-character " + (Edition.DialectLevel < 2002
                    ? "nonnumeric literal (ISO §1985:VII.2.7.3 SR1: \"Literal-1 must be a one-character nonnumeric literal.\")"
                    : "alphanumeric literal (ISO §2002:12.3.4.10.2 SR1: \"Literal-1 shall be a one-character alphanumeric literal.\")"));
        return new PaddingCharacterSpec { Literal = value ?? lit.GetText(), At = Edition.Cursor };
    }

    /// <summary>The one character literal-1 stands for, or null when it is not a one-character alphanumeric literal.</summary>
    private static string? OneCharacter(Core.LiteralContext lit, LiteralEnvironment env)
    {
        if (lit.nonNumericLiteral() is not { } nn) return null;   // a numeric literal
        if (nn.figurativeConstant() is { } fig)
        {
            if (fig.allLiteral() is { } all)
                return CobolLiteral.ClassOf(all.GetText()) == LiteralClass.Alphanumeric
                    && CobolLiteral.Decode(all.GetText()) is { Length: 1 } one ? one : null;
            return ConcatFolder.FigurativeChar(fig, PicCategory.Alphanumeric, env) is { } c ? c.ToString() : null;
        }
        // NULL is an identifier, not a literal (§2023:8.4.3.10.3 SR1): the one refusal, not the literal-shape verdict.
        if (nn.predefinedNull() is not null) return null;
        if (nn.STRINGLIT() is null && nn.HEXLIT() is null) return null;   // a concatenation, national or boolean literal
        string token = nn.GetText();
        return CobolLiteral.ClassOf(token) == LiteralClass.Alphanumeric && CobolLiteral.Decode(token) is { Length: 1 } v ? v : null;
    }

    /// <summary>Screen a resolved data-name-1 (an unresolved one was reported by its resolution).</summary>
    private void ScreenPaddingDataName(PaddingCharacterSpec pad)
    {
        if (pad.Item is not { } item) return;
        string? fault =
            !ItemCategory.IsAlphanumeric(item) ? $"is {ItemCategory.Face(item)}, not of the category alphanumeric"
            : item.ByteWidth != 1 || DataItem.HasOdoOnOrBeneath(item)
                ? $"is {item.ByteWidth} character positions long, not a one-character data item"
            : item.Section is { } s and not (EntrySection.WorkingStorage or EntrySection.LocalStorage or EntrySection.Linkage)
                ? $"is defined in the {DataBinder.SectionWords(s)}"
            : null;
        if (fault is null) return;
        using var _ = Edition.At(pad.At);
        Edition.Error(DiagnosticCatalog.PaddingCharacterOperand, $"PADDING CHARACTER IS "
            + $"{DataBinder.WrittenQualified(pad.DataName!, pad.Qualifiers)}: data-name-1 {fault} "
            + (Edition.DialectLevel < 2002
                ? "(ISO §1985:VII.2.7.3 SR3: \"Data-name-1 must be defined in the Data Division as a one-character data item of "
                  + "the category alphanumeric and must not be defined in the Communication Section, the File Section, or the "
                  + "Report Section.\")"
                : "(ISO §2002:12.3.4.10.2 SR3: \"Data-name-1 shall be a one-character data item of category alphanumeric, "
                  + "defined in the working-storage, local-storage, or linkage section.\")"));
    }
}
