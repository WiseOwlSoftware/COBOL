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
        "abbreviated-relation-after-boolean-relation", "abbreviated-relation-without-subject", "accept-receiver-excluded",
        "accept-variable-length-group", "access-mode-not-sequential-on-sequential-file", "all-literal-class",
        "all-literal-zero-length", "all-subscript-not-repeatable-argument", "alphabet-clause", "alphabet-code-name",
        "alphabet-code-set-branch", "alphabet-name-not-declared", "alphabet-through-with-also", "ambiguous-procedure-name",
        "area-a-item-outside-area-a", "arithmetic-format-operand", "arithmetic-operand-not-numeric",
        "arithmetic-resultant-category", "assign-literal-shape", "assign-target-list-not-allowed", "call-argument-conformance",
        "call-argument-count", "call-argument-mode", "call-as-nested-context", "call-as-nested-scope",
        "call-by-reference-operand-kind", "call-content-operand-format", "call-operand-section", "call-target-category",
        "call-variable-length-group", "cancel-target-category", "char-function-argument-not-integer",
        "character-operand-class-mix", "character-operand-usage", "choice-alternative-repeated", "class-clause",
        "class-condition-alphabetic-category", "class-condition-not-numeric-category", "class-condition-numeric-operand-usage",
        "class-condition-operand-class", "class-condition-subject-not-identifier", "clause-elementary-only",
        "clause-operand-not-a-data-name", "clause-operand-subscript-not-literal", "clause-operand-subscript-out-of-range",
        "close-phrase-organization", "closed-format-unrecognized-clause", "closed-format-unrecognized-paragraph",
        "code-set-clause", "collating-sequence-alphabet-kind", "common-program-not-contained", "communication-entry-rule",
        "communication-statement-cd-name", "communication-statement-operand", "condition-name-no-conditional-variable",
        "condition-name-reference-modified", "condition-name-variable-excluded", "conditional-statement-as-imperative",
        "configuration-section-in-contained-program", "contained-report-file-not-global", "copy-literal-form",
        "copy-replacing-nested-copy", "copy-statement-placement", "currency-sign-clause-form", "currency-sign-literal-rule",
        "currency-symbol-class-usage", "currency-symbol-rule", "data-clause-placement", "data-clause-unrecognized",
        "data-item-beyond-size-limit", "debug-all-operand-is-procedure", "debugging-line-between-literal-parts",
        "decimal-point-form-invalid", "declarative-referenced-from-outside", "declarative-references-nondeclarative",
        "declarative-use-rule", "delete-record-on-sequential-file", "digit-capacity-over-18-pre-2002", "digit-capacity-over-31",
        "digit-capacity-under-1", "duplicate-contained-program-name", "duplicate-externalized-definition",
        "edition-obsolete-flag", "edition-reserved-word", "empty-parentheses-on-data-reference", "end-marker-rule",
        "evaluate-object-negation-not-permitted", "evaluate-operand-combination-invalid", "evaluate-range-operand",
        "evaluate-selection-object-count", "exit-not-alone", "expression-formation-pair", "external-file-item-not-external",
        "external-file-status-consistency", "external-record-not-cell-backed", "external-relative-key-consistency",
        "figurative-spelling-not-the-keyword", "file-clause-size-range", "file-control-clause-required",
        "file-control-format-3-on-keyed-file", "file-description-clause-format", "file-description-without-select",
        "file-key-clause-rule", "file-name-operand-shape", "file-name-selected-twice", "file-status-item-rule",
        "fixed-indicator-invalid", "format-element-out-of-order", "format-element-repeated", "format-required-phrase-omitted",
        "from-phrase-function-class", "from-phrase-zero-length-literal", "function-identifier-receiving",
        "function-keyword-or-argument-list-form", "go-to-depending-selector-class", "go-to-format-shape", "go-to-not-last",
        "group-value-subject-shape", "group-value-subordinate", "implicit-record-area-shape", "index-name-context",
        "index-name-not-in-table", "initialize-category-repeated", "initialize-renames-item", "initialize-replacing-move-invalid",
        "initialize-replacing-set-category-mismatch", "initialize-target-class", "inspect-operand-rule",
        "integer-operand-beyond-limit", "integer-operand-zero", "into-phrase-receiver-not-admissible", "intrinsic-argument-class",
        "intrinsic-argument-data-item-rule", "intrinsic-argument-not-a-value", "intrinsic-function-form-invalid",
        "intrinsic-function-unknown", "io-phrase-forbidden-here", "io-statement-operand-rule", "level-number-entry-format",
        "level-number-hierarchy", "level-number-out-of-range", "lexer-text-unrecognized", "linage-clause-operand-rule",
        "linage-counter-outside-procedure-division", "linage-counter-receiving", "linage-counter-without-linage",
        "linkage-item-not-referenceable", "literal-content-repertoire", "literal-continuation-part-empty",
        "literal-continuation-quote", "literal-continued-two-forms", "literal-too-long", "literal-unterminated",
        "merge-in-sort-merge-proc", "messaging-message-area", "messaging-operand-class", "mnemonic-name-not-device",
        "move-corresponding-position", "move-operand-class-not-allowed", "move-shape-invalid",
        "move-variable-length-incompatible", "multiple-character-token-split", "nonstandard-extension-used", "not-a-subscript",
        "numeric-literal-decimal-separator", "numval-c-ambiguous-currency", "occurs-depending-attribute-mismatch",
        "occurs-depending-bounds-invalid", "occurs-depending-object-in-table-range", "occurs-depending-object-not-integer",
        "occurs-depending-object-subscripted", "occurs-depending-object-undefined", "occurs-depending-table-in-redefines-area",
        "occurs-depending-table-nested", "occurs-depending-table-not-last", "occurs-format-not-printed",
        "occurs-key-not-within-table", "open-extend-access-linage", "open-norewind-open-mode", "open-norewind-organization",
        "open-report-file-mode", "open-reversed-organization", "operand-class-excluded", "operand-is-not-a-condition",
        "order-table-repeated", "paragraph-interior-period", "paragraph-name-duplicated-in-section", "paragraph-period-missing",
        "perform-format3-loop-control-phrase", "perform-inline-head-multiple-control-phrases", "perform-range-declaratives",
        "perform-times-count-not-integer", "perform-varying-operand-rule", "perform-varying-operand-shape",
        "picture-at-elementary-level", "picture-composition", "picture-item-too-large", "picture-precedence",
        "picture-repetition-factor", "picture-string-invalid", "picture-string-too-long", "picture-trailing-symbol-not-last",
        "procedure-division-format-mixed", "procedure-header-item-not-linkage-root", "procedure-header-parameter",
        "program-attribute-containment", "program-text-in-area-a", "program-text-past-margin-r",
        "pseudo-text-delimiter-placement", "qualified-procedure-header", "read-key-on-non-indexed-file",
        "record-clause-description-size", "record-delimiter-unsupported", "record-depending-operand",
        "record-less-fd-no-record-size", "record-less-fd-read-without-into", "redefines-data-name-2-qualified",
        "redefines-entry-rule", "redefines-of-redefinition", "redefines-storage-larger-than-target", "redefines-target-occurs",
        "redefines-target-unresolved", "redefines-variable-length", "ref-mod-function-result-class",
        "ref-mod-identifier-not-permitted", "ref-mod-leftmost-position-omitted", "ref-mod-of-ref-mod",
        "reference-to-refused-declaration", "refmod-literal-out-of-range", "refmod-literal-out-of-range-checked",
        "relation-index-pair", "relation-numeric-not-integer", "relation-operands-all-literal", "renames-area-not-whole-bytes",
        "renames-entry-rule", "renames-operand-unresolved", "renames-range-content", "replace-operand-not-pseudo-text",
        "replacing-operand-content", "replacing-result-forbidden", "report-clause-name-rule", "report-code-clause-rule",
        "report-column-alignment-not-absolute", "report-column-clause-rule", "report-column-left-of-line",
        "report-control-final-placement", "report-control-operand-index", "report-control-operand-shape",
        "report-control-operand-unresolved", "report-control-type-operand", "report-counter-qualification",
        "report-description-without-group", "report-expression-operand-section", "report-file-record-entry",
        "report-file-reference", "report-generate-needs-control", "report-generate-not-detail", "report-group-before-01",
        "report-group-clause-rule", "report-group-indicate-placement", "report-group-reference-ambiguous",
        "report-group-set-rule", "report-item-reference", "report-line-clause-rule", "report-line-counter-receiving",
        "report-next-group-clause-rule", "report-occurs-format3-rule", "report-page-clause-rule",
        "report-page-phrase-without-limit", "report-page-type-requires-page", "report-reset-not-control-operand",
        "report-source-operand-parens", "report-source-operand-rule", "report-subscript-counter", "report-sum-addend-not-numeric",
        "report-sum-addend-unresolved", "report-sum-counter-reference", "report-sum-data-name-groups",
        "report-sum-data-name-rule", "report-sum-entry-category", "report-sum-upon-not-detail", "report-suppress-context",
        "report-usage-not-display-or-national", "required-imperative-missing", "reserve-areas-beyond-limit",
        "return-in-global-declarative", "same-clause-rule", "screen-statement-unsupported", "sd-file-io-statement",
        "sd-record-io-statement", "search-all-table-no-key-phrase", "search-all-when-key-operand",
        "search-all-when-sending-operand", "search-end-search-next-sentence", "search-identifier-1-operand",
        "search-varying-operand", "select-without-file-description", "send-phrases-of-two-formats",
        "separator-comma-without-space", "separator-literal-delimiter", "separator-operator-spacing",
        "separator-period-without-space", "set-content-not-numeric", "set-content-sign-required", "set-index-assignment-operand",
        "set-literal-amount-out-of-range", "set-no-format-admits-receiver", "sign-clause-subject",
        "signed-literal-sign-not-adjacent", "sort-key-beyond-minimum-record", "sort-merge-placement",
        "source-text-read-as-latin1", "source-unit-format", "special-name-declared-twice", "special-names-literal-class",
        "special-names-literal-empty", "statement-format-shape", "statement-operand-rule", "stop-not-last",
        "string-unstring-operand-rule", "subscript-count-mismatch", "subscript-on-non-table-item", "suffix-before-qualifier",
        "symbolic-characters-clause", "table-element-not-subscripted", "table-nesting-too-deep", "termination-status-operand",
        "text-manipulation-statement-syntax", "token-cut-at-margin-r", "unavailable-implementor-name", "undefined-reference",
        "unterminated-embedded-block", "usage-clause-compatibility", "usage-group-contradiction",
        "usage-keyword-unmapped-internal", "use-before-reporting-restriction", "use-statement-placement",
        "user-word-type-conflict", "user-word-without-letter", "value-clause-placement", "value-connective-not-printed",
        "value-edited-literal-form", "value-format-1-single-literal", "value-format-requires-level-88", "value-literal-class",
        "value-literal-class-numeric-edited", "value-literal-oversize", "value-numeric-edited-oversize",
        "value-numeric-out-of-range", "value-on-non-literal-usage", "value-operand-not-a-literal", "value-through-not-ascending",
        "variable-length-group-comparison", "when-other-out-of-position", "word-character-not-permitted", "word-length-exceeded",
        "write-advancing-mnemonic-not-feature", "write-advancing-operand", "write-advancing-page-with-end-of-page",
        "write-before-after-advancing-page", "write-end-of-page-without-linage", "written-kind-not-admitted",
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
