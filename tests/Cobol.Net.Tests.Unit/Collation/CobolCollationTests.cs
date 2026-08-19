// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit.Collation;

/// <summary>
/// The ONE collating-sequence carrier <see cref="CobolCollation"/> (DESIGN-locale-facility §4.4; kb/Work PB101):
/// the LOCALE arm's ISO §8.8.4.2.11 operand rule and engine comparison, its L7 materialized positions (ORD/CHAR,
/// HIGH-/LOW-VALUE), the EC-LOCALE-INCOMPATIBLE determination (L6), the base class's single ThruMember, the table
/// arms' unchanged answers through the carrier, and the DRIFT test that the overload set stays collapsed — no
/// consumer may grow a raw-<c>ushort[]</c> or <c>NationalCollation</c>-typed collating slot again.
/// </summary>
public sealed class CobolCollationTests
{
    private static readonly LocaleCollation Loc = LocaleCollation.Current;

    // ---- the LOCALE arm ----------------------------------------------------------------------------------------

    [Fact]
    public void LocaleArm_ComparesByTheEngine_NotByCodeUnit()
    {
        Assert.True(Loc.Compare("apple", "Zebra") < 0);        // native would say Zebra (0x5A) < apple (0x61)
        Assert.True(Loc.Compare("zebra", "Zebra") < 0);        // lowercase first (tertiary)
        Assert.True(Loc.Compare("resume", "resumE") < 0);
        Assert.True(Loc.Compare("abc", "abd") < 0);
        Assert.True(Loc.Compare("abd", "abc") > 0);
        Assert.Equal(0, Loc.Compare("abc", "abc"));
        Assert.Equal(0, CobolString.Compare("abc", "abc", Loc));   // through the carrier-taking entry
        Assert.True(CobolString.Compare("apple", "Zebra", Loc) < 0);
    }

    /// <summary>§8.8.4.2.11 sentence 1: trailing spaces are truncated; an all-space operand becomes ONE space;
    /// two zero-length operands are equal. No padding — unlike every table arm.</summary>
    [Fact]
    public void LocaleArm_TruncatesTrailingSpaces_AllSpacesToOne()
    {
        Assert.Equal(0, Loc.Compare("abc", "abc   "));
        Assert.Equal(0, Loc.Compare("abc  ", "abc"));
        Assert.Equal(0, Loc.Compare("     ", " "));
        Assert.Equal(0, Loc.Compare("", ""));
        Assert.Equal(0, Loc.Compare(null, ""));
        Assert.True(Loc.Compare("", " ") < 0);                 // "" trims to ""; " " (all spaces) trims to ONE space, not ""
        Assert.True(Loc.Compare("a", "") > 0);
        Assert.Equal("", LocaleCollation.TrimForLocale(""));
        Assert.Equal(" ", LocaleCollation.TrimForLocale("    "));
        Assert.Equal("a  b", LocaleCollation.TrimForLocale("a  b   "));
        Assert.Same("ab", LocaleCollation.TrimForLocale("ab"));   // no allocation when nothing trims
    }

    /// <summary>DETERMINATION L6: the derived table orders every well-formed text, so the only operand "the locale
    /// does not define a collating sequence for" is ill-formed UTF-16 — an unpaired surrogate raises the fatal
    /// EC-LOCALE-INCOMPATIBLE and the comparison still answers deterministically.</summary>
    [Fact]
    public void LocaleArm_UnpairedSurrogate_SetsEcLocaleIncompatible()
    {
        // Since kb/Work PB64 T1 the raise is CHECKING-GATED like every EC-LOCALE condition (§14.6.13.1.1: "if checking
        // for an exception that occurs is not enabled, no exception condition is raised"): nothing is recorded with
        // checking off and the comparison still answers a deterministic order; with checking on the statement guard's
        // flag makes it a fatal CobolFatalException the USE declarative can observe (LocaleStateTests pins the flag path).
        RunUnit.Run(_ =>
        {
            ExceptionState.Clear();
            int c = Loc.Compare("a\uD800", "a");
            Assert.Null(ExceptionState.LastName);                 // checking off: no condition (§14.6.13.1.1)
            Assert.Equal(c, Loc.Compare("a\uD800", "a"));        // deterministic
            ExceptionState.LocaleIncompatibleChecking = true;
            try
            {
                var ex = Assert.Throws<CobolFatalException>(() => Loc.Compare("a\uD800", "a"));
                Assert.Equal("EC-LOCALE-INCOMPATIBLE", ex.EcName);
                Assert.Equal("EC-LOCALE-INCOMPATIBLE", ExceptionState.LastName);
                Assert.True(ExceptionState.LastFatal);
                ExceptionState.Clear();
                Loc.Compare("a\U0001F600", "a");                // a well-formed supplementary character: no condition
                Assert.Null(ExceptionState.LastName);
            }
            finally { ExceptionState.LocaleIncompatibleChecking = false; }
        });
    }

    /// <summary>DETERMINATION L7: positions materialized from the engine — ORD/CHAR round-trip, equal-collating code
    /// units share a position (CHAR returns the lowest-coded member), HIGH-VALUE is U+FFFF, LOW-VALUE U+0000.</summary>
    [Fact]
    public void LocaleArm_MaterializedPositions_RoundTrip_AndExtremes()
    {
        Assert.True(Loc.PositionCount > 1000 && Loc.PositionCount <= 0x10000);
        Assert.True(Loc.Weight('a') < Loc.Weight('A'));
        Assert.True(Loc.Weight('A') < Loc.Weight('b'));
        Assert.True(Loc.Weight('1') < Loc.Weight('a'));
        foreach (char c in "aAzZ09 é€中")
            Assert.Equal(c, (char)Loc.CharAt(Loc.Weight(c)));   // each of these is the lowest code unit of its position
        Assert.Equal(0, Loc.Weight('\0'));                        // completely ignorable — the first position
        Assert.Equal(Loc.Weight('\0'), Loc.Weight('\U00000001'));     // shared with every other completely ignorable unit
        Assert.Equal('\0', (char)Loc.CharAt(0));
        Assert.Equal(-1, Loc.CharAt(Loc.PositionCount));
        Assert.Equal(-1, Loc.CharAt(-1));
        Assert.Equal('\U0000FFFF', Loc.HighValue);
        Assert.Equal('\0', Loc.LowValue);
        Assert.Equal(Loc.PositionCount - 1, Loc.Weight('\U0000FFFF'));
        // The intrinsic bodies read the carrier.
        Assert.Equal(Loc.Weight('Q') + 1L, CobolIntrinsics.Ord("Q", Loc));
        Assert.Equal("Q", CobolIntrinsics.Char(Loc.Weight('Q') + 1, Loc));
        Assert.Equal("Zebra", CobolIntrinsics.MaxString(Loc, "Zebra", "apple", "zebra"));
        Assert.Equal("apple", CobolIntrinsics.MinString(Loc, "Zebra", "apple", "zebra"));
    }

    [Fact]
    public void LocaleArm_NamedLocale_UsesItsTailoring_CurrentFollowsTheRunUnit()
    {
        var es = new LocaleCollation("es-ES");
        Assert.Equal("es-ES", es.LocaleTag);
        Assert.True(es.Compare("ñu", "nz") > 0);                 // Spanish: ñ after n
        Assert.True(new LocaleCollation("root").Compare("ñu", "nz") < 0);
        Assert.Null(LocaleCollation.Current.LocaleTag);
        RunUnit.Run(ru =>
        {
            ru.Locale.Set(LocaleCategory.Collate, "es-ES");    // the seam SET LOCALE lands on
            Assert.True(LocaleCollation.Current.Compare("ñu", "nz") > 0);
            Assert.Equal("es-ES", LocaleCollation.Current.Resolve().Table.Name);
            ru.Locale.Set(LocaleCategory.Collate, null);
            Assert.Equal(ru.Locale.UserDefault.Collate, ru.Locale.Current(LocaleCategory.Collate));
        });
    }

    /// <summary>DETERMINATION L2 (owner decision Q2): the environment variable wins; else the culture; "" is root.</summary>
    [Fact]
    public void LocaleState_DeterminesTheDefaultsFromTheEnvironment()
    {
        Assert.Equal("es-MX", Probe("COBOLNET_TEST_LOCALE_PROBE_A", " es-MX ", () => System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal("", Probe("COBOLNET_TEST_LOCALE_PROBE_B", " ", () => System.Globalization.CultureInfo.GetCultureInfo("fr-FR")));   // a blank value is the ROOT (Windows deletes an empty variable, so a space stands in)
        Assert.Equal("fr-FR", Probe("COBOLNET_TEST_LOCALE_PROBE_C", null, () => System.Globalization.CultureInfo.GetCultureInfo("fr-FR")));
        Assert.Equal("", Probe("COBOLNET_TEST_LOCALE_PROBE_D", null, () => throw new InvalidOperationException("no culture data")));

        static string Probe(string variable, string? value, Func<System.Globalization.CultureInfo> fallback)
        {
            Environment.SetEnvironmentVariable(variable, value);
            try { return LocaleState.Determine(variable, fallback); }
            finally { Environment.SetEnvironmentVariable(variable, null); }
        }
    }

    // ---- the base class and the table arms ---------------------------------------------------------------------

    /// <summary>ISO §14.7.8: ONE ThruMember on the base class serves every arm — the inclusive test, and lo > hi
    /// sets the nonfatal EC-RANGE-INVALID and answers false.</summary>
    [Fact]
    public void ThruMember_IsOneImplementationOnTheBase()
    {
        RunUnit.Run(_ =>
        {
            Assert.True(Loc.ThruMember("m", "a", "z"));
            Assert.True(Loc.ThruMember("a", "a", "z"));
            Assert.True(Loc.ThruMember("z", "a", "z"));
            Assert.False(Loc.ThruMember("Z", "a", "y"));         // Z sorts after y under the locale order
            ExceptionState.Clear();
            Assert.False(Loc.ThruMember("m", "z", "a"));         // lo collates after hi
            Assert.Equal("EC-RANGE-INVALID", ExceptionState.LastName);
            Assert.False(ExceptionState.LastFatal);
            Assert.True(CobolString.ThruMember("m", "a", "z", Loc));
        });
        var reversed = new AlphanumericCollation(ReversedPositions(), ReversedRep(), 256, 'A', 'Z');
        Assert.True(reversed.ThruMember("M", "Z", "A"));          // under the reversed alphabet Z < A
        Assert.False(reversed.ThruMember("M", "A", "Z"));         // …so A..Z is an inverted (empty) range
    }

    [Fact]
    public void TableArms_AnswerThroughTheCarrier_AsBefore()
    {
        var reversed = new AlphanumericCollation(ReversedPositions(), ReversedRep(), 256, 'A', 'Z');
        CobolCollation c = reversed;
        Assert.True(c.Compare("A", "B") > 0);                     // reversed A..Z
        Assert.True(c.Compare("B", "A") < 0);
        Assert.Equal(0, c.Compare("A", "A   "));                  // the table arm space-extends
        Assert.True(c.Compare("A", "AĀ") < 0);               // above-block code unit keeps its native code — order-equivalent tail
        Assert.Equal('A', c.HighValue);
        Assert.Equal('Z', c.LowValue);
        Assert.Equal(25, c.Weight('A'));
        Assert.Equal('A', (char)c.CharAt(25));
        var nat = new NationalCollation([(ushort)'A', (ushort)'B', (ushort)'C'], [2, 1, 0], [(ushort)'C', (ushort)'B', (ushort)'A'], 3, '\U0000FFFF', 'C');
        CobolCollation n = nat;
        Assert.True(n.Compare("C", "A") < 0);
        Assert.Equal(0, n.Compare("C", "C  "));
        Assert.Equal('C', n.LowValue);
        Assert.Equal("CBA", CobolIntrinsics.MinString(n, "ABC", "CBA"));
    }

    /// <summary>The overload set stays COLLAPSED (DESIGN-locale-facility §4.4.1's drift test): the comparison entries
    /// take a <c>char</c> pad (native) or the ONE <see cref="CobolCollation"/> — never a raw <c>ushort[]</c> or a
    /// concrete arm — and every collating slot of the sort / file / MAX-MIN / CHAR-ORD surface is carrier-typed.</summary>
    [Fact]
    public void Drift_TheCarrierIsTheOnlyCollatingParameterType()
    {
        static IEnumerable<MethodInfo> Overloads(Type t, string name) =>
            t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).Where(m => m.Name == name);

        foreach (string name in new[] { nameof(CobolString.Compare), nameof(CobolString.ThruMember) })
        {
            var lastParams = Overloads(typeof(CobolString), name).Select(m => m.GetParameters()[^1].ParameterType).ToList();
            Assert.Equal(2, lastParams.Count);
            Assert.Contains(typeof(char), lastParams);
            Assert.Contains(typeof(CobolCollation), lastParams);
        }
        // Every collating parameter anywhere on the runtime's public surface is the carrier, never a raw table.
        var offenders = new List<string>();
        foreach (var t in new[] { typeof(CobolString), typeof(CobolSort), typeof(CobolFile), typeof(FileRegistry), typeof(CobolIntrinsics) })
            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                foreach (var p in m.GetParameters())
                    if (p.ParameterType == typeof(ushort[]) || p.ParameterType == typeof(AlphanumericCollation) || p.ParameterType == typeof(NationalCollation))
                        offenders.Add($"{t.Name}.{m.Name}({p.Name}: {p.ParameterType.Name})");
        Assert.True(offenders.Count == 0, "collating parameters that bypass CobolCollation:\n" + string.Join("\n", offenders));
        Assert.True(typeof(CobolCollation).IsAbstract);
        Assert.True(typeof(AlphanumericCollation).IsSubclassOf(typeof(CobolCollation)));
        Assert.True(typeof(NationalCollation).IsSubclassOf(typeof(CobolCollation)));
        Assert.True(typeof(LocaleCollation).IsSubclassOf(typeof(CobolCollation)));
    }

    private static ushort[] ReversedPositions()
    {
        // ALPHABET IS "ZYX…A": Z=0 … A=25, every other Latin-1 code unit follows in native order (§12.3.7.4 GR7 1.3).
        var pos = new ushort[256];
        Array.Fill(pos, ushort.MaxValue);
        ushort next = 0;
        for (char c = 'Z'; c >= 'A'; c--) pos[c] = next++;
        for (int code = 0; code < 256; code++) if (pos[code] == ushort.MaxValue) pos[code] = next++;
        return pos;
    }

    private static ushort[] ReversedRep()
    {
        var pos = ReversedPositions();
        var rep = new ushort[256];
        for (int code = 0; code < 256; code++) rep[pos[code]] = (ushort)code;
        return rep;
    }
}
