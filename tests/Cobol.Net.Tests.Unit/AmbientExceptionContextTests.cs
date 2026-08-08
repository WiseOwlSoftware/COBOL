// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE AMBIENT STATEMENT CONTEXT CONTRACT (kb/Work R14 — ISO §15.32.3 r1/r2, §15.30.3 r1/r2). The
/// (Table-12 statement name, location) pair is a property of the RAISING STATEMENT and travels ambiently:
/// the emitted checked-statement wrapper enters it with EXACTLY the names whose enabling TURN carried WITH
/// LOCATION, and the 2-argument <c>Set</c> — the form every raise site an emitter cannot thread uses —
/// resolves the pair from it PER-CONDITION. These pins are what made the F3 family of defects (SEARCH /
/// CONTINUE AFTER / the runtime string sites answering 63 spaces under WITH LOCATION) impossible to
/// reintroduce by adding a new 2-argument raise site: the site inherits the channel by construction.
/// </summary>
public sealed class AmbientExceptionContextTests
{
    private static ExceptionEngine Fresh() => new();

    [Fact]
    public void TwoArgSet_InsideCoveredStatement_StampsThePair()
    {
        var e = Fresh();
        var sv = e.EnterStatement("SEARCH", "PROG; PARA; 12", ["EC-RANGE-SEARCH-NO-MATCH"]);
        e.Set("EC-RANGE-SEARCH-NO-MATCH", fatal: false);
        Assert.Equal("SEARCH", e.LastStatement);
        Assert.Equal("PROG; PARA; 12", e.LastLocation);
        e.ExitStatement(sv);
    }

    [Fact]
    public void TwoArgSet_OfAnUncoveredName_AnswersR1Spaces()
    {
        // §15.32.3 r1 is PER-CONDITION (kb/Work R06): a sibling condition's WITH LOCATION must not
        // contaminate a name whose own enabling TURN did not carry it.
        var e = Fresh();
        var sv = e.EnterStatement("COMPUTE", "PROG; PARA; 30", ["EC-BOUND-SUBSCRIPT"]);
        e.Set("EC-SIZE-TRUNCATION", fatal: true);
        Assert.Null(e.LastStatement);
        Assert.Null(e.LastLocation);
        e.ExitStatement(sv);
    }

    [Fact]
    public void TwoArgSet_OutsideAnyStatement_AnswersR1Spaces()
    {
        var e = Fresh();
        e.Set("EC-RANGE-SEARCH-NO-MATCH", fatal: false);
        Assert.Null(e.LastStatement);
        Assert.Null(e.LastLocation);
    }

    [Fact]
    public void PositionalOperands_AlwaysWin_OverTheAmbientPair()
    {
        // The one remaining positional channel is the per-(name, FILE) I-O bridge (__IoCheckEc), whose
        // file-scoped WITH LOCATION a name set cannot express — its explicit operands take precedence.
        var e = Fresh();
        var sv = e.EnterStatement("READ", "PROG; PARA; 40", ["EC-I-O-AT-END"]);
        e.Set("EC-I-O-AT-END", fatal: false, statement: "READ", location: "PROG; OTHER; 41");
        Assert.Equal("PROG; OTHER; 41", e.LastLocation);
        e.ExitStatement(sv);
    }

    [Fact]
    public void EnterExit_IsSaveRestore_SoANestedActivationRestoresTheCaller()
    {
        // A CALL inside a checked statement runs callee statements that enter their OWN contexts; the emitted
        // finally restores the CALLER's context, so a post-return raise attributes the CALL statement.
        var e = Fresh();
        var outer = e.EnterStatement("CALL", "CALLER; MAIN; 10", ["EC-PROGRAM-NOT-FOUND"]);
        var inner = e.EnterStatement("MOVE", "CALLEE; P1; 99", ["EC-BOUND-SUBSCRIPT"]);
        e.ExitStatement(inner);
        e.Set("EC-PROGRAM-NOT-FOUND", fatal: true);
        Assert.Equal("CALL", e.LastStatement);
        Assert.Equal("CALLER; MAIN; 10", e.LastLocation);
        e.ExitStatement(outer);
        e.Set("EC-PROGRAM-NOT-FOUND", fatal: true);
        Assert.Null(e.LastStatement);   // fully restored — nothing leaks past the statement
    }

    [Fact]
    public void TheName_MatchesCaseInsensitively_ViaTheCanonicalUppercase()
    {
        // Set canonicalizes the raised name to uppercase before consulting the coverage set — the emitted
        // set is already canonical (it comes from the catalog), so a mixed-case raise still matches.
        var e = Fresh();
        var sv = e.EnterStatement("CONTINUE", "P; M; 5", ["EC-CONTINUE-LESS-THAN-ZERO"]);
        e.Set("ec-continue-less-than-zero", fatal: false);
        Assert.Equal("CONTINUE", e.LastStatement);
        e.ExitStatement(sv);
    }
}
