// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ A BY VALUE CLASS REFUSAL NAMES THE CLASS §8.5.2.1 GIVES A STRONGLY-TYPED GROUP — ITS TYPE-NAME (kb/Work PB3192
/// review R1-1). ISO §8.5.2.1: "Both the class and the category of a strongly-typed group item are the type-name
/// specified in the TYPE clause". The refusal of a strongly-typed group passed or received BY VALUE is right (§14.2.2
/// SR2 admits a formal of class numeric, message-tag, object or pointer; §14.9.4.3 SR22 and §8.4.3.2.3 SR10 an argument
/// of class numeric, object or pointer; §14.9.23.3 SR15 an INVOKE identifier-5 "of class message-tag, numeric, object or
/// pointer", through a typed receiver and through the predefined NULL alike, kb/Work PB1136); the reason it
/// gave was not: each screen spelled <c>IntrinsicArgumentRules.Name</c> over <c>ClassOfItem</c> / <c>ClassOf</c>, which
/// answer "alphanumeric" for a strongly-typed group on §15.3 item 2's FUNCTION-ARGUMENT reading, so the user was told
/// the formal or argument was "of class alphanumeric". Each screen now asks <c>IntrinsicArgumentRules.ClassNameOf</c>
/// (the kb/Work PB1400 spelling). TYPEDEF STRONG is COBOL-2002, and the text is edition-invariant from there; the
/// header half's 2002/2014/2023 refusals are also the corpus negatives <c>pb3222-by-value-formal-*</c>.
/// </summary>
public sealed class StrongGroupClassNameTests
{
    private const string Reason = "is of class ST, the type-name of a strongly-typed group item";

    private const string HeaderFormal = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SGCNHDR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ST TYPEDEF STRONG.
          05 F PIC 9.
       LINKAGE SECTION.
       01 G TYPE ST.
       PROCEDURE DIVISION USING BY VALUE G.
           GOBACK.
       END PROGRAM SGCNHDR.
""";

    private const string CallArgument = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SGCNCALL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ST TYPEDEF STRONG GLOBAL.
          05 F PIC 9.
       01 G TYPE ST.
       PROCEDURE DIVISION.
       MAIN.
           CALL "SGCNSUB" AS NESTED USING BY VALUE G
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SGCNSUB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LV PIC 9(4) USAGE BINARY.
       PROCEDURE DIVISION USING BY VALUE LV.
       P.
           GOBACK.
       END PROGRAM SGCNSUB.
       END PROGRAM SGCNCALL.
""";

    private const string FunctionArgument = """
       IDENTIFICATION DIVISION.
       FUNCTION-ID. SGCNFN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC 9.
       01 R PIC 9.
       PROCEDURE DIVISION USING BY VALUE P RETURNING R.
           MOVE P TO R.
           GOBACK.
       END FUNCTION SGCNFN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SGCNMAIN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. FUNCTION SGCNFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ST TYPEDEF STRONG.
          05 F PIC 9.
       01 G TYPE ST.
       01 X PIC 9.
       PROCEDURE DIVISION.
           MOVE FUNCTION SGCNFN(G) TO X.
           STOP RUN.
       END PROGRAM SGCNMAIN.
""";

    private const string InvokeTypedArgument = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SGCNINV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS SGCNCLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ST TYPEDEF STRONG.
          05 F PIC 9.
       01 G TYPE ST.
       01 O USAGE OBJECT REFERENCE SGCNCLS.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE SGCNCLS "NEW" RETURNING O.
           INVOKE O "M" USING BY VALUE G.
           STOP RUN.
       END PROGRAM SGCNINV.

       IDENTIFICATION DIVISION.
       CLASS-ID. SGCNCLS INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN PIC S9(4) COMP-5.
       PROCEDURE DIVISION USING BY VALUE LN.
           CONTINUE.
       END METHOD M.
       END OBJECT.
       END CLASS SGCNCLS.
""";

    private const string InvokeNullArgument = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SGCNNUL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ST TYPEDEF STRONG.
          05 F PIC 9.
       01 G TYPE ST.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE NULL "PING" USING BY VALUE G.
           STOP RUN.
       END PROGRAM SGCNNUL.
""";

    private static readonly Dictionary<string, (string Source, string Code)> Screens = new()
    {
        ["header-formal"] = (HeaderFormal, "COBOL1553"),          // §14.2.2 SR2
        ["call-argument"] = (CallArgument, "COBOL1628"),          // §14.9.4.3 SR22
        ["function-argument"] = (FunctionArgument, "COBOL1554"),  // §8.4.3.2.3 SR10
        ["invoke-typed-argument"] = (InvokeTypedArgument, "COBOL2741"),  // §14.9.23.3 SR15, the typed lane
        ["invoke-null-argument"] = (InvokeNullArgument, "COBOL2741"),    // §14.9.23.3 SR15, the NULL receiver's screen
    };

    public static TheoryData<int, string> Cases()
    {
        var data = new TheoryData<int, string>();
        foreach (int edition in (int[])[2002, 2014, 2023])
            foreach (string screen in Screens.Keys)
                data.Add(edition, screen);
        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TheByValueRefusal_NamesTheTypeNameAsTheClass(int edition, string screen)
    {
        var (source, code) = Screens[screen];
        var (ok, errors, _) = EditionHarness.CompileFull(source, edition);
        Assert.False(ok, $"[std {edition}] a strongly-typed group BY VALUE must be refused ({code})");
        Assert.Contains(errors, e => e.Contains(code, StringComparison.Ordinal) && e.Contains(Reason, StringComparison.Ordinal));
        Assert.DoesNotContain(errors, e => e.Contains(code, StringComparison.Ordinal) && e.Contains("class alphanumeric", StringComparison.Ordinal));
    }
}
