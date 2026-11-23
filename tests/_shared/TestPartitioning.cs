// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>
/// THE mechanism for running one theory-heavy test class across SEVERAL xUnit collections — one rule, one place.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ WHY THIS EXISTS (plan §11 A13, measured). xUnit <b>2.9.2 parallelizes at TEST-COLLECTION granularity, and by
/// default each test CLASS is one collection</b> — so every test in a class, including every row of a
/// <c>[Theory]</c>, runs <b>SERIALLY ON ONE THREAD</b>. A single fat class therefore caps an entire assembly's wall
/// clock while the other cores idle, and nothing in the normal output says so. Measured on the battery #41 trx
/// (2026-09-01, 32-core box, <c>scripts/profile-test-parallelism.py</c>): the Conformance assembly ran 5,241 tests
/// in <b>721 s wall for 1,948 s of test time — 2.7x average concurrency</b>, of which
/// <c>VersionMatrixTests</c> alone was <b>2,127 tests running serially for 720.5 s</b>, i.e. essentially the WHOLE
/// leg was one class on one thread; the Unit assembly was <b>171 s wall / 329 s test time — 1.9x</b>, and one class
/// (<c>StorageFormEquivalenceTests</c>' NIST sweep, 171.5 s) <i>was</i> that wall clock.
/// </para>
/// <para>
/// ⭐ THE SHAPE, and why it is this one. xUnit v2 offers exactly two levers — genuine class splits, or
/// <c>[Collection]</c> attributes — and <c>[Collection]</c> only ever <i>merges</i> classes into a shared
/// collection, so it cannot split one. That leaves class splits, and the naive form of a class split DUPLICATES
/// THE TEST BODIES, which is how a split silently rots. So: the tests live ONCE in an <b>abstract generic base</b>
/// <c>Family&lt;TSlot&gt;</c>, its <c>[MemberData]</c> sources slice the family's full row set with
/// <see cref="SliceRows{TSlot}"/>, and each partition is a <b>one-line</b> concrete class
/// <c>sealed class Family_P3 : FamilyBase&lt;Slot3&gt;;</c>. Every concrete class is its own default collection, so
/// the partitions run concurrently.
/// </para>
/// <para>
/// This works because <b>static members of a CLOSED generic type are per-type-argument</b>, and xUnit resolves
/// <c>[MemberData]</c> against <c>testMethod.DeclaringType</c> — which for a method inherited from
/// <c>FamilyBase&lt;Slot3&gt;</c> is the <i>closed</i> <c>FamilyBase&lt;Slot3&gt;</c>, not the open definition. That
/// is not assumed: it was proved on xunit 2.9.2 / xunit.runner.visualstudio 2.8.2 with a standalone probe (3
/// partitions over 9 rows produced 9 tests, 3 per partition, each asserting its OWN slot — an open-type resolution
/// would have produced 27 tests and 18 failures).
/// </para>
/// <para>
/// ⚠ THE INVARIANT THAT MATTERS is that the partitions cover the family's row set <b>exactly once</b> — a family
/// whose partition classes go stale (one deleted, one duplicated, the count bumped without a new class) drops rows
/// SILENTLY and stays green. <see cref="TestPartitionAudit"/> is the drift gate that makes that impossible, and it
/// is generic: it discovers every family in an assembly by shape, so a NEW partitioned family is covered the
/// moment it is written, with no registration step.
/// </para>
/// </remarks>
internal static class TestPartitioning
{
    /// <summary>Slot <paramref name="index"/>'s share of <paramref name="rows"/> under a stride of
    /// <paramref name="partitions"/> — row <c>i</c> belongs to slot <c>i % partitions</c>, so the slots are
    /// disjoint and their union is the whole set by construction.</summary>
    /// <remarks>A stride (not a contiguous block) is deliberate: theory rows are usually ordered by construct or
    /// program name, and adjacent rows have correlated cost, so a stride balances the partitions where a block
    /// would concentrate the expensive rows in one of them.</remarks>
    internal static IEnumerable<T> Slice<T>(IEnumerable<T> rows, int index, int partitions)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentOutOfRangeException.ThrowIfLessThan(partitions, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, partitions);
        return rows.Where((_, i) => i % partitions == index);
    }

    /// <summary>The <typeparamref name="TSlot"/> partition's share of a theory's rows. This is the ONE call a
    /// partitioned family's <c>[MemberData]</c> source makes.</summary>
    /// <remarks>The row type is fixed to <c>object[]</c> — xUnit's data-source shape — rather than generic,
    /// because C# takes explicit type arguments all-or-nothing: a <c>Slice&lt;TSlot, T&gt;</c> could not be called
    /// as <c>Slice&lt;Slot3&gt;(rows, 12)</c>, and spelling out both at every call site is exactly the kind of
    /// noise that makes a family adopt something else instead. It also means the drift audit has ONE row shape to
    /// key on, so a family that slices something else (a list of program paths, say) still routes through here and
    /// is still covered.</remarks>
    internal static IEnumerable<object[]> SliceRows<TSlot>(IEnumerable<object[]> rows, int partitions)
        where TSlot : ITestPartitionSlot
        => Slice(rows, TSlot.Index, partitions);
}

/// <summary>
/// Marks the UNPARTITIONED row source of a partitioned family, naming the sliced member the family's theories
/// actually consume. <see cref="TestPartitionAudit"/> reads both and proves the partitions reconstruct the source
/// exactly once — the whole point of the mechanism, and the one thing a class split can silently lose.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal sealed class PartitionedRowSourceAttribute(string slicedMember) : Attribute
{
    /// <summary>Name of the sliced member (the one carrying <c>[MemberData]</c>) fed from this source.</summary>
    public string SlicedMember { get; } = slicedMember;
}
