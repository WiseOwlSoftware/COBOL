// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Common;
using Common = CobolNet.Frontend.Common;   // alias for the namespace-relative Common. shorthand (rearch P1 rename)
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolSharp.Compiler.Semantics.Bound.Binding;

/// <summary>
/// Procedure name resolution: ExtractProcedureNameText, ResolveProcedureName,
/// ResolveProcedureNameForThruEnd, ResolveProcedureNameForPerform.
/// </summary>
internal sealed class ProcedureNameResolver
{
    private readonly BindingContext _ctx;

    internal ProcedureNameResolver(BindingContext ctx) => _ctx = ctx;

    /// <summary>Extract the paragraph/section name from a procedureName context.
    /// Uses first cobolWord/INTEGERLIT token only, ignoring OF/IN qualifiers.</summary>
    internal static string ExtractProcedureNameText(CobolParserCore.ProcedureNameContext ctx)
    {
        var words = ctx.cobolWord();
        if (words.Length > 0) return words[0].GetText();
        var ints = ctx.INTEGERLIT();
        if (ints.Length > 0) return ints[0].GetText();
        return ctx.GetText();
    }

    /// <summary>Extract both the paragraph/section name and optional OF/IN section qualifier
    /// from a procedureName context. Grammar: (cobolWord | INTEGERLIT) ((OF | IN) (cobolWord | INTEGERLIT))?</summary>
    internal static (string name, string? qualifier) ExtractProcedureNameWithQualifier(CobolParserCore.ProcedureNameContext ctx)
    {
        string name = ExtractProcedureNameText(ctx);

        // Check for OF/IN qualifier — if present, the qualifier is the last cobolWord/INTEGERLIT child
        if (ctx.OF() == null && ctx.IN() == null) return (name, null);

        // With OF/IN present, there are 2 cobolWord/INTEGERLIT children.
        // The qualifier is the second one. Collect all in parse order by child index.
        var words = ctx.cobolWord();
        var ints = ctx.INTEGERLIT();
        int totalTokens = words.Length + ints.Length;
        if (totalTokens < 2) return (name, null);

        // The last cobolWord or INTEGERLIT by child index is the qualifier
        string qualifier;
        if (words.Length > 1)
            qualifier = words[^1].GetText();
        else if (ints.Length > 1)
            qualifier = ints[^1].GetText();
        else
        {
            // One cobolWord + one INTEGERLIT — pick whichever appears later in the tree
            int wordIdx = GetChildIndex(ctx, words[0]);
            int intIdx = GetChildIndex(ctx, ints[0]);
            qualifier = intIdx > wordIdx ? ints[0].GetText() : words[0].GetText();
            // But name is the first one, so qualifier must be the other
            if (intIdx > wordIdx)
                qualifier = ints[0].GetText();
            else
                qualifier = words[0].GetText();
        }

        return (name, qualifier);
    }

    private static int GetChildIndex(Antlr4.Runtime.ParserRuleContext parent, Antlr4.Runtime.Tree.IParseTree child)
    {
        for (int i = 0; i < parent.ChildCount; i++)
            if (parent.GetChild(i) == child) return i;
        return -1;
    }

    /// <summary>
    /// Resolve a paragraph name within a specific section's scope.
    /// Used for section-qualified procedure names (e.g., PAR-2A OF SECTION-1).
    /// </summary>
    private ParagraphSymbol? ResolveQualifiedParagraph(string paragraphName, string sectionName)
    {
        var sec = _ctx.Semantic.ResolveSection(sectionName);
        if (sec == null)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0402,
                Common.SourceLocation.None,
                Common.TextSpan.Empty, $"{paragraphName} OF {sectionName}");
            return null;
        }

        // Look up the paragraph in the section's scope (which has section-local paragraphs)
        var para = sec.Scope.Resolve<ParagraphSymbol>(paragraphName);
        if (para != null) return para;

        _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0402,
            Common.SourceLocation.None,
            Common.TextSpan.Empty, $"{paragraphName} OF {sectionName}");
        return null;
    }

    /// <summary>
    /// Resolve a procedure name (paragraph or section) to a ParagraphSymbol.
    /// For sections, returns the first paragraph in the section.
    /// For paragraphs, returns the paragraph directly.
    /// </summary>
    internal ParagraphSymbol? ResolveProcedureName(string name, string? sectionQualifier = null)
    {
        // Section-qualified resolution: look up paragraph within the specified section
        if (sectionQualifier != null)
            return ResolveQualifiedParagraph(name, sectionQualifier);

        var para = _ctx.ResolveParagraphScoped(name);   // OO §11.7: method-local first, then program-wide
        var sec = _ctx.Semantic.ResolveSection(name);

        if (para != null && sec != null)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0400,
                Common.SourceLocation.None,
                Common.TextSpan.Empty, name);
            return para;
        }

        if (para != null) return para;

        if (sec != null)
        {
            var sectionParas = _ctx.Semantic.GetSectionParagraphs(name);
            if (sectionParas != null && sectionParas.Count > 0)
                return _ctx.Semantic.ResolveParagraph(sectionParas[0]);

            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0401,
                Common.SourceLocation.None,
                Common.TextSpan.Empty, name);
            return null;
        }

        _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0402,
            Common.SourceLocation.None,
            Common.TextSpan.Empty, name);
        return null;
    }

    /// <summary>
    /// Resolve a procedure name for THRU end targets.
    /// For sections, returns the LAST paragraph (end of section range).
    /// For paragraphs, returns the paragraph itself.
    /// </summary>
    internal ParagraphSymbol? ResolveProcedureNameForThruEnd(string name, string? sectionQualifier = null)
    {
        // Section-qualified resolution: look up paragraph within the specified section
        if (sectionQualifier != null)
            return ResolveQualifiedParagraph(name, sectionQualifier);

        var para = _ctx.ResolveParagraphScoped(name);   // OO §11.7: method-local first, then program-wide
        var sec = _ctx.Semantic.ResolveSection(name);

        if (para != null && sec != null)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0400,
                Common.SourceLocation.None,
                Common.TextSpan.Empty, name);
            return para;
        }

        if (para != null) return para;

        if (sec != null)
        {
            var sectionParas = _ctx.Semantic.GetSectionParagraphs(name);
            if (sectionParas != null && sectionParas.Count > 0)
                return _ctx.Semantic.ResolveParagraph(sectionParas[^1]); // LAST paragraph

            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0401,
                Common.SourceLocation.None,
                Common.TextSpan.Empty, name);
            return null;
        }

        _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0402,
            Common.SourceLocation.None,
            Common.TextSpan.Empty, name);
        return null;
    }

    internal (ParagraphSymbol? first, ParagraphSymbol? last) ResolveProcedureNameForPerform(string name, string? sectionQualifier = null)
    {
        // Section-qualified resolution: look up paragraph within the specified section
        if (sectionQualifier != null)
        {
            var para = ResolveQualifiedParagraph(name, sectionQualifier);
            return (para, null);
        }

        var para2 = _ctx.ResolveParagraphScoped(name);   // OO §11.7: method-local first, then program-wide
        var sec = _ctx.Semantic.ResolveSection(name);

        if (para2 != null && sec != null)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0400,
                Common.SourceLocation.None,
                Common.TextSpan.Empty, name);
            return (para2, null);
        }

        if (para2 != null) return (para2, null);

        if (sec != null)
        {
            var sectionParas = _ctx.Semantic.GetSectionParagraphs(name);
            if (sectionParas != null && sectionParas.Count > 0)
            {
                var first = _ctx.Semantic.ResolveParagraph(sectionParas[0]);
                var last = _ctx.Semantic.ResolveParagraph(sectionParas[^1]);
                return (first, sectionParas.Count > 1 ? last : null);
            }

            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0401,
                Common.SourceLocation.None,
                Common.TextSpan.Empty, name);
            return (null, null);
        }

        _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0402,
            Common.SourceLocation.None,
            Common.TextSpan.Empty, name);
        return (null, null);
    }
}
