// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.Exceptions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ ONE ANSWER TO AN INTRINSIC ARGUMENT THAT BREAKS ITS FUNCTION'S ARGUMENT RULES, DECIDED BY THE ACTIVATION'S EDITION
/// (kb/Work PB6019).
///
/// <para><b>The rule.</b> At COBOL-85 a function reference whose arguments "do not have values that comply with the specified
/// constraints" has an undefined returned value (§1989:A.2.2 p3), and the Correction Amendment's clarification "allows an
/// implementation of amended third Standard COBOL to terminate processing" (§1985:p.B-46); from COBOL-2002 the breach is
/// EC-ARGUMENT-FUNCTION (§2002:15.2 AR13, §2014:15.3 AR13, §2023:15.3 AR14). <c>ExceptionEngine.ArgumentError</c> and its two
/// text twins are the one place that answer lives: at 85 every breach terminates the run unit, after it the condition is
/// raised when checking is enabled and the documented default returned when it is not.</para>
///
/// <para><b>What went wrong.</b> kb/Work PB5889 made an undecided integer argument that is not an integer terminate at 85, through
/// a flag the emitter compiled into each check, while every other value check (a negative FACTORIAL argument, a zero MOD divisor,
/// a date out of range) still returned 0 at 85 and the run went on: one rule, two answers. The edition is now the activation's
/// (<c>ICobolProgram.ArgumentBreachTerminates</c>, installed by <c>ProgramTable</c>), read only by the raise site, so no check
/// can carry an answer of its own; the structural test below fails a raise of EC-ARGUMENT-FUNCTION or a termination that
/// bypasses it.</para>
/// </summary>
public sealed class ArgumentBreachEditionTests
{
    [Fact]
    public void AtCobol85_EveryBreachTerminatesTheRunUnit_WithItsMessage()
    {
        var e = new ExceptionEngine { ArgumentBreachTerminates = true };
        Action[] breaches =
        [
            () => e.ArgumentError("FACTORIAL argument -4"),
            () => e.ArgumentErrorZeroLength("REVERSE argument"),
            () => e.ArgumentErrorSpaces("CHAR argument 0", 1),
        ];
        foreach (var breach in breaches)
        {
            var x = Assert.Throws<CobolImplementorFatalException>(breach);
            Assert.Contains("§1989:A.2.2 p3", x.Message);
            Assert.Contains("§1985:p.B-46", x.Message);
        }
        // Checking state is not the 1985 answer: a TURN cannot exist at 85, and the answer is the same with the flag set.
        e.ArgumentFunctionChecking = true;
        Assert.Throws<CobolImplementorFatalException>(() => e.ArgumentError("FACTORIAL argument -4"));
    }

    [Fact]
    public void From2002_AnUncheckedBreachReturnsTheDocumentedDefault()
    {
        var e = new ExceptionEngine();
        Assert.False(e.ArgumentBreachTerminates);
        Assert.Equal(0, e.ArgumentError("FACTORIAL argument -4"));
        Assert.Equal(ArgumentSubstitute.ZeroLength, e.ArgumentErrorZeroLength("REVERSE argument"));
        Assert.Equal(ArgumentSubstitute.Spaces(1), e.ArgumentErrorSpaces("CHAR argument 0", 1));
    }

    [Fact]
    public void From2002_ACheckedBreachRaisesEcArgumentFunction()
    {
        var e = new ExceptionEngine { ArgumentFunctionChecking = true };
        var x = Assert.Throws<CobolFatalException>(() => e.ArgumentError("FACTORIAL argument -4"));
        Assert.Equal("EC-ARGUMENT-FUNCTION", x.EcName);
    }

    /// <summary>Every runtime source outside the raise site: none reads the EC-ARGUMENT-FUNCTION checking flag, none raises the
    /// condition by name except straight after <c>ArgumentError</c> has given the edition its answer (the two bodies with no value
    /// to return at all, an argument list that is empty, end the reference loudly after it), and no intrinsic body terminates the
    /// run unit itself. A breach has one path, so a value check added tomorrow takes the edition's answer by calling
    /// <c>ArgumentError</c>, the only thing it can call. The exception catalog names the condition and raises nothing.</summary>
    [Fact]
    public void EveryBreach_TakesTheOnePath()
    {
        string catalog = Path.GetFullPath(TestRepo.Src("WiseOwl.Cobol.Runtime", "Exceptions", "ExceptionCatalog.cs"));
        string raiseSite = Path.GetFullPath(TestRepo.Src("WiseOwl.Cobol.Runtime", "Exceptions", "ExceptionState.cs"));
        string intrinsics = Path.GetFullPath(TestRepo.Src("WiseOwl.Cobol.Runtime", "Intrinsics")) + Path.DirectorySeparatorChar;
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Runtime"), "*.cs", SearchOption.AllDirectories))
        {
            string full = Path.GetFullPath(file);
            if (full.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || full.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || string.Equals(full, raiseSite, StringComparison.OrdinalIgnoreCase)
                || string.Equals(full, catalog, StringComparison.OrdinalIgnoreCase))
                continue;
            string[] lines = File.ReadAllLines(file);
            string previous = "";
            for (int i = 0; i < lines.Length; i++)
            {
                string t = lines[i].Trim();
                if (t.Length == 0 || t.StartsWith("//", StringComparison.Ordinal) || t.StartsWith("*", StringComparison.Ordinal)) continue;
                bool afterTheAnswer = previous.Contains("ExceptionState.ArgumentError(", StringComparison.Ordinal);
                previous = t;
                bool bypass = t.Contains("ArgumentFunctionChecking", StringComparison.Ordinal)
                    || (t.Contains("\"EC-ARGUMENT-FUNCTION\"", StringComparison.Ordinal) && !afterTheAnswer)
                    || (full.StartsWith(intrinsics, StringComparison.OrdinalIgnoreCase)
                        && t.Contains("new Exceptions.CobolImplementorFatalException", StringComparison.Ordinal))
                    || (full.StartsWith(intrinsics, StringComparison.OrdinalIgnoreCase)
                        && t.Contains("new CobolImplementorFatalException", StringComparison.Ordinal));
                if (bypass) offenders.Add($"{Path.GetRelativePath(TestRepo.Root, file)}:{i + 1}  {t}");
            }
        }
        Assert.True(offenders.Count == 0,
            $"{offenders.Count} runtime site(s) answer a broken intrinsic argument rule outside ExceptionEngine.ArgumentError:"
            + Environment.NewLine + string.Join(Environment.NewLine, offenders.Select(o => "    " + o))
            + Environment.NewLine + "Call ExceptionState.ArgumentError (or ArgumentErrorZeroLength / ArgumentErrorSpaces): it is the one "
            + "place the activation's edition decides the breach (terminate at COBOL-85, §1985:p.B-46; EC-ARGUMENT-FUNCTION from "
            + "2002, §2023:15.3 AR14). See kb/Work/PB6019.");
    }
}
