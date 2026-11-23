// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Reflection;

namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>
/// The DRIFT GATE for <see cref="TestPartitioning"/>: proves, per assembly and by shape alone, that every
/// partitioned family's concrete partitions cover its row set exactly once, each partition its even share.
/// </summary>
/// <remarks>
/// ⛔ A partitioned family fails OPEN if nothing checks it: delete one partition class and the rows it owned are
/// simply never run, with a green leg and a smaller — but still plausible — test count. Every check below exists
/// for one such way to lose rows silently, or to rebuild the serial pole the partitions exist to remove:
/// <list type="bullet">
///   <item>a row source that yields NOTHING — every check below would then compare an empty union against an
///         empty source and report green;</item>
///   <item>slot indices ≠ {0 … <c>Partitions</c>−1} — a deleted, duplicated or mis-slotted partition class;</item>
///   <item>an EMPTY partition over a non-empty source — more partitions declared than the source can fill, the
///         one waste a pure union check cannot see because the surviving slots still cover the whole set;</item>
///   <item>a partition holding more or fewer than its even share (⌊rows/P⌋ or ⌈rows/P⌉ of a source's rows) — a
///         sliced member that filters by something other than the stride, so one partition can grow into the
///         family's serial pole while the union still covers every row once;</item>
///   <item>the union of the partitions ≠ the source as a MULTISET — rows dropped or double-run.</item>
/// </list>
/// The share check is what bounds a partition, and it bounds it RELATIVELY: a partition holds 1/P of its family
/// whatever the family's size, so a family that grows grows every partition alike, and no absolute row bound exists
/// to be crossed by growth alone (kb/Work PB4146; DESIGN-test-build-ci.md section 3.11).
/// ⚠ The slot and empty-partition checks have been fired deliberately (a partition class commented out; a slice count
/// desynced from the const) rather than trusted silent, the share check by a planted count
/// (<c>TestPartitionCoverageDriftTests</c> in the Unit assembly) — and the discovery filter itself went RED on its
/// first real run, on Roslyn's compiler-generated <c>&lt;&gt;O</c> type.
/// It is deliberately shape-driven rather than registered: a new family is covered with no edit here, which is
/// what keeps "automatic" true.
/// </remarks>
internal static class TestPartitionAudit
{
    /// <summary>Name of the <c>public const int</c> every partitioned family base declares.</summary>
    internal const string PartitionCountMember = "Partitions";

    /// <summary>One discovered family: its base definition, declared partition count, concrete partitions and
    /// the row sources audited.</summary>
    internal sealed record Family(Type BaseDefinition, int DeclaredPartitions, IReadOnlyList<Type> Partitions,
        IReadOnlyList<string> RowSources)
    {
        /// <summary>The base's short name, for reporting.</summary>
        public string Name => BaseDefinition.Name;
    }

    /// <summary>The audit result: what was found, and every way it is wrong.</summary>
    internal sealed record Report(IReadOnlyList<Family> Families, IReadOnlyList<string> Violations);

    /// <summary>Run <see cref="Audit"/> and THROW with the full report if anything is wrong — the one line each
    /// test assembly's drift test consists of.</summary>
    /// <remarks>This throws rather than asserting so that <c>tests/_shared</c> stays free of an xUnit reference:
    /// <c>tests/Directory.Build.props</c> links these files into EVERY project under <c>tests/</c>, and
    /// <c>WiseOwl.Cobol.Benchmarks</c> is not a test project and has no xUnit package.
    /// <para>⛔ Zero families is a FAILURE, not a pass. A selector that returns nothing is evidence about what it
    /// returned, never about what it dropped, and a coverage gate that quietly found nothing to cover is the
    /// exact shape of a green test holding a gap open.</para></remarks>
    internal static void AssertClean(Assembly assembly)
    {
        var report = Audit(assembly);
        var problems = new List<string>(report.Violations);
        if (report.Families.Count == 0)
        {
            problems.Add($"NO partitioned family was found in {assembly.GetName().Name} — this drift test is "
                + "asserting nothing. Either a family lost its abstract generic base (and its rows are no longer "
                + "partitioned) or this test class belongs in a different assembly.");
        }

        if (problems.Count == 0) return;

        string census = string.Join("\n", report.Families.Select(f =>
            $"  {f.Name}: Partitions={f.DeclaredPartitions}, classes=[{string.Join(", ", f.Partitions.Select(t => t.Name))}], "
            + $"sources=[{string.Join(", ", f.RowSources)}]"));
        throw new InvalidOperationException(
            $"{problems.Count} partitioned-test coverage violation(s) in {assembly.GetName().Name}:\n"
            + string.Join("\n", problems.Select(p => "  ⛔ " + p))
            + (census.Length > 0 ? $"\nFamilies found:\n{census}" : string.Empty));
    }

    /// <summary>Audit every partitioned family in <paramref name="assembly"/>.</summary>
    internal static Report Audit(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var families = new List<Family>();
        var violations = new List<string>();

        AuditSlotLadder(violations);

        foreach (var baseDefinition in assembly.GetTypes()
                     .Where(IsFamilyBaseDefinition)
                     .OrderBy(t => t.Name, StringComparer.Ordinal))
        {
            families.Add(AuditFamily(assembly, baseDefinition, violations));
        }

        return new Report(families, violations);
    }

    /// <summary>A family base is a TOP-LEVEL, author-written, ABSTRACT generic definition with exactly one type
    /// parameter, constrained to <see cref="ITestPartitionSlot"/>. That shape is the registration — there is no
    /// list to keep current.</summary>
    /// <remarks>⚠ The two negative clauses are not defensive padding — they are why this gate went RED on its very
    /// first run. Roslyn emits a nested <c>&lt;&gt;O</c> delegate-cache class inside any generic type that caches a
    /// lambda, and a nested type INHERITS its enclosing type's generic parameter <i>with its constraints</i>. So
    /// every partitioned family produced a phantom "family" called <c>&lt;&gt;O</c> with no
    /// <c>Partitions</c> const and no partition classes. Compiler-generated and nested types are excluded on
    /// principle rather than by name.</remarks>
    private static bool IsFamilyBaseDefinition(Type t)
        => t is { IsAbstract: true, IsGenericTypeDefinition: true, IsNested: false }
           && !t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false)
           && t.GetGenericArguments() is [{ } p]
           && p.GetGenericParameterConstraints().Contains(typeof(ITestPartitionSlot));

    private static Family AuditFamily(Assembly assembly, Type baseDefinition, List<string> violations)
    {
        string name = baseDefinition.Name;

        int declared = -1;
        if (baseDefinition.GetField(PartitionCountMember, BindingFlags.Public | BindingFlags.Static) is { } f
            && f.GetRawConstantValue() is int n)
        {
            declared = n;
        }
        else
        {
            violations.Add($"{name}: no `public const int {PartitionCountMember}` — a partitioned family must "
                + "declare how many partitions it slices into, so the audit can prove the classes match it.");
        }

        // The concrete partitions: every non-abstract class whose immediate base is this definition, closed.
        var partitions = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.BaseType is { IsGenericType: true } b
                        && b.GetGenericTypeDefinition() == baseDefinition)
            .OrderBy(t => SlotIndexOf(t), Comparer<int>.Default)
            .ToList();

        var rowSources = baseDefinition.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.GetCustomAttribute<PartitionedRowSourceAttribute>() is not null)
            .OrderBy(m => m.Name, StringComparer.Ordinal)
            .ToList();

        if (partitions.Count == 0)
        {
            violations.Add($"{name}: declares {declared} partitions but NO concrete partition class exists — "
                + "every row of this family is unreachable and the leg is silently green.");
            return new Family(baseDefinition, declared, partitions, [.. rowSources.Select(m => m.Name)]);
        }

        if (rowSources.Count == 0)
        {
            violations.Add($"{name}: no [{nameof(PartitionedRowSourceAttribute)}] method — the family's row set "
                + "is unstated, so coverage cannot be proved. Mark the unpartitioned source.");
        }

        // ① The slot indices must be EXACTLY {0 .. declared-1}.
        var slots = partitions.Select(SlotIndexOf).ToList();
        if (declared >= 0 && (slots.Distinct().Count() != slots.Count
                              || !slots.Order().SequenceEqual(Enumerable.Range(0, declared))))
        {
            violations.Add($"{name}: {PartitionCountMember} = {declared} but the concrete partition classes carry "
                + $"slots [{string.Join(", ", slots.Order())}] — expected exactly [0 … {declared - 1}], each once. "
                + $"Classes: {string.Join(", ", partitions.Select(t => t.Name))}.");
        }

        foreach (var source in rowSources)
        {
            AuditRowSource(name, declared, partitions, source, violations);
        }

        return new Family(baseDefinition, declared, partitions, [.. rowSources.Select(m => m.Name)]);
    }

    private static void AuditRowSource(string familyName, int declared, IReadOnlyList<Type> partitions,
        MethodInfo source, List<string> violations)
    {
        string sliced = source.GetCustomAttribute<PartitionedRowSourceAttribute>()!.SlicedMember;
        var whole = Invoke(partitions[0].BaseType!, source.Name, familyName, violations);
        if (whole is null) return;

        // ⓪ An EMPTY source makes every check below compare nothing to nothing and report green — the exact shape
        //    of a zero fan-out that looks clean. A family exists because it has rows; none is a failure.
        if (whole.Count == 0)
        {
            violations.Add($"{familyName}.{source.Name} yielded NO rows. Every coverage check below would then "
                + "compare an empty union against an empty source and pass, so the family would be partitioned, "
                + "green, and testing nothing.");
            return;
        }

        var expected = Multiset(whole);
        var actual = new Dictionary<string, int>(StringComparer.Ordinal);
        var counts = new List<(string Partition, int Rows)>(partitions.Count);
        foreach (var partition in partitions)
        {
            var rows = Invoke(partition.BaseType!, sliced, familyName, violations);
            if (rows is null) return;
            counts.Add((partition.Name, rows.Count));

            // ② An EMPTY partition over a non-empty source. The union check below CANNOT see this: the remaining
            //    slots still cover the whole set, and an empty collection looks exactly like a fast one. It means
            //    the family declares more partitions than its source has rows — those collections cost a test
            //    class each and run nothing — or that the sliced member filters by something other than the
            //    stride, in which case the coverage the union check just proved is accidental.
            //    (The opposite desync — slicing with a count SMALLER than the highest slot in use — cannot reach
            //    here at all: TestPartitioning.Slice's own range guard throws first, which was verified by
            //    deliberately desyncing it.)
            if (rows.Count == 0)
            {
                violations.Add($"{familyName}.{sliced}: partition {partition.Name} selected 0 of {whole.Count} "
                    + $"rows — {PartitionCountMember} = {declared} exceeds what this source can fill, so that "
                    + "collection runs nothing.");
            }

            foreach (var key in rows)
            {
                actual[key] = actual.GetValueOrDefault(key) + 1;
            }
        }

        // ③ Every partition holds its even share of the source. The empty partitions ② already named are left out,
        //    so one cause is reported once.
        foreach (var (partition, rows) in OutsideTheirShare(whole.Count, counts).Where(c => c.Rows > 0))
        {
            var (low, high) = Share(whole.Count, counts.Count);
            violations.Add($"{familyName}.{sliced}: partition {partition} holds {rows} of {whole.Count} rows, outside "
                + $"its share of {low}..{high} at {counts.Count} partitions — the sliced member does not divide the "
                + $"source by the stride ({nameof(TestPartitioning)}.{nameof(TestPartitioning.SliceRows)}), so one "
                + "partition can grow into the family's serial pole.");
        }

        // ④ The union must equal the source as a MULTISET — nothing dropped, nothing run twice.
        var missing = expected.Where(kv => actual.GetValueOrDefault(kv.Key) < kv.Value)
            .Select(kv => kv.Key).Order(StringComparer.Ordinal).ToList();
        var extra = actual.Where(kv => kv.Value > expected.GetValueOrDefault(kv.Key))
            .Select(kv => kv.Key).Order(StringComparer.Ordinal).ToList();
        if (missing.Count > 0 || extra.Count > 0)
        {
            violations.Add($"{familyName}.{sliced}: the {partitions.Count} partitions do not cover "
                + $"{source.Name}'s {whole.Count} rows exactly once — {missing.Count} row(s) NEVER RUN, "
                + $"{extra.Count} row(s) run more than once. "
                + $"First missing: {string.Join(" | ", missing.Take(5))}. "
                + $"First duplicated: {string.Join(" | ", extra.Take(5))}.");
        }
    }

    /// <summary>The even share of <paramref name="total"/> rows over <paramref name="partitions"/> partitions: each
    /// holds ⌊total/P⌋ or ⌈total/P⌉ rows, which is exactly what the stride of <see cref="TestPartitioning.Slice{T}"/>
    /// gives.</summary>
    internal static (int Low, int High) Share(int total, int partitions)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(total);
        ArgumentOutOfRangeException.ThrowIfLessThan(partitions, 1);
        return (total / partitions, (total + partitions - 1) / partitions);
    }

    /// <summary>The partitions of <paramref name="counts"/> (name and row count, one entry per partition) holding
    /// fewer or more rows than their even <see cref="Share"/> of <paramref name="total"/>.</summary>
    internal static IReadOnlyList<(string Partition, int Rows)> OutsideTheirShare(int total,
        IReadOnlyList<(string Partition, int Rows)> counts)
    {
        ArgumentNullException.ThrowIfNull(counts);
        var (low, high) = Share(total, counts.Count);
        return [.. counts.Where(c => c.Rows < low || c.Rows > high)];
    }

    /// <summary>Invoke a public static parameterless row member on a CLOSED family base and key its rows.</summary>
    private static List<string>? Invoke(Type closedBase, string member, string familyName, List<string> violations)
    {
        var method = closedBase.GetMethod(member, BindingFlags.Public | BindingFlags.Static,
            binder: null, types: Type.EmptyTypes, modifiers: null);
        if (method is null)
        {
            violations.Add($"{familyName}: `{member}` is not a public static parameterless member of "
                + $"{closedBase.Name} — the [{nameof(PartitionedRowSourceAttribute)}] pairing is stale.");
            return null;
        }

        if (method.Invoke(null, null) is not IEnumerable<object[]> rows)
        {
            violations.Add($"{familyName}: `{member}` did not return IEnumerable<object[]>.");
            return null;
        }

        return [.. rows.Select(Key)];
    }

    /// <summary>A row's identity, for multiset comparison. Rows are theory arguments — scalars and strings — so a
    /// unit-separated invariant rendering is a faithful key.</summary>
    private static string Key(object[] row)
        => string.Join('␟', row.Select(o => Convert.ToString(o, CultureInfo.InvariantCulture) ?? "<null>"));

    private static Dictionary<string, int> Multiset(IEnumerable<string> keys)
    {
        var m = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string k in keys)
        {
            m[k] = m.GetValueOrDefault(k) + 1;
        }

        return m;
    }

    /// <summary>The slot index carried by a concrete partition class, via its closed base's type argument.</summary>
    private static int SlotIndexOf(Type partition)
        => (int)partition.BaseType!.GetGenericArguments()[0]
            .GetProperty(nameof(ITestPartitionSlot.Index), BindingFlags.Public | BindingFlags.Static)!
            .GetValue(null)!;

    /// <summary>⛔ The slot ladder itself is drift-prone in the one way that would corrupt EVERY family at once: a
    /// copy-paste that leaves <c>Slot9.Index =&gt; 8</c>. Then two classes claim slot 8, a ninth of the rows never
    /// run, and every family using that slot is wrong together. Checked here so it is checked once.</summary>
    private static void AuditSlotLadder(List<string> violations)
    {
        var slots = typeof(ITestPartitionSlot).Assembly.GetTypes()
            .Where(t => t.IsValueType && t.IsAssignableTo(typeof(ITestPartitionSlot)))
            .ToList();
        if (slots.Count == 0)
        {
            violations.Add("the slot ladder is EMPTY — no ITestPartitionSlot implementation exists.");
            return;
        }

        foreach (var slot in slots.OrderBy(t => t.Name, StringComparer.Ordinal))
        {
            int index = (int)slot.GetProperty(nameof(ITestPartitionSlot.Index),
                BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
            string digits = new(slot.Name.SkipWhile(char.IsLetter).ToArray());
            if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out int fromName)
                || fromName != index)
            {
                violations.Add($"slot {slot.Name} reports Index = {index} — the name and the index disagree, so "
                    + "every partition class using it is in a different partition than it reads as.");
            }
        }

        var indices = slots.Select(t => (int)t.GetProperty(nameof(ITestPartitionSlot.Index),
            BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!).Order().ToList();
        if (!indices.SequenceEqual(Enumerable.Range(0, slots.Count)))
        {
            violations.Add($"the slot ladder is not contiguous from 0: [{string.Join(", ", indices)}].");
        }
    }
}
