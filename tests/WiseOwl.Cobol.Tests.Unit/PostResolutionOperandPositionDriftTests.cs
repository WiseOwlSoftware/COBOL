// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A DIAGNOSTIC ABOUT AN OPERAND STANDS AT THE OPERAND, ALSO WHEN IT IS RAISED AFTER THE RESOLUTION (kb/Work PB4406,
/// the second half of PB4383). <c>ReferenceResolver.ResolveImpl</c> opens <c>edition.At(dref)</c> for what the
/// resolution reports; the screens that run after it (the index-name, class, receiving and resultant-category screens, and
/// a literal's refusal) report from <c>ExpressionBinder</c> members whose scope the resolver's has already closed, so
/// without their own <c>At(...)</c> they land on the statement's first token, a line above the operand of a continued
/// statement. Each member below is the ONE mapping of its operand kind to a bound node (or the one screen on it) and
/// must open the scope before it reports. A NEW member that reports an operand-level diagnostic after a resolution joins
/// this list with its position; the behaviour is pinned by
/// <c>DiagnosticPositionTests.PostResolutionScreen_NamesTheOperandsOwnLineAndColumn</c>.
/// </summary>
public sealed class PostResolutionOperandPositionDriftTests
{
    /// <summary>The signature fragment that finds each member in <c>ExpressionBinder.cs</c>, and the reason it must position.</summary>
    private static readonly (string Signature, string Why)[] Members =
    [
        ("public BoundOperand FieldOperand(Core.DataReferenceContext dref)", "the statement-slot mapping of a data reference: LINAGE-COUNTER and keyword-omitted-function reports"),
        ("private BoundExpr RefExpr(Core.DataReferenceContext dref, OperandContext context)", "the expression mapping of a data reference: COBOL1637, COBOL0844, LINAGE-COUNTER"),
        ("public Place? ResolveReceiving(Core.DataReferenceContext dref)", "the receiving chokepoint: the form and place bars and the deferred-shape error"),
        ("internal Place? RefuseNonReceivable(NonReceivableForm form,", "the refusal of a non-receivable identifier form"),
        ("internal Place? ScreenResultant(Place p,", "the arithmetic resultant's category screen, COBOL1675"),
        ("public BoundOperand? NonNumericLiteralOperand(Core.NonNumericLiteralContext? nn)", "a non-numeric literal's refusals"),
        ("public BoundOperand LiteralOperand(Core.LiteralContext lit)", "a numeric literal's digit-cap and exponent-range screens"),
        ("private BoundExpr NumLiteral(Core.LiteralContext lit,", "the arithmetic tier's twin of LiteralOperand"),
        ("private BoundExpr? NonNumericInNumericContext(", "a non-numeric literal in an arithmetic position, COBOL0844"),
        ("private BoundExpr NotNumericIdentifier(", "an object-view, SELF, SUPER, NULL or address in an arithmetic position, COBOL0844"),
        ("private BoundExpr BindPrimary(Core.PrimaryExpressionContext pe,", "the arithmetic primary: numeric literals and function-identifiers"),
    ];

    /// <summary>How far below its signature a member must open the scope: the opening brace, its comment block, then the statement.</summary>
    private const int LinesToOpen = 14;

    [Fact]
    public void EveryPostResolutionReporterOpensItsOperandsPosition()
    {
        string[] lines = File.ReadAllLines(Path.Combine(TestRepo.Src(), "WiseOwl.Cobol.Compiler", "Binding", "Procedure", "ExpressionBinder.cs"));
        var offenders = new List<string>();
        foreach (var (signature, why) in Members)
        {
            int at = Array.FindIndex(lines, l => l.Contains(signature, StringComparison.Ordinal));
            if (at < 0) { offenders.Add($"{signature}: not found in ExpressionBinder.cs (renamed? update this list: {why})"); continue; }
            bool opens = lines.Skip(at + 1).Take(LinesToOpen).Any(l => l.Contains("ctx.Edition.At(", StringComparison.Ordinal));
            if (!opens) offenders.Add($"{signature}: no `using var _ = ctx.Edition.At(...)` within {LinesToOpen} lines ({why})");
        }
        Assert.True(offenders.Count == 0,
            "A member that reports an operand-level diagnostic after the resolution does not position it at the operand, so a "
            + "continued statement reports it at the statement's first token (kb/Work PB4406):\n  " + string.Join("\n  ", offenders));
    }
}
