// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Frontend.Parsing;

namespace WiseOwl.Cobol.Frontend;

/// <summary>The kind of source unit a <see cref="SourceUnit"/> is.</summary>
public enum SourceUnitKind { Program, Function, Class, Interface, Factory, Object, Method }

/// <summary>One source unit of the compilation group as the parse read it (DESIGN-listing.md section 4.3).</summary>
/// <param name="Id">0-based, in text order (the order the parse met the units' first tokens).</param>
/// <param name="Parent">The unit that directly contains this one, or null for an outermost unit.</param>
/// <param name="Kind">Program, function, class, ...</param>
/// <param name="Name">The name the unit's identification paragraph declares, as written (FACTORY and OBJECT carry their
/// keyword).</param>
/// <param name="FirstLine">The 1-based RESULTANT line of the unit's first token.</param>
/// <param name="LastLine">The 1-based RESULTANT line of its last token.</param>
public sealed record SourceUnit(int Id, int? Parent, SourceUnitKind Kind, string Name, int FirstLine, int LastLine);

/// <summary>A position of the resultant text (1-based line, 0-based column), as the parser's tokens carry it.</summary>
public readonly record struct ResultantPoint(int Line, int Column);

/// <summary>One statement as the parse read it (DESIGN-listing.md section 6.8): its verb as written and where it begins.</summary>
/// <param name="Verb">The first word of the statement, in upper case: the verb as the programmer wrote it, never as the binder
/// desugars it (<c>GO</c> of GO TO, <c>NEXT</c> of NEXT SENTENCE).</param>
/// <param name="At">The position of that word.</param>
public sealed record SourceStatement(string Verb, ResultantPoint At);

/// <summary>
/// ⛔ WHAT THE PARSE SAYS ABOUT THE SOURCE that the listing places on it (DESIGN-listing.md sections 4.3 and 11, step 1;
/// kb/Work PB5923): the source units with their nesting and resultant line ranges (the page heading names the innermost
/// program of its first line, §1985:VI.3.3.4 GR1 and §1985:X.3.1.4 GR1), the DATE-COMPILED paragraphs whose comment-entry
/// the listing replaces (§1985:VI.3.4.4 GR1), and the head of every comment-entry paragraph (which settles a
/// <see cref="Preprocessor.PhysicalLineKind.CommentEntryCandidate"/> line), and every statement with its verb (the listing's
/// statement count and verb counts, section 6.8). Read off the parse tree the parser built,
/// recovered or not, so a listing of a program that does not parse still names its programs. Collected only when the
/// compilation asks for a listing (<see cref="Frontend.RecordsListing"/>).
/// </summary>
public sealed class SourceUnitFacts
{
    /// <summary>Every source unit, in text order.</summary>
    public required IReadOnlyList<SourceUnit> Units { get; init; }

    /// <summary>The position of the DATE-COMPILED word of every DATE-COMPILED paragraph.</summary>
    public required IReadOnlyList<ResultantPoint> DateCompiled { get; init; }

    /// <summary>The position of the paragraph word of every comment-entry paragraph (AUTHOR, INSTALLATION, DATE-WRITTEN,
    /// DATE-COMPILED, SECURITY, REMARKS).</summary>
    public required IReadOnlyList<ResultantPoint> CommentEntryParagraphs { get; init; }

    /// <summary>Every statement the parse read, in text order, a statement nested in another included.</summary>
    public required IReadOnlyList<SourceStatement> Statements { get; init; }

    /// <summary>Whether the parse recovered from a syntax error (the front end's own verdict on the pass the tree is of): the
    /// recovery may have skipped source, so a unit or statement the source holds may be missing from these facts
    /// (DESIGN-listing.md section 6.8).</summary>
    public required bool Recovered { get; init; }

    /// <summary>Read the facts off <paramref name="tree"/>, which the parse built with recovery when <paramref name="recovered"/>.</summary>
    public static SourceUnitFacts Read(CobolParserCore.CompilationUnitContext tree, bool recovered)
    {
        var units = new List<SourceUnit>();
        var dateCompiled = new List<ResultantPoint>();
        var commentEntries = new List<ResultantPoint>();
        var statements = new List<SourceStatement>();
        Walk(tree, parent: null, units, dateCompiled, commentEntries, statements);
        return new SourceUnitFacts
        {
            Units = units, DateCompiled = dateCompiled, CommentEntryParagraphs = commentEntries, Statements = statements,
            Recovered = recovered,
        };
    }

    private static void Walk(IParseTree node, int? parent, List<SourceUnit> units, List<ResultantPoint> dateCompiled,
        List<ResultantPoint> commentEntries, List<SourceStatement> statements)
    {
        if (node is not ParserRuleContext context) return;
        int? inner = parent;
        if (UnitOf(context) is ({ } kind, { } name) && context.Start is { } start && context.Stop is { } stop)
        {
            inner = units.Count;
            units.Add(new SourceUnit(units.Count, parent, kind, name, start.Line, Math.Max(start.Line, stop.Line)));
        }
        switch (context)
        {
            case CobolParserCore.DateCompiledParagraphContext:
                dateCompiled.Add(new ResultantPoint(context.Start.Line, context.Start.Column));
                commentEntries.Add(new ResultantPoint(context.Start.Line, context.Start.Column));
                return;
            case ParserRuleContext when Parsing.CommentEntryParagraph.Is(context):
                commentEntries.Add(new ResultantPoint(context.Start.Line, context.Start.Column));
                return;
            // A statement the parse began and recovered from consumed at least its first token; one that consumed none (its
            // stop token precedes its start) is no statement of the source.
            case CobolParserCore.StatementContext when context.Start is { Type: > 0 } verb
                && context.Stop is { } last && last.TokenIndex >= verb.TokenIndex:
                statements.Add(new SourceStatement(VerbOf(verb.Text), new ResultantPoint(verb.Line, verb.Column)));
                break;
        }
        for (int i = 0; i < context.ChildCount; i++)
            Walk(context.GetChild(i), inner, units, dateCompiled, commentEntries, statements);
    }

    /// <summary>The verb a statement's first token spells: its first word (a token that is a whole embedded block, which a
    /// recovered parse can hold, begins with its word), in upper case. A verb is a reserved word of the standard, so the
    /// invariant upper case is its one spelling.</summary>
    private static string VerbOf(string token)
    {
        int end = 0;
        while (end < token.Length && !char.IsWhiteSpace(token[end])) end++;
        return token[..end].ToUpperInvariant();
    }

    /// <summary>The kind and name of the source unit <paramref name="context"/> opens, or nulls when it opens none (or the
    /// recovered tree lacks its name).</summary>
    private static (SourceUnitKind? Kind, string? Name) UnitOf(ParserRuleContext context) => context switch
    {
        CobolParserCore.ProgramUnitContext p => ProgramOrFunction(p.identificationDivision()),
        CobolParserCore.NestedProgramContext n => ProgramOrFunction(n.identificationDivision()),
        CobolParserCore.ClassDefinitionContext c => (SourceUnitKind.Class, c.classIdParagraph()?.className(0)?.GetText()),
        CobolParserCore.InterfaceDefinitionContext i => (SourceUnitKind.Interface, i.interfaceName(0)?.GetText()),
        CobolParserCore.FactoryParagraphContext => (SourceUnitKind.Factory, "FACTORY"),
        CobolParserCore.ObjectParagraphContext => (SourceUnitKind.Object, "OBJECT"),
        CobolParserCore.MethodDefinitionContext m =>
            (SourceUnitKind.Method, m.methodName(0)?.GetText() ?? m.methodPropertySelector()?.GetText()),
        _ => (null, null),
    };

    private static (SourceUnitKind? Kind, string? Name) ProgramOrFunction(CobolParserCore.IdentificationDivisionContext? division)
    {
        var body = division?.identificationBody();
        if (body?.programIdParagraph()?.programName() is { } program) return (SourceUnitKind.Program, program.GetText());
        if (body?.functionIdParagraph()?.programName() is { } function) return (SourceUnitKind.Function, function.GetText());
        return (null, null);
    }
}
