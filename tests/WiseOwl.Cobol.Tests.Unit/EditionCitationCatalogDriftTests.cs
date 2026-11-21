// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Editions.Diagnostics;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>⛔ EVERY DIAGNOSTIC WHOSE CITATION NAMES A LATER EDITION SAYS WHAT IT CITES AT <c>--std 85</c> (kb/Work PB5993, PB4950).
/// Asked of the catalog, not of the refusals a suite happens to run at 85 (the conformance suite's <c>EditionCitationNet</c> sees
/// only those): a descriptor whose <see cref="DiagnosticDescriptor.IsoSection"/> cites a rule of COBOL-2002, 2014 or 2023 and no
/// 1985 rule carries either <see cref="DiagnosticDescriptor.Rule1985"/>, the 1985 rule its message cites at 85 (the 1985 arm of
/// <c>EditionInfo.Cite</c> at its site), or <see cref="DiagnosticDescriptor.No1985Rule"/>, the reason the 1985 text has no such
/// rule (a construct 1985 lacks, refused at 85 by the edition gate before this diagnostic can be reached, or a rule a later edition
/// added). <see cref="Unmoved"/> lists, by <see cref="DiagnosticDescriptor.Id"/>, the descriptors not yet moved: it RATCHETS DOWN
/// only, a new descriptor that carries neither fails, and a listed one that now carries one fails until it leaves the list.</summary>
public sealed class EditionCitationCatalogDriftTests
{
    /// <summary>A citation of a rule of a later edition than 1985, in the edition form or the prose form.</summary>
    private static readonly Regex LaterEditionCitation =
        new(@"§(2002|2014|2023):|ISO/IEC 1989:(2002|2014|2023)\b", RegexOptions.Compiled);

    /// <summary>A citation of the 1985 standard or its 1989 intrinsic-function amendment.</summary>
    private static readonly Regex Cobol85Citation = new(@"§(1985|1989):", RegexOptions.Compiled);

    /// <summary>A 1985 rule in the edition form: the locator after <c>§1985:</c> (or the 1989 amendment's <c>§1989:</c>).</summary>
    private static readonly Regex EditionForm1985 = new(@"^§(1985|1989):\S", RegexOptions.Compiled);

    /// <summary>The descriptors whose citation names a later edition and that carry neither their 1985 rule nor the reason 1985
    /// has none (kb/Work PB5993 stays open while this is not empty).</summary>
    private static readonly HashSet<string> Unmoved = new(StringComparer.Ordinal)
    {
        "all-literal-class", "all-subscript-not-repeatable-argument", "alphabet-code-set-branch", "alphabet-name-not-declared",
        "area-a-item-outside-area-a", "arithmetic-resultant-category", "assign-target-list-not-allowed",
        "call-argument-conformance", "call-argument-count", "call-argument-mode", "call-as-nested-context",
        "call-as-nested-scope", "call-by-reference-operand-kind", "call-content-operand-format", "call-target-category",
        "cancel-target-category", "char-function-argument-not-integer", "character-operand-class-mix",
        "choice-alternative-repeated", "clause-operand-not-a-data-name", "clause-operand-subscript-not-literal",
        "clause-operand-subscript-out-of-range", "close-phrase-organization", "code-set-clause", "communication-entry-rule",
        "communication-statement-cd-name", "condition-name-reference-modified", "condition-name-variable-excluded",
        "configuration-section-in-contained-program", "copy-literal-form", "copy-replacing-nested-copy",
        "copy-statement-placement", "currency-sign-clause-form", "currency-sign-literal-rule", "currency-symbol-rule",
        "data-item-beyond-size-limit", "decimal-point-form-invalid", "digit-capacity-over-18-pre-2002", "digit-capacity-over-31",
        "digit-capacity-under-1", "duplicate-externalized-definition", "edition-obsolete-flag", "edition-reserved-word",
        "empty-parentheses-on-data-reference", "end-marker-rule", "evaluate-object-negation-not-permitted",
        "evaluate-operand-combination-invalid", "evaluate-range-operand", "evaluate-selection-object-count",
        "expression-formation-pair", "external-file-item-not-external", "external-file-status-consistency",
        "external-record-not-cell-backed", "figurative-spelling-not-the-keyword", "file-control-clause-required",
        "file-description-clause-format", "file-description-without-select", "file-name-selected-twice", "file-status-item-rule",
        "format-element-repeated", "format-required-phrase-omitted", "from-phrase-function-class",
        "from-phrase-zero-length-literal", "go-to-format-shape", "group-value-subject-shape", "implicit-record-area-shape",
        "initialize-replacing-set-category-mismatch", "inspect-operand-rule", "integer-operand-beyond-limit",
        "integer-operand-zero", "into-phrase-receiver-not-admissible", "intrinsic-argument-class",
        "intrinsic-argument-data-item-rule", "intrinsic-argument-not-a-value", "intrinsic-function-form-invalid",
        "intrinsic-function-unknown", "lexer-text-unrecognized", "linage-clause-operand-rule", "linage-counter-without-linage",
        "literal-content-repertoire", "literal-continuation-part-empty", "literal-continued-two-forms", "literal-too-long",
        "literal-unterminated", "merge-in-sort-merge-proc", "messaging-message-area", "messaging-operand-class",
        "move-corresponding-position", "move-operand-class-not-allowed", "move-shape-invalid", "not-a-subscript",
        "numval-c-ambiguous-currency", "occurs-depending-attribute-mismatch", "occurs-format-not-printed",
        "operand-class-excluded", "paragraph-interior-period", "paragraph-period-missing", "perform-format3-loop-control-phrase",
        "picture-trailing-symbol-not-last", "procedure-division-format-mixed", "procedure-header-parameter",
        "program-attribute-containment", "program-text-in-area-a", "program-text-past-margin-r", "record-clause-description-size",
        "record-delimiter-unsupported", "record-depending-operand", "record-less-fd-no-record-size",
        "ref-mod-identifier-not-permitted", "ref-mod-of-ref-mod", "reference-to-refused-declaration",
        "refmod-literal-out-of-range", "refmod-literal-out-of-range-checked", "relation-operands-all-literal",
        "renames-area-not-whole-bytes", "renames-range-content", "replacing-operand-content", "replacing-result-forbidden",
        "report-column-alignment-not-absolute", "report-column-clause-rule", "report-column-left-of-line",
        "report-control-operand-index", "report-control-operand-unresolved", "report-counter-qualification",
        "report-generate-not-detail", "report-group-before-01", "report-group-indicate-placement",
        "report-group-reference-ambiguous", "report-item-reference", "report-next-group-clause-rule",
        "report-occurs-format3-rule", "report-page-clause-rule", "report-source-operand-parens", "report-source-operand-rule",
        "report-sum-addend-unresolved", "required-imperative-missing", "return-in-global-declarative", "sd-file-io-statement",
        "sd-record-io-statement", "search-all-table-no-key-phrase", "search-all-when-key-operand",
        "search-all-when-sending-operand", "search-identifier-1-operand", "select-without-file-description",
        "send-phrases-of-two-formats", "set-literal-amount-out-of-range", "set-no-format-admits-receiver", "sign-clause-subject",
        "sort-key-beyond-minimum-record", "source-unit-format", "special-name-declared-twice", "special-names-literal-class",
        "special-names-literal-empty", "string-unstring-operand-rule", "subscript-count-mismatch", "termination-status-operand",
        "text-manipulation-statement-syntax", "token-cut-at-margin-r", "unavailable-implementor-name",
        "unterminated-embedded-block", "usage-keyword-unmapped-internal", "user-word-type-conflict", "value-edited-literal-form",
        "value-format-1-single-literal", "value-format-requires-level-88", "value-literal-class-numeric-edited",
        "value-literal-oversize", "value-numeric-edited-oversize", "value-on-non-literal-usage", "value-operand-not-a-literal",
        "when-other-out-of-position", "word-character-not-permitted", "word-length-exceeded", "write-before-after-advancing-page",
        "written-kind-not-admitted",
    };

    private static bool CitesOnlyALaterEdition(DiagnosticDescriptor d) =>
        LaterEditionCitation.IsMatch(d.IsoSection) && !Cobol85Citation.IsMatch(d.IsoSection);

    [Fact]
    public void EveryLaterEditionDescriptor_CarriesIts1985Rule_OrWhy1985HasNone()
    {
        var missing = DiagnosticCatalog.All
            .Where(d => CitesOnlyALaterEdition(d) && d.Rule1985 is null && d.No1985Rule is null)
            .Select(d => d.Id)
            .ToHashSet(StringComparer.Ordinal);
        var added = missing.Except(Unmoved).Order(StringComparer.Ordinal).ToList();
        var moved = Unmoved.Except(missing).Order(StringComparer.Ordinal).ToList();
        Assert.True(added.Count == 0 && moved.Count == 0,
            $"{missing.Count} descriptors cite only a later edition's rule and say nothing of 1985 (expected {Unmoved.Count}). "
            + "Give each new one its Rule1985 (the 1985 rule its site cites at --std 85 through EditionInfo.Cite, checked with "
            + "scripts/spec/cite.py --check) or its No1985Rule (why the 1985 text has none), and take each moved one off Unmoved:\n"
            + (added.Count > 0 ? "  new: " + string.Join(", ", added) + "\n" : "")
            + (moved.Count > 0 ? "  moved (remove from Unmoved): " + string.Join(", ", moved) : ""));
    }

    [Fact]
    public void ADescriptor_CarriesAtMostOneOf1985RuleAndNo1985Rule_AndItsRuleIsA1985Citation()
    {
        var both = DiagnosticCatalog.All.Where(d => d.Rule1985 is not null && d.No1985Rule is not null).Select(d => d.Id).ToList();
        Assert.True(both.Count == 0, "a descriptor carries both its 1985 rule and the reason 1985 has none: " + string.Join(", ", both));
        var malformed = DiagnosticCatalog.All
            .Where(d => d.Rule1985 is { } r && !EditionForm1985.IsMatch(r))
            .Select(d => $"{d.Id}: {d.Rule1985}")
            .ToList();
        Assert.True(malformed.Count == 0,
            "Rule1985 is a 1985 rule in the edition form \"§1985:<locator>[ <rule>]\" (kb/Work PB4887):\n  " + string.Join("\n  ", malformed));
    }
}
