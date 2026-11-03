// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Compiler.Oo;

using Core = CobolParserCore;

/// <summary>
/// ⛔ A PARAMETERIZED DEFINITION AS A SKELETON — written in this group or published by another (ISO §9.3.12, §9.3.13;
/// DESIGN-external-repository §10.5; kb/Work PB2104). It is the one thing an expansion is made from and the one thing a group
/// publishes for a parameterized definition: the definition's context (the group's own tree, or the tree the recorded
/// <see cref="Text"/> was parsed into), its identity, formals, source text after text manipulation and the
/// <see cref="CompileState"/> it was compiled under. Its <c>Resolutions</c> are the class-specifiers and interface-specifiers of its
/// own REPOSITORY paragraph that are not formals (<see cref="ResolutionEntries"/>); what each stands for is asked of the class table
/// (<see cref="ToRecord"/>), because a group class a specifier names is known by its externalized name once the table exists.
/// </summary>
/// <param name="Name">The definition's declared name.</param>
/// <param name="Identity">Its externalized name (ISO §11.3.4 GR1: "literal-1, if specified, is the name of the class that is externalized to the operating environment"; else the name).</param>
/// <param name="IsInterface">An INTERFACE-ID definition.</param>
/// <param name="Ctx">The definition (a class or an interface context).</param>
/// <param name="Text">The definition's source text (<see cref="SkeletonText"/>).</param>
/// <param name="State">The compile state the definition was compiled under.</param>
/// <param name="Formals">The parameter-names, in USING order.</param>
/// <param name="FormalIsInterface">For each formal, whether its specifier declares an interface.</param>
/// <param name="ResolutionEntries">The class-specifiers and interface-specifiers of the definition's own REPOSITORY paragraph that
/// are not formals.</param>
/// <param name="From">The assembly that published the skeleton, or null for this group's own.</param>
internal sealed record SkeletonSource(string Name, string Identity, bool IsInterface, ParserRuleContext Ctx, string Text, CompileState State,
    IReadOnlyList<string> Formals, IReadOnlyList<bool> FormalIsInterface, IReadOnlyList<Core.RepositoryEntryContext> ResolutionEntries,
    ImportedFrom? From)
{
    /// <summary>The record this skeleton publishes (§6.1 <see cref="SkeletonRecord"/>); <paramref name="resolve"/> says what a
    /// specifier of the definition's REPOSITORY paragraph stands for: its word, whether it is an interface, and its identity.</summary>
    public SkeletonRecord ToRecord(Func<Core.RepositoryEntryContext, ResolutionRecord> resolve) =>
        new(new RecordList<string>([.. Formals]), new RecordList<bool>([.. FormalIsInterface]), Text,
            new RecordList<ResolutionRecord>([.. ResolutionEntries.Select(resolve)]), State);
}

/// <summary>
/// WHAT MAKES A CLASS OR AN INTERFACE AN EXPANSION (ISO §12.3.8.4 GR5 / GR8): the skeleton it was made from, the REPOSITORY entry
/// that asked for it (<c>CLASS name [AS literal] EXPANDS definition USING actual …</c>) and the words of its actuals as that entry's
/// paragraph declares them. The bind session's source map holds it for the expansion's symbol (<c>SourceMap.ExpansionOf</c>, kb/Work PB2401) so the repository can publish the
/// expansion's identity and the placement pass can name what it means; an actual's IDENTITY is asked of the table once the table
/// exists, as a resolution's is.
/// </summary>
/// <param name="Skeleton">The skeleton it was made from.</param>
/// <param name="Entry">The REPOSITORY entry that declares the expansion.</param>
/// <param name="ActualWords">The actual parameters, in USING order, as written.</param>
/// <param name="Text">The expansion's own source text: the skeleton's tokens with the header renamed, the USING clause dropped and each
/// formal replaced by its actual (<see cref="OoExpansion"/>), which an expansion made in an assembly of its own is compiled from.</param>
/// <param name="Region">The region every token of the expansion's text names (<see cref="ExpansionToken"/>): what a diagnostic raised
/// while the group binds the text is attributed to, where it is printed and who answers for it — the group withdraws the ones of a
/// repository text that the expansion's own compilation, or the one that made an imported expansion, reports
/// (<c>EditionContext.WithdrawWarningsOf</c>, kb/Work PB4469, PB4483).</param>
internal sealed record ExpansionOrigin(SkeletonSource Skeleton, Core.RepositoryEntryContext Entry, IReadOnlyList<string> ActualWords, string Text,
    DiagnosticRegion Region);
