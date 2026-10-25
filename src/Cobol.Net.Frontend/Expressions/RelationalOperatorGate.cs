// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Expressions;

/// <summary>
/// ⭐ THE relational-operator INTRODUCTION gate — ONE rule, asked by BOTH lanes. The symbol <c>&lt;&gt;</c> is "an
/// abbreviation for NOT EQUAL" (ISO §8.7.5.2 SR11) and a COBOL-2014 introduction: the 2002 text's clause 8.7.5.2 ends its
/// abbreviation rules at <c>&lt;=</c>, and Annex D.3 (2002→2014 changes) item 16 of the 2009 committee draft adds "The symbol
/// &lt;&gt; … as an extended relational operator" (kb/Work PB3604; row <c>not-equal-operator-2014</c>). Checked through the ONE
/// <see cref="ConstructRegistry"/> funnel so the code and the §4.2 severity are the registry's.
/// <para><b>One node to recognize.</b> Every relation — a condition's, an EVALUATE <c>WHEN</c> partial expression's, a
/// constant-conditional-expression's — spells its operator through the one <c>comparisonOperator</c> grammar rule, the only
/// rule that holds the <c>NOTEQUAL</c> token, so the gate asks for that node and never for a tier (the
/// <see cref="LogicalOperatorGate"/> precedent, kb/Work PB1390).</para>
/// <para><b>Both lanes.</b> <c>VersionConformancePass</c> calls it per <c>comparisonOperator</c> node of the compilation
/// unit; the conditional-compilation stage calls it per constant-conditional-expression FRAGMENT, which the
/// compilation-unit walk never reaches — a <c>&gt;&gt;IF</c> operand is "formed according to the rules in 8.8.4.2" (§7.3.8.2
/// SR1 a)) of the targeted edition, so <c>&gt;&gt;IF 5 &lt;&gt; 3</c> below 2014 is the same COBOLNET0900 its runtime twin
/// draws. Until kb/Work PB3604 the row sat at 2002 and the directive lane had no gate at all.</para>
/// </summary>
public static class RelationalOperatorGate
{
    /// <summary>Report the <c>&lt;&gt;</c> operator if <paramref name="tree"/> contains one and
    /// <paramref name="edition"/> has not introduced it — once per call, however many the tree holds.</summary>
    /// <param name="edition">The targeted edition.</param>
    /// <param name="sink">The caller's positioned diagnostic sink.</param>
    /// <param name="tree">A <c>comparisonOperator</c> node, or a fragment subtree to scan.</param>
    public static void Check(EditionInfo edition, IDiagnosticSink sink, IParseTree tree)
    {
        if (Contains(tree))
            ConstructRegistry.Check(edition, sink, Constructs.NotEqualOperator2014, "the relational operator <>");
    }

    private static bool Contains(IParseTree t)
    {
        if (t is CobolParserCore.ComparisonOperatorContext op && op.NOTEQUAL() is not null) return true;
        for (int i = 0; i < t.ChildCount; i++)
            if (Contains(t.GetChild(i))) return true;
        return false;
    }
}
