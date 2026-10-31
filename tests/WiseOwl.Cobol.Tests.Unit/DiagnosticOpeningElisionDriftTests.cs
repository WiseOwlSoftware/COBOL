// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE DRIFT TEST for EVERY DIAGNOSTIC THAT OPENS WITH AN ELIDED STATEMENT (kb/Work PB4160, the generalization of
/// PB388's and PB4121's SET-only rule).
/// <para>PB388 found thirteen SET messages that opened <c>SET … TO SELF</c>; PB4121 found three more; PB4160 found about
/// fifty in the other binders (<c>CALL … USING BY VALUE</c>, <c>READ … KEY IS X</c>, <c>OCCURS … DEPENDING ON</c>,
/// <c>CODE-SET … X</c>, <c>DIVIDE … BY without GIVING</c>), each written by a hand that elided a phrase it had in
/// hand. The console renders the U+2026 as <c>.</c>, so the programmer read <c>CALL . USING BY VALUE</c>, a statement
/// nobody wrote. The rule is the same for every verb, so the test is: <b>no message opens with an ellipsis</b>, in any
/// binder, any validation pass, any call form that reports one. A message names the phrase it is about as the
/// programmer wrote it (<c>CALL USING BY VALUE 3</c>, <c>OCCURS ASCENDING KEY IS 'K' on 'T'</c>), never a verb and
/// three dots.</para>
/// <para>A mid-sentence <c>…</c> is the general format's own notation (<c>"the receiving operand of SET … TO
/// ENTRY"</c>, a <c>where</c> label that a registry splices after a dash) and is deliberately untouched: the pattern
/// is head-anchored, which is what separates the two. The other half of this net is the conformance suite's refusal
/// test, which checks the RENDERED message of every negative golden (<see cref="ElidedMessageOpening.Rendered"/>) and
/// so sees a head that string-building assembled from a label.</para>
/// <para>Proven to FAIL before it was trusted: <see cref="ThePatternFiresOnEveryDefectShapeTheTreeCarried"/> feeds the
/// matcher the exact lines the tree carried and asserts each is caught, and feeds it the repaired twins and the
/// form references and asserts they are not (a gate that has never been seen to fail is not evidence).</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class DiagnosticOpeningElisionDriftTests
{
    [Fact]
    public void NoDiagnosticMessageOpensWithAnElidedStatement()
    {
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(TestRepo.Src(), file);
            string[] parts = rel.Split(Path.DirectorySeparatorChar);
            if (parts.Contains("bin") || parts.Contains("obj") || parts.Contains("Generated")) continue;

            // A line comment can quote a defect line (this tree's own history does); it is not a message.
            string text = string.Join('\n', File.ReadAllLines(file).Select(l => l.TrimStart().StartsWith("//", StringComparison.Ordinal) ? "" : l));
            foreach (System.Text.RegularExpressions.Match m in ElidedMessageOpening.Source.Matches(text))
            {
                int line = text.AsSpan(0, m.Index).Count('\n') + 1;
                offenders.Add($"{rel.Replace('\\', '/')}:{line}  {m.Value.Trim().Replace('\n', ' ')}");
            }
        }
        Assert.True(offenders.Count == 0,
            "A diagnostic message OPENS with a statement whose phrase it elided (`VERB … PHRASE`). The ellipsis stands "
            + "for operands the binder holds where it reports, and the console renders U+2026 as `.`, so the programmer "
            + "reads a statement nobody wrote. Name the phrase the message is about, as it was written (kb/Work PB388, "
            + "PB4121, PB4160):\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>A construct's <c>display</c> is the HEAD of every edition-gate message the registry words
    /// (<c>"{display} requires COBOL-2002 …"</c>, <c>"{display} was removed in …"</c>), so a row that spells
    /// <c>SET … TO object-reference</c> there opens a diagnostic with an elided statement through no call the source scan
    /// can see. The registry is data (tests/version-matrix/constructs.json), so it is asked as data.</summary>
    [Fact]
    public void NoConstructDisplayOpensWithAnElidedStatement()
    {
        var offenders = WiseOwl.Cobol.Editions.ConstructRegistry.Entries
            .Where(e => ElidedMessageOpening.Opening.IsMatch(e.Display))
            .Select(e => $"{e.Id}: {e.Display}")
            .ToList();
        Assert.True(offenders.Count == 0,
            "A construct display opens with an elided statement; the registry puts it at the head of the edition-gate "
            + "diagnostic (kb/Work PB4160). Spell the construct by its written words:\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>⛔ THE GATE FIRES. Each defect line is one the tree carried verbatim (or the call form it was reported
    /// through); each repaired twin and form reference must NOT match.</summary>
    [Fact]
    public void ThePatternFiresOnEveryDefectShapeTheTreeCarried()
    {
        string[] defects =
        [
            // PB388: the Error(code-descriptor, interpolated message) form
            "ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,\n                \"SET … TO SELF/SUPER: SELF and SUPER are object references\"",
            // PB4121: BoundRejected.Report(edition, descriptor, message)
            "return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, \"SET … TO ENTRY with no receiving operand\");",
            // PB4160: a string-literal code as the first argument, an interpolated message
            "Edition.Error(\"COBOL0851\", $\"OCCURS … DEPENDING ON '{writtenDep}' on '{subject}': data-name-1 \"",
            "data.Edition.Error(\"COBOL0864\", $\"READ … KEY on '{file.CobolName}': {refusal}\");",
            // PB4160: three-dot spelling, two-word verb phrase
            "ctx.Edition.Error(DiagnosticCatalog.PerformTimesCountNotInteger,\n                $\"PERFORM VARYING ... FROM count '{c}' \"",
            // PB4160: a {value} hole standing for the verb, and the EditionDiagnostic constructor (five leading arguments)
            "ctx.Edition.Error(DiagnosticCatalog.ArithmeticFormatOperand,\n                $\"{verb} … GIVING: Format 2 prints ONE sending operand\"",
            "Sink.Report(new EditionDiagnostic(DiagnosticCatalog.X.Code,\n            EditionSeverity.Error, DiagnosticCatalog.X.Id,\n            $\"{owner} … {phrase}: this {name} statement\"",
            // PB4160: InitializeErrorAction carries a message as its first argument
            "actions.Add(new InitializeErrorAction(\"INITIALIZE ... WITH FILLER of the implicit filler bits of \"",
            // R1-1 (the review of PB4160): the three EditionContext seams whose caller string IS the head of the message
            "ctx.Edition.Extension(ExtensionIds.SetProgramPointerToEntry, \"SET … TO ENTRY\");",
            "ctx.Edition.Removed(\"COBOL0902\", $\"CALL … AS NESTED argument {n}: \"",
            "ctx.Edition.Declined(DiagnosticCatalog.X, \"WRITE ... FILE of the record\");",
            // PB4212: the messages of the RUNNING program, as the tree carried them verbatim
            "string guard = LandAmount(s.Value, SetAmountRule.IndexTo, \"SET … TO\", out string tmp, \"set\");",
            "string guard = set.LandAmount(from, SetAmountRule.IndexTo, \"PERFORM VARYING … FROM\", out string tmp, \"pv\");",
            "throw new InvalidOperationException(\n                $\"OPEN … WITH NO REWIND reached a {c.Category} connector '{name}'\");",
            "throw MisroutedVerb(\"OPEN … REVERSED\", name, c);",
            "set.AugmentSetTarget(also, down: false, new NumX(\"1\", 0), \"SEARCH ... VARYING\");",
        ];
        foreach (string defect in defects)
            Assert.True(ElidedMessageOpening.Source.IsMatch(defect), $"the elided-opening pattern no longer matches: {defect}");

        string[] fine =
        [
            // the repaired twins
            "ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,\n                $\"SET {SetFormatSelection.Written(targetRefs)} TO {senderText}: SELF and SUPER \"",
            "return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, \"SET TO ENTRY with no receiving operand\");",
            "Edition.Error(\"COBOL0851\", $\"OCCURS DEPENDING ON '{writtenDep}' on '{subject}': data-name-1 \"",
            "data.Edition.Error(\"COBOL0864\", $\"READ KEY on '{file.CobolName}': {refusal}\");",
            // a general-format REFERENCE, mid-sentence: the standard's own ellipsis
            "ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,\n                    $\"SET '{drefs[i].GetText()}': the receiving operand of SET … TO ENTRY \"",
            // a message that does not open with the statement's words
            "ctx.Edition.Error(DiagnosticCatalog.X, \"the NESTED phrase … is not admitted\");",
            // the repaired twins of the three seams
            "ctx.Edition.Extension(ExtensionIds.SetProgramPointerToEntry, $\"SET {SetFormatSelection.Written(drefs)} TO ENTRY\");",
            "ctx.Edition.Removed(\"COBOL0902\", $\"CALL AS NESTED argument {n}: \"",
            "ctx.Edition.Declined(DiagnosticCatalog.X, \"WRITE FILE of the record\");",
            // a collection's own Removed( with no message string
            "list.Removed(item, 1);",
            // PB4212: the repaired twins, and a runtime message whose ellipsis is mid-sentence
            "string guard = LandAmount(s.Value, SetAmountRule.IndexTo, \"SET index-name TO\", out string tmp, \"set\");",
            "throw new InvalidOperationException(\n                $\"OPEN WITH NO REWIND reached a {c.Category} connector '{name}'\");",
            "throw MisroutedVerb(\"OPEN REVERSED\", name, c);",
            "throw new InvalidOperationException(\"the receiving operand of SET … TO ENTRY is missing\");",
        ];
        foreach (string ok in fine)
            Assert.False(ElidedMessageOpening.Source.IsMatch(ok), $"the elided-opening pattern matches a message it must leave alone: {ok}");

        // The rendered half, on the exact text the CLI printed for the defect this note was filed for.
        Assert.Matches(ElidedMessageOpening.Rendered, "p1.cob(4,12): error COBOL0899: CALL … USING NULL: a keyword-less literal");
        Assert.Matches(ElidedMessageOpening.Rendered, "p.cob(1,1): error COBOL0864: READ ... KEY on 'F': the KEY phrase");
        Assert.DoesNotMatch(ElidedMessageOpening.Rendered, "p.cob(1,1): error COBOL0864: READ KEY on 'F': the KEY phrase");
        Assert.DoesNotMatch(ElidedMessageOpening.Rendered, "p.cob(1,1): error COBOL0869: 'X' is the receiving operand of SET … TO ENTRY");
    }
}
