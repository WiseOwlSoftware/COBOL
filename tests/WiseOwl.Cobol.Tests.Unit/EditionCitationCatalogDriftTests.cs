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
        "accept-variable-length-group", "access-mode-not-sequential-on-sequential-file", "address-identifier-receiving",
        "address-of-bit-alignment", "address-of-object-data", "address-of-operand-kind", "algebraic-float-edited-range",
        "aligned-clause-subject", "all-literal-class", "all-literal-zero-length", "all-subscript-not-repeatable-argument",
        "alphabet-clause", "alphabet-code-name", "alphabet-code-set-branch", "alphabet-name-not-declared",
        "alphabet-through-with-also", "ambiguous-procedure-name", "any-length-call-argument", "any-length-other-clauses",
        "any-length-picture-form", "any-length-placement", "any-length-redefined-item", "any-length-subject-not-in-header",
        "apply-commit-clause-rule", "apply-commit-subject-rule", "area-a-item-outside-area-a", "arithmetic-format-operand",
        "arithmetic-operand-not-numeric", "arithmetic-resultant-category", "arithmetic-standard-binary-unsupported",
        "assign-literal-shape", "assign-target-list-not-allowed", "assign-using-in-own-record", "assign-using-not-alphanumeric",
        "base-convert-argument-rule", "based-record-substrate", "based-subject-of-class-object", "based-subject-variable-length",
        "boolean-condition-name-through", "boolean-relation-form", "boolean-shift-count-not-integer", "by-value-argument-class",
        "by-value-formal-class", "call-argument-conformance", "call-argument-count", "call-argument-mode",
        "call-as-nested-context", "call-as-nested-scope", "call-bit-alignment", "call-by-reference-object-data",
        "call-by-reference-operand-kind", "call-by-value-literal-kind", "call-by-value-operand-class",
        "call-content-operand-format", "call-omitted-needs-optional", "call-operand-section",
        "call-restricted-program-pointer-signature", "call-returning-conformance", "call-target-category",
        "call-variable-length-group", "cancel-target-category", "capacity-register-name-duplicated",
        "capacity-register-qualifier", "capacity-register-receiving", "capacity-register-subscripted",
        "capacity-register-under-table", "char-function-argument-not-integer", "character-operand-class-mix",
        "character-operand-usage", "choice-alternative-repeated", "class-clause", "class-condition-alphabetic-category",
        "class-condition-boolean-category", "class-condition-not-numeric-category", "class-condition-not-standard-float",
        "class-condition-numeric-operand-usage", "class-condition-operand-class", "class-condition-subject-not-identifier",
        "clause-elementary-only", "clause-operand-not-a-data-name", "clause-operand-subscript-not-literal",
        "clause-operand-subscript-out-of-range", "close-phrase-organization", "closed-format-unrecognized-clause",
        "closed-format-unrecognized-paragraph", "cobol-words-directive-invalid", "code-set-clause",
        "collating-sequence-alphabet-kind", "commit-rollback-context", "common-program-not-contained", "communication-entry-rule",
        "communication-statement-cd-name", "communication-statement-operand", "composed-based-placement", "concat-all-figurative",
        "concat-class-mismatch", "concat-operand-not-literal", "concat-result-too-long",
        "concat-symbolic-character-in-special-names", "condition-name-no-conditional-variable",
        "condition-name-reference-modified", "condition-name-variable-excluded", "conditional-statement-as-imperative",
        "configuration-section-in-contained-program", "constant-as-receiver", "constant-entry-rule", "constant-record-rule",
        "contained-report-file-not-global", "convert-function-rule", "copy-literal-form", "copy-replacing-nested-copy",
        "copy-statement-placement", "currency-sign-clause-form", "currency-sign-literal-rule", "currency-symbol-class-usage",
        "currency-symbol-rule", "data-clause-placement", "data-clause-unrecognized", "data-item-beyond-size-limit",
        "date-time-format-kind", "debug-all-operand-is-procedure", "debugging-line-between-literal-parts",
        "decimal-point-form-invalid", "declarative-referenced-from-outside", "declarative-references-nondeclarative",
        "declarative-use-rule", "define-directive-malformed", "define-no-override-redefinition",
        "delete-record-on-sequential-file", "digit-capacity-over-18-pre-2002", "digit-capacity-over-31", "digit-capacity-under-1",
        "directing-statement-synonym-before-syntax-error", "directive-between-clauses-placement", "directive-continued",
        "directive-expression-violation", "directive-in-exception-checking-perform", "directive-malformed-operand",
        "directive-placement-violation", "directive-structure-violation", "directive-within-text-manipulation-statement",
        "directive-word-as-compilation-variable-name", "display-directive-upon", "duplicate-contained-program-name",
        "duplicate-externalized-definition", "dyn-length-limit-above-implementor-maximum", "dynamic-capacity-bounds-invalid",
        "dynamic-length-other-clauses", "dynamic-length-picture-form", "dynamic-length-structure-invalid",
        "dynamic-length-structure-reference", "ec-name-introduced-later", "ec-name-unknown",
        "ec-name-wider-than-exception-status", "ec-user-not-specified-in-raising", "edition-obsolete-flag",
        "edition-reserved-word", "empty-parentheses-on-data-reference", "end-marker-rule", "entry-convention-violation",
        "evaluate-object-negation-not-permitted", "evaluate-operand-combination-invalid", "evaluate-range-operand",
        "evaluate-selection-object-count", "exception-file-argument-not-file", "exception-object-receiving",
        "exception-perform-close-multi-file", "exception-perform-delete-file-multi", "exception-perform-exception-name-repeated",
        "exception-perform-file-name-repeated", "exception-perform-file-pairing-not-io", "exception-perform-file-sort",
        "exception-perform-finally-transfer-out", "exception-perform-go-to-in-when", "exception-perform-initialize-multi",
        "exception-perform-initiate-multi-report", "exception-perform-merge", "exception-perform-open-multi",
        "exception-perform-raise-outside-imp1", "exception-perform-terminate-multi-report",
        "exception-perform-validate-multi-identifier", "exception-perform-when-required", "exit-not-alone",
        "exit-perform-cycle-in-exception-perform", "expression-formation-pair", "external-clause-as-literal",
        "external-file-item-not-external", "external-file-status-consistency", "external-pointer-or-object-item",
        "external-record-not-cell-backed", "external-relative-key-consistency", "external-type-rule",
        "externalized-name-all-spaces", "externalized-name-contained", "externalized-name-literal",
        "externalized-name-not-unique", "externalized-name-spaces", "figurative-spelling-not-the-keyword",
        "file-clause-size-range", "file-collating-alphabet", "file-collating-national-unsupported",
        "file-control-clause-required", "file-control-format-3-on-keyed-file", "file-description-clause-format",
        "file-description-without-select", "file-key-clause-rule", "file-name-operand-shape", "file-name-selected-twice",
        "file-status-item-rule", "final-method-overridden", "fixed-indicator-invalid", "flag-02-incompatibility",
        "flag-14-incompatibility", "flag-85-incompatibility", "flag-directive-malformed", "flag-native-arithmetic-use",
        "float-decimal-clause-unsupported", "float-usage-unsupported", "floating-continuation-comment",
        "floating-continuation-on-fixed-continuation", "floating-debugging-on-fixed-indicator", "floating-point-literal",
        "format-clause-violation", "format-element-out-of-order", "format-element-repeated", "format-required-phrase-omitted",
        "from-phrase-function-class", "from-phrase-zero-length-literal", "function-address-operand",
        "function-argument-conformance", "function-bit-alignment", "function-formal-receiving", "function-id-duplicated",
        "function-identifier-receiving", "function-keyword-or-argument-list-form", "function-omitted-needs-optional",
        "function-pointer-parentheses-required", "function-word-names-another-function", "global-in-oo-definition",
        "go-to-depending-selector-class", "go-to-format-shape", "go-to-not-last", "group-usage-rule", "group-value-subject-shape",
        "group-value-subordinate", "hex-literal-digit-grouping", "implicit-record-area-shape", "index-name-context",
        "index-name-not-in-table", "inherited-class-not-in-scope", "inherits-clause-rule", "initialize-category-repeated",
        "initialize-renames-item", "initialize-replacing-move-invalid", "initialize-replacing-pointer-needs-identifier",
        "initialize-replacing-set-category-mismatch", "initialize-target-class", "initialize-value-choice-missing",
        "inline-invocation-empty-arguments", "inline-invocation-no-returning", "inline-invocation-receiver",
        "inline-invocation-returning-shape", "inspect-operand-rule", "integer-operand-beyond-limit", "integer-operand-zero",
        "interface-inherited-prototypes-conflict", "interface-name-not-in-scope", "interface-not-implemented",
        "interface-prototype-redeclares-inherited", "into-phrase-receiver-not-admissible",
        "into-phrase-strong-receiver-record-areas", "intrinsic-argument-class", "intrinsic-argument-data-item-rule",
        "intrinsic-argument-not-a-value", "intrinsic-function-form-invalid", "intrinsic-function-unknown",
        "intrinsic-removed-by-cobol-words", "invoke-active-class-subordinate", "invoke-argument-not-conforming",
        "invoke-argument-zero-length-literal", "invoke-base-class-method-rule", "invoke-bit-alignment",
        "invoke-by-value-literal-not-numeric", "invoke-by-value-operand-class", "invoke-method-not-defined",
        "invoke-object-data-by-reference", "invoke-omitted-needs-optional", "invoke-operand-section",
        "invoke-receiver-not-object-reference", "invoke-universal-reference-rule", "io-phrase-forbidden-here",
        "io-statement-operand-rule", "level-number-entry-format", "level-number-hierarchy", "level-number-out-of-range",
        "lexer-text-unrecognized", "linage-clause-operand-rule", "linage-counter-outside-procedure-division",
        "linage-counter-receiving", "linage-counter-without-linage", "line-sequential-block-record-contains",
        "linkage-item-not-referenceable", "literal-content-repertoire", "literal-continuation-part-empty",
        "literal-continuation-quote", "literal-continued-two-forms", "literal-too-long", "literal-unterminated",
        "locale-alphabet-not-a-charset", "locale-name-undeclared", "lock-multiple-on-sequential-file",
        "lock-phrase-under-automatic-locking", "merge-in-sort-merge-proc", "messaging-message-area", "messaging-operand-class",
        "method-prototype-override-or-final", "method-redefines-scope", "mnemonic-name-not-device",
        "module-name-nested-outside-nested-program", "move-corresponding-position", "move-operand-class-not-allowed",
        "move-shape-invalid", "move-variable-length-incompatible", "multiple-character-token-split",
        "multiple-inheritance-declined", "national-conversion-argument-rule", "national-data",
        "national-literal-fixed-continuation", "new-without-base", "nonstandard-extension-used", "not-a-subscript",
        "null-receiving", "numeric-literal-decimal-separator", "numval-c-ambiguous-currency", "object-property-reference-rule",
        "object-reference-active-class-placement", "object-reference-assignment-not-conforming",
        "object-reference-interface-phrase", "object-relation-shape", "object-view-interface-phrase", "object-view-receiving",
        "occurs-depending-attribute-mismatch", "occurs-depending-bounds-invalid", "occurs-depending-object-in-table-range",
        "occurs-depending-object-not-integer", "occurs-depending-object-subscripted", "occurs-depending-object-undefined",
        "occurs-depending-table-in-redefines-area", "occurs-depending-table-nested", "occurs-depending-table-not-last",
        "occurs-dynamic-bound-above-maximum", "occurs-format-not-printed", "occurs-key-not-within-table",
        "omitted-condition-operand", "omitted-intrinsic-argument", "oo-data-division-placement", "oo-definition-name-not-unique",
        "oo-environment-placement", "open-extend-access-linage", "open-norewind-open-mode", "open-norewind-organization",
        "open-report-file-mode", "open-reversed-organization", "open-sharing-all-requires-lock-mode", "operand-class-excluded",
        "operand-is-not-a-condition", "options-initialize-fill-literal", "order-table-repeated", "order-table-unresolved",
        "override-signature-mismatch", "override-without-inherited-method", "paragraph-interior-period",
        "paragraph-name-duplicated-in-section", "paragraph-period-missing", "parameterized-definition-using",
        "parametric-polymorphism-declined", "perform-format3-loop-control-phrase", "perform-inline-head-multiple-control-phrases",
        "perform-range-declaratives", "perform-times-count-not-integer", "perform-varying-operand-rule",
        "perform-varying-operand-shape", "perform-when-file-unknown", "picture-at-elementary-level", "picture-composition",
        "picture-editing-char1-not-a-literal", "picture-editing-character-absent", "picture-editing-character-invalid",
        "picture-editing-character-repeated", "picture-editing-extended-count", "picture-editing-for-symbols",
        "picture-editing-literal-class", "picture-editing-literal-too-long", "picture-editing-phrase-order",
        "picture-editing-sign-literals-differ", "picture-float-edited", "picture-item-too-large", "picture-locale-format2",
        "picture-precedence", "picture-repetition-factor", "picture-string-invalid", "picture-string-too-long",
        "picture-trailing-symbol-not-last", "picture-with-fixed-width-binary-usage", "picture-with-floating-point-usage",
        "picture-with-object-reference-usage", "pointer-operand-shape", "pop-directive-unsuccessful", "predefined-null-context",
        "predefined-object-reference-placement", "procedure-division-format-mixed", "procedure-header-item-not-linkage-root",
        "procedure-header-parameter", "program-address-operand", "program-attribute-containment",
        "program-initial-recursive-exclusive", "program-text-in-area-a", "program-text-past-margin-r",
        "property-accessor-description-mismatch", "property-clause-unnamed-subject", "property-method-rule", "property-object",
        "property-with-based-or-typedef", "prototype-body", "prototype-definition-signature-mismatch", "prototype-order",
        "prototype-pointer-restriction", "prototype-pointer-signature", "pseudo-text-delimiter-placement",
        "qualified-procedure-header", "raise-checking-not-enabled", "raise-operand-rule", "raised-exception-name-not-level-3",
        "raising-last-out-of-place", "raising-object-operand-rule", "raising-phrase-name-invalid",
        "range-alphabet-class-mismatch", "read-key-on-non-indexed-file", "record-clause-description-size",
        "record-delimiter-unsupported", "record-depending-operand", "record-format-layout-unsupported",
        "record-less-fd-no-record-size", "record-less-fd-read-without-into", "redefines-data-name-2-qualified",
        "redefines-entry-rule", "redefines-of-redefinition", "redefines-pointer-object", "redefines-storage-larger-than-target",
        "redefines-subject-is-dynamic-table", "redefines-target-occurs", "redefines-target-unresolved",
        "redefines-variable-length", "redefinition-without-override", "ref-mod-function-result-class",
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
        "report-sum-data-name-rule", "report-sum-entry-category", "report-sum-rounded-without-column",
        "report-sum-upon-not-detail", "report-suppress-context", "report-usage-not-display-or-national",
        "repository-assembly-namespace-collision", "repository-description-unbuildable", "repository-entry-malformed",
        "repository-entry-shadowed-differs", "repository-entry-unreadable", "repository-entry-version-skew",
        "repository-entry-without-signatures", "repository-identity-unanswered", "repository-intrinsic-name-as-user-word",
        "repository-location-unavailable", "repository-previous-build-skew", "repository-program-specifier",
        "repository-signature-differs", "required-imperative-missing", "reserve-areas-beyond-limit",
        "reserve-on-line-sequential-file", "resume-at-declarative-procedure", "resume-in-global-declarative",
        "resume-in-when-not-next-statement", "resume-outside-declarative-or-when", "return-in-global-declarative",
        "same-as-cycle", "same-as-entry-rule", "same-as-referenced-entry", "same-clause-rule", "screen-statement-unsupported",
        "sd-file-io-statement", "sd-record-io-statement", "search-all-table-no-key-phrase", "search-all-when-key-operand",
        "search-all-when-sending-operand", "search-end-search-next-sentence", "search-identifier-1-operand",
        "search-varying-operand", "select-when-clause-violation", "select-without-file-description", "self-super-receiving",
        "send-phrases-of-two-formats", "separator-comma-without-space", "separator-literal-delimiter",
        "separator-operator-spacing", "separator-period-without-space", "set-content-not-numeric",
        "set-content-not-standard-float", "set-content-sign-required", "set-index-assignment-operand",
        "set-literal-amount-out-of-range", "set-locale-categories", "set-locale-pointer-category",
        "set-locale-user-default-source", "set-no-format-admits-receiver", "set-size-target-not-dynamic-length",
        "sign-clause-subject", "sign-clause-with-locale-picture", "signed-literal-sign-not-adjacent",
        "sort-key-beyond-minimum-record", "sort-merge-placement", "source-text-read-as-latin1", "source-unit-format",
        "special-name-declared-twice", "special-names-literal-class", "special-names-literal-empty", "standard-compare-argument",
        "statement-format-shape", "statement-operand-rule", "stop-not-last", "string-unstring-operand-rule",
        "strong-class-condition", "strong-compare-mismatch", "strong-compare-ordering", "strong-group-comparison-member",
        "strong-move-mismatch", "strong-type-level-rule", "strong-type-redefined", "strong-type-renamed",
        "subscript-count-mismatch", "subscript-on-non-table-item", "suffix-before-qualifier", "super-position",
        "super-qualifier-not-inherited", "symbolic-characters-clause", "table-element-not-subscripted", "table-nesting-too-deep",
        "table-sort-subject-rightmost-all", "table-value-from-subscript-out-of-range", "table-value-to-subscript-invalid",
        "termination-status-operand", "text-manipulation-statement-syntax", "token-cut-at-margin-r", "turn-directive-malformed",
        "turn-file-name-non-io", "type-declaration-self-reference", "type-declaration-shape",
        "type-entry-followed-by-subordinate", "type-entry-rule", "type-level-77-not-elementary", "type-name-undefined",
        "type-object-reference-file-section", "type-superordinate-usage-clause", "unavailable-implementor-name",
        "undefined-reference", "unterminated-embedded-block", "usage-clause-compatibility", "usage-constant-record",
        "usage-encoding-phrase-scope", "usage-endianness-phrase-scope", "usage-float-format-phrase-repeated",
        "usage-group-contradiction", "usage-keyword-unmapped-internal", "usage-object-reference-file-section",
        "use-before-reporting-restriction", "use-exception-file-pair-repeated", "use-exception-object-name",
        "use-file-exception-name-not-io", "use-statement-placement", "user-function-argument-rule", "user-word-type-conflict",
        "user-word-without-letter", "validate-data-division-clause-unsupported", "validate-operand-class",
        "validate-operand-description", "validate-operand-form", "validate-operand-unbuilt", "value-boolean-literal-rule",
        "value-clause-placement", "value-connective-not-printed", "value-edited-literal-form", "value-format-1-single-literal",
        "value-format-requires-level-88", "value-literal-class", "value-literal-class-numeric-edited", "value-literal-oversize",
        "value-numeric-edited-oversize", "value-numeric-out-of-range", "value-on-non-literal-usage",
        "value-operand-not-a-literal", "value-through-not-ascending", "variable-length-group-comparison",
        "when-other-out-of-position", "word-character-not-permitted", "word-length-exceeded",
        "word-withdrawn-before-syntax-error", "write-advancing-mnemonic-not-feature", "write-advancing-operand",
        "write-advancing-page-with-end-of-page", "write-before-after-advancing-page", "write-end-of-page-without-linage",
        "write-rewrite-file-bit-alignment", "write-rewrite-file-operand", "write-rewrite-file-requires-from",
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
