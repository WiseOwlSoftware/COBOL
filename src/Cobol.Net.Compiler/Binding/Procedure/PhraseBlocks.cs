// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The ONE binder of a Table 12 conditional-phrase pair (P7 Step 10b; kb/Work PB3830). Every statement's [NOT] phrases
/// are the grammar's one <c>conditionalPhrases</c> rule (AT END §14.9.30/§14.9.34, INVALID KEY, ON SIZE ERROR §14.7.5,
/// ON OVERFLOW §14.9.43/§14.9.48, ON EXCEPTION, AT END-OF-PAGE §14.9.51), each phrase classified by
/// <see cref="ConditionalPhrases"/>, so a verb binder asks for the family it owns and receives the (positive, NOT)
/// imperative-statements by ROLE — §5.2.6.4's choice indicators let the two be written in either order, and the order of
/// writing never changes which branch is which.
/// </summary>
internal static class PhraseBlocks
{
    /// <summary>Bind the (positive, NOT) branches of <paramref name="family"/>'s pair; either side is null when unwritten.
    /// The blocks are bound in WRITTEN order, so the diagnostics they draw keep source order. <paramref name="bind"/> is
    /// the caller's statement-list binder (the collaborator split keeps binding on the verb binder; this class owns only
    /// the shared SHAPE).</summary>
    public static (List<BoundStatement>? On, List<BoundStatement>? NotOn) Bind(
        Core.ConditionalPhrasesContext? phrases, ConditionalPhraseFamily family,
        Func<Core.StatementBlockContext, List<BoundStatement>> bind)
    {
        List<BoundStatement>? on = null, notOn = null;
        if (phrases is not null)
            foreach (var phrase in phrases.conditionalPhrase())
            {
                var kind = ConditionalPhrases.KindOf(phrase);
                if (kind.Family != family) continue;
                if (kind.Negated) notOn = bind(phrase.statementBlock());
                else on = bind(phrase.statementBlock());
            }
        return (on, notOn);
    }
}
