// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Common;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Identifies the division/section context where a genericClause appears.
/// Each context has a different semantic interpretation even though the syntax is identical.
/// </summary>
public enum GenericClauseContext
{
    IdentificationParagraph,
    ConfigurationVendor,
    SpecialNames,
    FileDescription,
    DataDescription,
    ReportGroup,
    FileControl,
    IOControl,
}

/// <summary>
/// A parsed genericClause with its context. Represents a vendor extension or
/// unrecognized clause that the parser accepted syntactically.
/// The binder classifies these by context and either routes to a known handler
/// or records them as extension nodes.
/// </summary>
public sealed class GenericClauseNode
{
    public GenericClauseContext Context { get; }
    public string LeadingIdentifier { get; }
    public IReadOnlyList<GenericClauseOperand> Operands { get; }
    public SourceLocation Location { get; }

    public GenericClauseNode(
        GenericClauseContext context,
        string leadingIdentifier,
        IReadOnlyList<GenericClauseOperand> operands,
        SourceLocation location)
    {
        Context = context;
        LeadingIdentifier = leadingIdentifier;
        Operands = operands;
        Location = location;
    }

    /// <summary>
    /// Build a GenericClauseNode from a parse tree GenericClauseContext.
    /// </summary>
    public static GenericClauseNode FromParseTree(
        Generated.CobolParserCore.GenericClauseContext ctx,
        GenericClauseContext context,
        string sourcePath)
    {
        var identifiers = ctx.IDENTIFIER();
        string leading = identifiers.Length > 0 ? identifiers[0].GetText() : "";

        var operands = new List<GenericClauseOperand>();
        // Skip the first IDENTIFIER (it's the leading keyword)
        for (int i = 1; i < identifiers.Length; i++)
            operands.Add(new IdentifierOperand(identifiers[i].GetText()));

        foreach (var lit in ctx.literal())
            operands.Add(new LiteralOperand(lit.GetText()));

        var location = new SourceLocation(sourcePath, 0, ctx.Start?.Line ?? 0, ctx.Start?.Column ?? 0);
        return new GenericClauseNode(context, leading, operands, location);
    }
}

/// <summary>Base class for operands within a generic clause.</summary>
public abstract class GenericClauseOperand { }

/// <summary>An IDENTIFIER operand in a generic clause.</summary>
public sealed class IdentifierOperand : GenericClauseOperand
{
    public string Name { get; }
    public IdentifierOperand(string name) => Name = name;
}

/// <summary>A literal operand in a generic clause (numeric or string).</summary>
public sealed class LiteralOperand : GenericClauseOperand
{
    public string Text { get; }
    public LiteralOperand(string text) => Text = text;
}
