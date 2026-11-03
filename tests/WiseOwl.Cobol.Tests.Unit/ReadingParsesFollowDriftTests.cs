// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE DRIFT GUARD FOR THE PARSER'S SPECULATIVE READINGS (kb/Work PB3248, branch review R2-1; PB845).
///
/// <para><c>CobolParserCoreBase.ReadingParses</c> calls a reading COMPLETE when it parses and stops at the period or at
/// a token in the rule's context-free FOLLOW set, read off the generated ATN. Two facts of the grammar carry its two
/// callers, and both are pinned here so a grammar change that breaks one fails by name rather than as a cascade
/// diagnostic. The entry-name predicate (<c>entryNameHere</c>) asks for the no-entry-name reading of a WHOLE data
/// description entry, which the FOLLOW test gives only while <c>dataDescriptionBody</c> is followed by the period
/// alone. The error report and recovery (<c>ReadStatementAt</c>) tells a reserved verb written as an operand
/// from one that begins the next statement by the token after the statement's prefix reading — <c>MOVE GOBACK TO
/// W-X</c> stops at TO and <c>IF GOBACK = 1</c> at '=' — so neither may ever follow a statement, while the period's
/// neighbours a statement really ends at (the next verb, a scope terminator, ELSE) must. A prefix that stops at one of those
/// tokens is not an operand's evidence in itself: <c>MOVE W-X TO</c> then <c>ADD 1 FROM W-Y.</c> stops at FROM, which no
/// reading of the sentence needs, so the ADD is a statement with a stray word of its own
/// (<c>SentenceReadsWithOneOperandFor</c>, kb/Work PB4285 item 3), where <c>MOVE FREE W-X TO W-Y.</c> stops at the MOVE's own TO.</para>
/// </summary>
public sealed class ReadingParsesFollowDriftTests
{
    [Fact]
    public void TheDataDescriptionBody_IsFollowedByThePeriodAlone()
    {
        var follow = CobolParserCoreBase.FollowSet(CobolParserCore._ATN, CobolParserCore.RULE_dataDescriptionBody);
        Assert.Equal([CobolParserCore.DOT], follow.ToList());
    }

    [Fact]
    public void AStatement_EndsAtTheNextStatementOrATerminator_NeverAtAnOperandsContinuation()
    {
        var follow = CobolParserCoreBase.FollowSet(CobolParserCore._ATN, CobolParserCore.RULE_statement);
        Assert.All([CobolParserCore.DOT, CobolParserCore.DISPLAY, CobolParserCore.END_IF, CobolParserCore.ELSE],
            t => Assert.True(follow.Contains(t), $"{CobolParserCore.DefaultVocabulary.GetSymbolicName(t)} must follow a statement"));
        Assert.All([CobolParserCore.TO, CobolParserCore.EQUALS, CobolParserCore.GIVING, CobolParserCore.FROM],
            t => Assert.False(follow.Contains(t), $"{CobolParserCore.DefaultVocabulary.GetSymbolicName(t)} must not follow a statement"));
    }
}
