// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Runtime.CompilerServices;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ RUN-UNIT STATE LIVES ON <see cref="RunUnit"/>, NEVER IN A PROCESS-GLOBAL MUTABLE STATIC (kb/Work PB307).
/// </summary>
/// <remarks>
/// <para><see cref="RunUnit"/> is "the single owner of all run-unit-lifetime state (ISO §14.6.1)", replacing five
/// process-global stores — and FUNCTION RANDOM's sequence was a SIXTH the consolidation missed:
/// <c>private static Random _random</c> on <c>CobolIntrinsics</c>, whose own doc-comment said "ONE current
/// pseudo-random sequence per run unit". §15.75.3 r4 scopes the implementor seed to "the first reference to this
/// function in the run unit", so a second run unit in one .NET process (the <see cref="RunUnit.Run"/> /
/// <see cref="RunUnit.ResetCurrent"/> host shape) must NOT continue the first's sequence — and it did.</para>
/// <para>The second test is the structure that makes the next case automatic: it enumerates EVERY writable static
/// field the runtime assembly declares and fails on one that is not a documented PROCESS-lifetime store. A new
/// run-unit store written as a static cannot pass without someone writing, here, why it outlives the run unit.
/// (A <c>static readonly</c> reference to a mutable collection is not caught — the runtime's are immutable
/// tables or caches keyed by immutable inputs.)</para>
/// </remarks>
public sealed class RunUnitStateDriftTests
{
    /// <summary>The note's witness. It needs no knowledge of the implementor seed: it is an INEQUALITY against a
    /// value §15.75.4 r2 makes deterministic (draw 4 of the seed-7 sequence).</summary>
    [Fact]
    public void Random_DoesNotContinueAcrossARunUnitReset()
    {
        double draw4 = 0, afterReset = 0;
        RunUnit.Run(_ =>
        {
            CobolIntrinsics.Random((Int128)7);
            CobolIntrinsics.Random();
            CobolIntrinsics.Random();
            draw4 = new Random(7).Skip(3);
            RunUnit.ResetCurrent();            // cross the run-unit boundary (the emitted driver's ProgramRegistry.Reset)
            afterReset = CobolIntrinsics.Random();
        });
        Assert.NotEqual(draw4, afterReset);
    }

    /// <summary>The same through the host lifecycle boundary: a fresh <see cref="RunUnit.Run"/> scope begins its
    /// own sequence, while INSIDE one run unit §15.75.3 r5 continues the current sequence and r3 restarts it.</summary>
    [Fact]
    public void Random_IsScopedToTheRunUnit_AndContinuesWithinIt()
    {
        double[] first = new double[3];
        double secondRunUnit = 0;
        RunUnit.Run(_ =>
        {
            first[0] = CobolIntrinsics.Random((Int128)7);
            first[1] = CobolIntrinsics.Random();
            first[2] = CobolIntrinsics.Random((Int128)7);   // r3: a new sequence from the same seed
        });
        RunUnit.Run(_ => secondRunUnit = CobolIntrinsics.Random());

        var reference = new Random(7);
        Assert.Equal(reference.NextDouble(), first[0]);
        Assert.Equal(reference.NextDouble(), first[1]);     // r5: the NEXT number of the current sequence
        Assert.Equal(first[0], first[2]);                   // r3 + §15.75.4 r2: same seed, same sequence
        Assert.NotEqual(new Random(7).Skip(2), secondRunUnit);
    }

    /// <summary>Every writable static field in <c>Cobol.Net.Runtime</c>, with the reason it is PROCESS state.
    /// Adding an entry is a claim that the state must survive a run-unit boundary — write the reason.</summary>
    private static readonly Dictionary<string, string> ProcessLifetimeStatics = new(StringComparer.Ordinal)
    {
        ["CobolNet.Runtime.IO.StandardStreams::_applied"] =
            "the console encoding is a property of the PROCESS's standard streams, applied once",
        ["CobolNet.Runtime.Collation.CollationRuntime::s_initialized"] =
            "idempotence latch for the process-wide collation subsystem initialization",
        ["CobolNet.Runtime.Collation.CollationRuntime::s_hostConfigured"] =
            "records that the HOST configured the process-wide collation caches",
        ["CobolNet.Runtime.Collation.CollationRuntime::s_lastWarmup"] =
            "diagnostic status of the process-wide collation warm-up",
        ["CobolNet.Runtime.Collation.Cache.CollationKeyCache::s_defaultConfig"] =
            "process-wide cache sizing for collation keys derived from immutable collators",
        ["CobolNet.Runtime.CobolTable+Scratch`1::Slot"] =
            "the out-of-range reference scratch cell; overwritten before EVERY use, so it carries nothing between uses",
        ["CobolNet.Runtime.PointerImage::s_nextBase"] =
            "the pointer-image base allocator (kb/Work PB970 arm 2, DOC-A.1-216): its bases key the process-wide "
            + "area and name tables beside it, so it must count per PROCESS — a per-run-unit restart would hand a "
            + "second run unit bases the tables already hold for other areas, and two distinct pointers would image equal",
    };

    [Fact]
    public void NoWritableStatic_OutsideTheDocumentedProcessStores()
    {
        var offenders = new List<string>();
        foreach (Type t in typeof(RunUnit).Assembly.GetTypes())
        {
            if (IsCompilerGenerated(t)) continue;   // lambda / method-group caches: immutable delegates
            foreach (FieldInfo f in t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                                                 | BindingFlags.DeclaredOnly))
            {
                if (f.IsInitOnly || f.IsLiteral || f.IsDefined(typeof(CompilerGeneratedAttribute))
                    && !f.Name.Contains("BackingField", StringComparison.Ordinal)) continue;
                string key = $"{t.FullName}::{f.Name}";
                if (!ProcessLifetimeStatics.ContainsKey(key)) offenders.Add(key);
            }
        }
        Assert.True(offenders.Count == 0,
            "writable static field(s) in Cobol.Net.Runtime — run-unit state belongs on RunUnit (ISO §14.6.1; "
            + "kb/Work PB307); a genuinely process-lifetime store is documented in ProcessLifetimeStatics:\n  "
            + string.Join("\n  ", offenders));

        // The register must not rot: every documented entry still exists.
        var live = typeof(RunUnit).Assembly.GetTypes()
            .SelectMany(t => t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                                         | BindingFlags.DeclaredOnly).Select(f => $"{t.FullName}::{f.Name}"))
            .ToHashSet(StringComparer.Ordinal);
        var stale = ProcessLifetimeStatics.Keys.Where(k => !live.Contains(k)).ToList();
        Assert.True(stale.Count == 0, "ProcessLifetimeStatics names field(s) that no longer exist: " + string.Join(", ", stale));
    }

    private static bool IsCompilerGenerated(Type t)
    {
        for (Type? x = t; x is not null; x = x.DeclaringType)
            if (x.IsDefined(typeof(CompilerGeneratedAttribute))) return true;
        return false;
    }
}

file static class RandomDraws
{
    /// <summary>The value the <paramref name="n"/>+1-th draw of <paramref name="r"/> returns.</summary>
    public static double Skip(this Random r, int n)
    {
        for (int i = 0; i < n; i++) r.NextDouble();
        return r.NextDouble();
    }
}
