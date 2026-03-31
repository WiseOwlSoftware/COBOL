// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Generated;

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
    /// Uses first IDENTIFIER/INTEGERLIT token only, ignoring OF/IN qualifiers.</summary>
    internal static string ExtractProcedureNameText(CobolParserCore.ProcedureNameContext ctx)
    {
        var ids = ctx.IDENTIFIER();
        if (ids.Length > 0) return ids[0].GetText();
        var ints = ctx.INTEGERLIT();
        if (ints.Length > 0) return ints[0].GetText();
        return ctx.GetText();
    }

    /// <summary>
    /// Resolve a procedure name (paragraph or section) to a ParagraphSymbol.
    /// For sections, returns the first paragraph in the section.
    /// For paragraphs, returns the paragraph directly.
    /// </summary>
    internal ParagraphSymbol? ResolveProcedureName(string name)
    {
        var para = _ctx.Semantic.ResolveParagraph(name);
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
    internal ParagraphSymbol? ResolveProcedureNameForThruEnd(string name)
    {
        var para = _ctx.Semantic.ResolveParagraph(name);
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

    internal (ParagraphSymbol? first, ParagraphSymbol? last) ResolveProcedureNameForPerform(string name)
    {
        var para = _ctx.Semantic.ResolveParagraph(name);
        var sec = _ctx.Semantic.ResolveSection(name);

        if (para != null && sec != null)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0400,
                Common.SourceLocation.None,
                Common.TextSpan.Empty, name);
            return (para, null);
        }

        if (para != null) return (para, null);

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
