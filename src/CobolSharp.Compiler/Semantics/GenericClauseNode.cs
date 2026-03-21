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

// ═══════════════════════════════════════════════════
// Operands
// ═══════════════════════════════════════════════════

/// <summary>Base class for operands within a generic/extension clause.</summary>
public abstract class GenericClauseOperand { }

/// <summary>An IDENTIFIER operand in a generic clause.</summary>
public sealed class IdentifierOperand : GenericClauseOperand
{
    public string Name { get; }
    public IdentifierOperand(string name) => Name = name;
    public override string ToString() => Name;
}

/// <summary>A literal operand in a generic clause (numeric or string).</summary>
public sealed class LiteralOperand : GenericClauseOperand
{
    public string Text { get; }
    public LiteralOperand(string text) => Text = text;
    public override string ToString() => Text;
}

// ═══════════════════════════════════════════════════
// Base extension clause node
// ═══════════════════════════════════════════════════

/// <summary>
/// Base for all extension/vendor clause nodes captured from genericClause.
/// Each subclass represents a specific division/section context.
/// Grammar accepts; binder classifies; no genericClause is silently ignored.
/// </summary>
public abstract class ExtensionClauseNode
{
    /// <summary>The division/section context where this clause appeared.</summary>
    public GenericClauseContext Context { get; }

    /// <summary>The leading IDENTIFIER — the clause keyword (e.g., "LOCK", "FOO").</summary>
    public string Keyword { get; }

    /// <summary>Remaining operands after the keyword, in source order.</summary>
    public IReadOnlyList<GenericClauseOperand> Operands { get; }

    /// <summary>Source location for diagnostics.</summary>
    public SourceLocation Location { get; }

    protected ExtensionClauseNode(
        GenericClauseContext context,
        string keyword,
        IReadOnlyList<GenericClauseOperand> operands,
        SourceLocation location)
    {
        Context = context;
        Keyword = keyword;
        Operands = operands;
        Location = location;
    }

    /// <summary>Human-readable context name for diagnostics.</summary>
    public string ContextName => Context switch
    {
        GenericClauseContext.IdentificationParagraph => "IDENTIFICATION DIVISION",
        GenericClauseContext.ConfigurationVendor => "CONFIGURATION SECTION",
        GenericClauseContext.SpecialNames => "SPECIAL-NAMES",
        GenericClauseContext.FileDescription => "FILE DESCRIPTION (FD)",
        GenericClauseContext.DataDescription => "DATA DESCRIPTION",
        GenericClauseContext.ReportGroup => "REPORT GROUP",
        GenericClauseContext.FileControl => "FILE-CONTROL",
        GenericClauseContext.IOControl => "I-O-CONTROL",
        _ => "UNKNOWN"
    };

    /// <summary>
    /// Build the appropriate typed extension node from a parse tree GenericClauseContext.
    /// </summary>
    public static ExtensionClauseNode FromParseTree(
        Generated.CobolParserCore.GenericClauseContext ctx,
        GenericClauseContext context,
        string sourcePath)
    {
        var identifiers = ctx.IDENTIFIER();
        string keyword = identifiers.Length > 0 ? identifiers[0].GetText() : "";

        var operands = new List<GenericClauseOperand>();
        // Skip the first IDENTIFIER (it's the keyword)
        for (int i = 1; i < identifiers.Length; i++)
            operands.Add(new IdentifierOperand(identifiers[i].GetText()));

        foreach (var lit in ctx.literal())
            operands.Add(new LiteralOperand(lit.GetText()));

        var location = new SourceLocation(sourcePath, 0, ctx.Start?.Line ?? 0, ctx.Start?.Column ?? 0);

        return context switch
        {
            GenericClauseContext.IdentificationParagraph => new IdentificationExtensionParagraph(keyword, operands, location),
            GenericClauseContext.ConfigurationVendor => new ConfigurationExtensionEntry(keyword, operands, location),
            GenericClauseContext.SpecialNames => new SpecialNamesExtensionClause(keyword, operands, location),
            GenericClauseContext.FileDescription => new FileDescriptionExtensionClause(keyword, operands, location),
            GenericClauseContext.DataDescription => new DataDescriptionExtensionClause(keyword, operands, location),
            GenericClauseContext.ReportGroup => new ReportGroupExtensionClause(keyword, operands, location),
            GenericClauseContext.FileControl => new FileControlExtensionClause(keyword, operands, location),
            GenericClauseContext.IOControl => new IOControlExtensionEntry(keyword, operands, location),
            _ => throw new ArgumentOutOfRangeException(nameof(context))
        };
    }
}

// ═══════════════════════════════════════════════════
// Context-specific extension node types
// ═══════════════════════════════════════════════════

/// <summary>Unrecognized paragraph in IDENTIFICATION DIVISION.</summary>
public sealed class IdentificationExtensionParagraph(string keyword, IReadOnlyList<GenericClauseOperand> operands, SourceLocation location)
    : ExtensionClauseNode(GenericClauseContext.IdentificationParagraph, keyword, operands, location);

/// <summary>Vendor paragraph in CONFIGURATION SECTION.</summary>
public sealed class ConfigurationExtensionEntry(string keyword, IReadOnlyList<GenericClauseOperand> operands, SourceLocation location)
    : ExtensionClauseNode(GenericClauseContext.ConfigurationVendor, keyword, operands, location);

/// <summary>Unrecognized clause in SPECIAL-NAMES paragraph.</summary>
public sealed class SpecialNamesExtensionClause(string keyword, IReadOnlyList<GenericClauseOperand> operands, SourceLocation location)
    : ExtensionClauseNode(GenericClauseContext.SpecialNames, keyword, operands, location);

/// <summary>Unrecognized clause in FD (File Description) entry.</summary>
public sealed class FileDescriptionExtensionClause(string keyword, IReadOnlyList<GenericClauseOperand> operands, SourceLocation location)
    : ExtensionClauseNode(GenericClauseContext.FileDescription, keyword, operands, location);

/// <summary>Unrecognized clause in data description entry.</summary>
public sealed class DataDescriptionExtensionClause(string keyword, IReadOnlyList<GenericClauseOperand> operands, SourceLocation location)
    : ExtensionClauseNode(GenericClauseContext.DataDescription, keyword, operands, location);

/// <summary>Unrecognized clause in report group entry.</summary>
public sealed class ReportGroupExtensionClause(string keyword, IReadOnlyList<GenericClauseOperand> operands, SourceLocation location)
    : ExtensionClauseNode(GenericClauseContext.ReportGroup, keyword, operands, location);

/// <summary>Vendor clause in FILE-CONTROL paragraph.</summary>
public sealed class FileControlExtensionClause(string keyword, IReadOnlyList<GenericClauseOperand> operands, SourceLocation location)
    : ExtensionClauseNode(GenericClauseContext.FileControl, keyword, operands, location);

/// <summary>Vendor entry in I-O-CONTROL paragraph.</summary>
public sealed class IOControlExtensionEntry(string keyword, IReadOnlyList<GenericClauseOperand> operands, SourceLocation location)
    : ExtensionClauseNode(GenericClauseContext.IOControl, keyword, operands, location);

