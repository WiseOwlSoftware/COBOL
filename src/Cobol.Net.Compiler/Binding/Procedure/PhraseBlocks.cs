// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Binding.Bound;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The ONE conditional-phrase pair extractor (P7 Step 10b — replaces the ~8 per-verb clones the phase doc
/// enumerates). Every ISO two-branch phrase shares the grammar shape
/// <c>ON-X b1 (NOT-ON-X b2)? | NOT-ON-X b1</c> (AT END §14.9.30/§14.9.34, INVALID KEY §9.1.14, ON SIZE ERROR
/// §14.7.5, ON OVERFLOW §14.9.43/§14.9.48, ON EXCEPTION §14.9.10, AT END-OF-PAGE §14.9.51): blocks[0] is the
/// first written branch, blocks[1] the second when both are present, and a leading NOT token marks the
/// NOT-first alternative. The positional swap below is TOTAL over every legal shape: NOT-only (one block,
/// NOT-led) → (null, b0); RETURN's §14.9.34.3 SR4 reversed PAIR (two blocks, NOT-led) → (b1, b0); the normal
/// order → (b0, b1). For the three rules whose grammar cannot produce a NOT-led form (readAtEnd,
/// writeAtEndOfPage, deleteFileOnException) passing <see cref="StartsWithNot"/> is a provable no-op — the
/// premise audit's shape table — so ALL call sites use the same call uniformly.
/// </summary>
internal static class PhraseBlocks
{
    /// <summary>Split a phrase's statement blocks into its (positive, NOT) branch pair; either side is null
    /// when unwritten. <paramref name="bind"/> is the caller's statement-list binder (the collaborator split
    /// keeps binding on the verb binder; this class owns only the shared SHAPE).</summary>
    public static (List<BoundStatement>? On, List<BoundStatement>? NotOn) Split(
        Core.StatementBlockContext[] blocks, bool notFirst,
        Func<Core.StatementBlockContext, List<BoundStatement>> bind)
    {
        List<BoundStatement>? first = blocks.Length >= 1 ? bind(blocks[0]) : null;
        List<BoundStatement>? second = blocks.Length >= 2 ? bind(blocks[1]) : null;
        return notFirst ? (second, first) : (first, second);
    }

    /// <summary>True when the phrase context begins with a NOT token — the ONE discriminator of the NOT-first
    /// alternative every two-branch phrase rule shares.</summary>
    public static bool StartsWithNot(IParseTree ctx) =>
        ctx.ChildCount > 0 && ctx.GetChild(0) is ITerminalNode t && t.Symbol.Type == CobolLexer.NOT;
}
