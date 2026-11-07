// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ ONE TYPE DECLARATION OF A RESTRICTED PROGRAM-POINTER IS ONE TYPE IN EVERY COMPILATION THAT DECLARES IT (kb/Work
/// PB4107, branch review R1-1). Two separately compiled modules each declare <c>TB TYPEDEF USAGE PROGRAM-POINTER TO
/// P4107X</c> and a strong group <c>TG</c> holding one <c>TB</c>; one module holds P4107X's prototype, the other names it in
/// its REPOSITORY paragraph only, so its details are "taken from the external repository" (ISO §2023:12.3.8.4 GR10 c), cite.py
/// OK 12.3.8.4 10) c)) — the prototype's own module, P4107X.dll, compiled first (DESIGN-external-repository §8.2 step 3,
/// kb/Work PB2103). A BY REFERENCE argument of type TB is "of the same type" as a formal of type TB (§2023:14.8.2.3.2, OK
/// 14.8.2.3.2 4): "If either is a restricted pointer, both shall be restricted and of the same type"), and one of type TG
/// as a formal of type TG (§2023:14.8.2.2, OK 14.8.2.2 2)), because §2023:8.5.3.1 (OK 8.5.3.1) makes two type declarations
/// equivalent when "they have the same type-name" and the same USAGE clauses, whose TO phrase names the prototype
/// (docs/CONFORMANCE.md D-RP1).
/// <para>The run-time check (§2023:14.9.4.4 GR3 d), OK 14.9.4.4 3) d)) compares the two modules' §2023:8.5.3.1 keys
/// (<c>StrongTypeModel.TypeIdentityKey</c>), so the key must be one text whether the prototype's signature came from the
/// group or from the repository's record of it. The corpus cannot carry this case: §2023:10.6.2 SR1 puts every prototype of a
/// compilation group ahead of its other units, so one group never holds both a group-resolving and a repository-resolving
/// declaration.</para>
/// </summary>
public sealed class TypedPointerAcrossCompilationsTests
{
    private const string Types = """
               01 TB TYPEDEF USAGE PROGRAM-POINTER TO P4107X.
               01 TG TYPEDEF STRONG.
                  05 M TYPE TB.
        """;

    private const string Prototype = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. P4107X IS PROTOTYPE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 L-Y PIC 9(4).
               PROCEDURE DIVISION USING L-Y.
               END PROGRAM P4107X.

        """;

    private static string Callee(string id) => $"""
               >>TURN EC-ALL CHECKING ON
               IDENTIFICATION DIVISION.
               PROGRAM-ID. {id}.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               REPOSITORY.
                   PROGRAM P4107X.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
        {Types}
               LINKAGE SECTION.
               01 F TYPE TB.
               01 G TYPE TG.
               PROCEDURE DIVISION USING F G.
                   DISPLAY "IN {id}"
                   GOBACK.
               END PROGRAM {id}.
        """;

    private static string Caller(string id, string callee, string? then) => $"""
               >>TURN EC-ALL CHECKING ON
               IDENTIFICATION DIVISION.
               PROGRAM-ID. {id}.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               REPOSITORY.
                   PROGRAM P4107X
                   PROGRAM {callee}.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
        {Types}
               01 PB TYPE TB.
               01 GB TYPE TG.
               PROCEDURE DIVISION.
                   SET PB TO NULL
                   SET M OF GB TO NULL
                   CALL {callee} USING BY REFERENCE PB GB
                       ON EXCEPTION DISPLAY "{id} EXC " FUNCTION EXCEPTION-STATUS
                       NOT ON EXCEPTION DISPLAY "{id} OK"
                   END-CALL
                   {(then is null ? "" : $"CALL \"{then}\"")}
                   GOBACK.
               END PROGRAM {id}.
        """;

    [Fact]
    public void ATypedRestrictedPointer_IsOfOneType_WhetherOrNotItsCompilationHoldsThePrototypesSignature()
    {
        string dir = ScratchDirectory.Create("pb4107xc");
        try
        {
            // The prototype's own module, which answers the repository-resolving compilations (GR10 c)).
            NonCobolElement.CompileCobol(dir, "P4107X", Prototype, dialect: 2002);
            // Through the repository: the callee S4107U. With the prototype in the group: the callee S4107K.
            NonCobolElement.CompileCobol(dir, "S4107U", Callee("S4107U"), dialect: 2002);
            NonCobolElement.CompileCobol(dir, "S4107K", Prototype + Callee("S4107K"), dialect: 2002);
            // Through the repository: the caller T4107U of the group-resolving callee; and the main program T4107K, which
            // holds the prototype and calls the repository-resolving callee, then the repository-resolving caller.
            NonCobolElement.CompileCobol(dir, "T4107U", Caller("T4107U", "S4107K", then: null), dialect: 2002);
            NonCobolElement.CompileCobol(dir, "T4107K", Prototype + Caller("T4107K", "S4107U", then: "T4107U"), dialect: 2002);
            var (exit, stdout, detail) = CutRunner.RunExit(Path.Combine(dir, "T4107K.dll"), dir);
            Assert.True(exit == 0, detail);
            Assert.Equal("IN S4107U\nT4107K OK\nIN S4107K\nT4107U OK", stdout);
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
