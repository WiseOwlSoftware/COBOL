// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
//
// THE TEST ORDERER of every test host (GateOrderer: the classes the gate names first, then longest first by the
// committed weights; kb/Work PB4328, docs/rearchitecture/DESIGN-test-build-ci.md section 3.14.3), and CI's SHARD HOOK
// (GateShard, TestWeights; kb/Work PB4329, the same document's section 1.4). tests/Directory.Build.props links this
// file into every test project under tests/ and names GateTestFramework in each one's [assembly: Xunit.TestFramework].
#nullable enable
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace WiseOwl.Cobol.Gate;

/// <summary>
/// xunit's own framework, with an executor that ORDERS every run (<see cref="GateOrderer"/>) and runs one weighted shard
/// of it when, and only when, CI's merge group names one (<see cref="GateShard"/>, kb/Work PB4329).
/// </summary>
/// <remarks>
/// <para>Ordering changes WHEN a case runs, never WHETHER (owner, 2026-09-28: "order, don't skip"): with no shard every
/// discovered case runs, and the gate's population check (<c>scripts/test_population.py</c>) proves it by name.</para>
/// <para>⛔ WHY A REFUSAL IS AN EXECUTION ERROR AND NOT A THROW (evidence <c>b3-order-probe</c>): xunit catches a
/// framework constructor that throws and silently falls back to its default framework, so every case runs and the
/// run is green. An executor that throws is a "Catastrophic failure" with no verdict line and no trx result. An
/// executor that reports every case as xunit's <see cref="ExecutionErrorTestCase"/> gives a failed result per case,
/// a <c>Failed!</c> verdict line and exit 1, so a shard variable no host can run never passes as a run.</para>
/// </remarks>
public class GateTestFramework(IMessageSink messageSink) : XunitTestFramework(messageSink)
{
    protected sealed override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName) =>
        new GateExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
}

/// <summary>The executor: resolves the shard once per run and hands the runner either every case or the shard's, in the
/// orderer's order, or every case as an execution error when the shard cannot be run.</summary>
internal sealed class GateExecutor(AssemblyName assemblyName, ISourceInformationProvider sourceInformationProvider,
    IMessageSink diagnosticMessageSink)
    : XunitTestFrameworkExecutor(assemblyName, sourceInformationProvider, diagnosticMessageSink)
{
    // xunit's executor contract is `async void` (XunitTestFrameworkExecutor.RunTestCases); the runner reports the
    // run's end through the execution sink, never through a returned task.
    protected override async void RunTestCases(IEnumerable<IXunitTestCase> testCases,
        IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
    {
        var cases = testCases.ToList();
        IReadOnlyList<IXunitTestCase> selection = Select(cases, out GateOrderer? orderer);
        using var runner = new GateAssemblyRunner(TestAssembly, selection, DiagnosticMessageSink,
            executionMessageSink, executionOptions, orderer);
        await runner.RunAsync();
    }

    /// <summary>What this run executes, and the orderer that orders it (null for a refusal). A shard that cannot be read
    /// or dealt refuses the run: every case becomes an execution error naming the cause. An orderer that cannot be made
    /// (an unreadable first-classes file) costs the ORDER, never a case: xunit's order is kept and the cause is a
    /// diagnostic message.</summary>
    private IReadOnlyList<IXunitTestCase> Select(List<IXunitTestCase> cases, out GateOrderer? orderer)
    {
        orderer = null;
        string assemblyKey;
        GateShard? shard;
        try
        {
            assemblyKey = TestWeights.AssemblyKey(TestAssembly.Assembly.Name);
            shard = GateShard.Read(Environment.GetEnvironmentVariable);
        }
        catch (Exception ex)
        {
            return Refuse(cases, ex is GateShardException ? ex.Message : $"the shard could not be read: {ex}");
        }

        List<IXunitTestCase> mine;
        try
        {
            mine = shard is null ? cases : shard.CasesOf(cases, assemblyKey, TestWeights.Embedded());
        }
        catch (Exception ex)
        {
            return Refuse(cases, $"shard {shard} could not be dealt: {ex}");
        }

        try
        {
            orderer = GateOrderer.For(assemblyKey, TestWeights.Embedded(), Environment.GetEnvironmentVariable);
        }
        catch (Exception ex)
        {
            DiagnosticMessageSink.OnMessage(new DiagnosticMessage($"GATE ORDER: xunit's order is kept ({ex.Message})"));
        }

        return mine;
    }

    private List<IXunitTestCase> Refuse(List<IXunitTestCase> cases, string cause) =>
        cases.Select(c => (IXunitTestCase)new ExecutionErrorTestCase(DiagnosticMessageSink,
                TestMethodDisplay.ClassAndMethod, TestMethodDisplayOptions.None, c.TestMethod,
                $"{GateShard.RefusalPrefix}{cause} (case {c.DisplayName})")).ToList();
}

/// <summary>xunit's assembly runner with the gate's collection orderer installed.</summary>
internal sealed class GateAssemblyRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases,
    IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink,
    ITestFrameworkExecutionOptions executionOptions, GateOrderer? orderer)
    : XunitTestAssemblyRunner(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
{
    /// <summary>xunit reads assembly-level <c>[TestCollectionOrderer]</c> attributes in
    /// <see cref="XunitTestAssemblyRunner.AfterTestAssemblyStartingAsync"/>; the gate's orderer is installed after it.</summary>
    protected override async Task AfterTestAssemblyStartingAsync()
    {
        await base.AfterTestAssemblyStartingAsync();
        if (orderer is not null)
        {
            TestCollectionOrderer = orderer.Collections(TestCases);
        }
    }
}

/// <summary>THE SHARD HOOK (kb/Work PB4329; docs/rearchitecture/DESIGN-test-build-ci.md section 1.4): CI's merge group
/// runs every assembly as <see cref="Count"/> shards, the job of shard <see cref="Index"/> setting
/// <c>WISEOWL_COBOL_SHARD=k/N</c>, and each host runs the cases of the test classes <see cref="TestWeights.Deal"/> gives
/// shard k. Discovery never sees it (the executor deals), so <c>--list-tests</c> stays the whole population that
/// <c>ci-gate</c> checks the shards' trx against (test_population.py check): a case dealt to no shard or two is a named
/// NEVER RAN or RAN TWICE, never silence.</summary>
internal sealed partial record GateShard(int Index, int Count)
{
    public const string Variable = "WISEOWL_COBOL_SHARD";

    /// <summary>The first words of every refused case's message.</summary>
    public const string RefusalPrefix = "GATE SHARD REFUSED: ";

    [GeneratedRegex(@"\A(?<k>[1-9][0-9]{0,2})/(?<n>[1-9][0-9]{0,2})\z")]
    private static partial Regex Spelling();

    /// <summary>The shard <paramref name="environment"/> names, or null when <see cref="Variable"/> is unset or empty
    /// (Windows cannot hold an empty variable). Throws <see cref="GateShardException"/> for any other spelling than
    /// <c>k/N</c> with 1 &lt;= k &lt;= N: a shard nobody can run must refuse the run, never run the whole of it.</summary>
    public static GateShard? Read(Func<string, string?> environment)
    {
        string? text = environment(Variable);
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        Match m = Spelling().Match(text);
        int k = m.Success ? int.Parse(m.Groups["k"].Value, CultureInfo.InvariantCulture) : 0;
        int n = m.Success ? int.Parse(m.Groups["n"].Value, CultureInfo.InvariantCulture) : 0;
        return m.Success && k <= n
            ? new GateShard(k, n)
            : throw new GateShardException($"{Variable} is '{text}', not k/N with 1 <= k <= N");
    }

    /// <summary>This shard's cases, in their discovery order: those whose test class the weights deal to it.</summary>
    public List<T> CasesOf<T>(IEnumerable<T> cases, string assemblyKey, TestWeights weights) where T : ITestCase
    {
        var all = cases.ToList();
        Dictionary<string, int> dealt = weights.Deal(Count, assemblyKey, all.Select(c => ClassOf(c)));
        return all.Where(c => dealt[ClassOf(c)] == Index).ToList();
    }

    /// <summary>A case's test class, as the trx's <c>className</c> and the weights file spell it.</summary>
    public static string ClassOf(ITestCase testCase) => testCase.TestMethod.TestClass.Class.Name;

    public override string ToString() => $"{Index}/{Count}";
}

/// <summary>A shard variable no host can run.</summary>
internal sealed class GateShardException(string message) : Exception(message);

/// <summary>THE WEIGHTS FILE (<c>tests/_shared/test-weights.json</c>, written by <c>scripts/shard_weights.py
/// generate</c>; kb/Work PB4329): each test class's median seconds in the merge group, by assembly, and the default
/// for a class it does not name. tests/Directory.Build.props embeds it in every test assembly as
/// <see cref="ResourceName"/>, so a host deals by the weights it was built with.</summary>
internal sealed class TestWeights
{
    /// <summary>The schema this reader understands (<c>shard_weights.py</c> <c>WEIGHTS_SCHEMA</c>).</summary>
    public const int Schema = 1;

    public const string ResourceName = "WiseOwl.Cobol.Gate.test-weights.json";

    private static readonly Lazy<TestWeights> EmbeddedFile = new(() =>
    {
        using Stream stream = typeof(TestWeights).Assembly.GetManifestResourceStream(ResourceName)
                              ?? throw new InvalidDataException(
                                  $"the weights file is not embedded in this assembly as {ResourceName}");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return Parse(bytes.ToArray());
    });

    /// <summary>Every (assembly, class) the file names, with its weight in tenths of a second (whole numbers, so every
    /// host of one build deals identically).</summary>
    private readonly Dictionary<(string Assembly, string Class), long> _tenths;

    private TestWeights(Dictionary<(string Assembly, string Class), long> tenths, long defaultTenths)
    {
        _tenths = tenths;
        DefaultTenths = defaultTenths;
    }

    /// <summary>The weight of a class the file does not name.</summary>
    public long DefaultTenths { get; }

    public IReadOnlyDictionary<(string Assembly, string Class), long> Tenths => _tenths;

    /// <summary>The gated test projects are <c>WiseOwl.Cobol.Tests.&lt;key&gt;</c>; the weights file, the shards and the
    /// gate's population check name an assembly by its key (<c>scripts/test_population.py</c> <c>GATED_ASSEMBLIES</c>).</summary>
    public static string AssemblyKey(string assemblyName)
    {
        const string Prefix = "WiseOwl.Cobol.Tests.";
        string simple = new AssemblyName(assemblyName).Name
                        ?? throw new ArgumentException($"'{assemblyName}' names no assembly", nameof(assemblyName));
        return simple.StartsWith(Prefix, StringComparison.Ordinal) ? simple[Prefix.Length..] : simple;
    }

    /// <summary>The weights embedded in this test assembly.</summary>
    public static TestWeights Embedded() => EmbeddedFile.Value;

    /// <summary>Read a weights file's bytes; <see cref="InvalidDataException"/> names the defect.</summary>
    public static TestWeights Parse(ReadOnlySpan<byte> bytes)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(bytes.ToArray());
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"the weights file is not JSON ({ex.Message})");
        }

        using (doc)
        {
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("schema", out JsonElement schema) || !schema.TryGetInt32(out int s) || s != Schema)
            {
                throw new InvalidDataException($"the weights file is not schema {Schema}");
            }

            long fallback = root.TryGetProperty("default_seconds", out JsonElement d) ? TenthsOf(d, "default_seconds") : -1;
            if (fallback <= 0)
            {
                throw new InvalidDataException("the weights file's default_seconds is not a positive number");
            }

            if (!root.TryGetProperty("assemblies", out JsonElement assemblies) || assemblies.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("the weights file carries no assemblies object");
            }

            var tenths = new Dictionary<(string, string), long>();
            foreach (JsonProperty assembly in assemblies.EnumerateObject())
            {
                if (assembly.Value.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidDataException($"the weights file's assembly '{assembly.Name}' is not an object");
                }

                foreach (JsonProperty cls in assembly.Value.EnumerateObject())
                {
                    long w = TenthsOf(cls.Value, $"{assembly.Name}/{cls.Name}");
                    if (w < 0)
                    {
                        throw new InvalidDataException($"the weight of {assembly.Name}/{cls.Name} is negative");
                    }

                    tenths[(assembly.Name, cls.Name)] = w;
                }
            }

            return new TestWeights(tenths, fallback);
        }
    }

    private static long TenthsOf(JsonElement value, string what) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double seconds) && double.IsFinite(seconds)
            ? (long)Math.Round(seconds * 10, MidpointRounding.AwayFromZero)
            : throw new InvalidDataException($"the weights file's {what} is not a number");

    /// <summary>DEAL the classes to <paramref name="count"/> shards, longest first (LPT): every class the file names,
    /// of every assembly, heaviest first (ties by assembly then class, ordinal), each to the lightest shard so far (ties
    /// to the lowest); then the <paramref name="present"/> classes of <paramref name="assemblyKey"/> the file does not
    /// name, at the default weight, in ordinal order, the same way. Returns the 1-based shard of every present class.
    /// It is a function of the file, the count and the present classes alone, and every shard of one CI run sees the
    /// same three, so the shards partition the population. Dealing every assembly's named classes in one pass balances
    /// the shards over the three assemblies a shard job runs.</summary>
    public Dictionary<string, int> Deal(int count, string assemblyKey, IEnumerable<string> present)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        var load = new long[count];
        int Lightest()
        {
            int best = 0;
            for (int i = 1; i < count; i++)
            {
                if (load[i] < load[best])
                {
                    best = i;
                }
            }

            return best;
        }

        var shardOf = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var ((assembly, cls), w) in _tenths
                     .OrderByDescending(e => e.Value)
                     .ThenBy(e => e.Key.Assembly, StringComparer.Ordinal)
                     .ThenBy(e => e.Key.Class, StringComparer.Ordinal))
        {
            int i = Lightest();
            load[i] += w;
            if (assembly == assemblyKey)
            {
                shardOf[cls] = i + 1;
            }
        }

        foreach (string cls in present.Distinct(StringComparer.Ordinal).Where(c => !shardOf.ContainsKey(c))
                     .Order(StringComparer.Ordinal))
        {
            int i = Lightest();
            load[i] += DefaultTenths;
            shardOf[cls] = i + 1;
        }

        return shardOf;
    }
}

/// <summary>THE ORDER OF A RUN (kb/Work PB4328; DESIGN-test-build-ci.md section 3.14.3): the test collections whose
/// classes the gate names FIRST (the file <see cref="FirstVariable"/> names: the classes red at the worktree's previous
/// gate or in the merge-queue run that bounced it, written by <c>scripts/gate.py</c> <c>Gate._first</c>), then every
/// collection longest first by the committed weights (a collection weighs the sum of its classes' weights, a class the
/// file does not name the default), ties in discovery order. Longest first keeps a long collection from starting last
/// and running alone at the end; the first classes put a re-gate's likely reds where its log shows them first. It
/// ORDERS, it never selects: every collection it is handed is returned exactly once (<see cref="GateOrder"/>).</summary>
internal sealed class GateOrderer(string assemblyKey, TestWeights weights, IReadOnlySet<string> first)
{
    /// <summary>The variable naming the first-classes file, one class per line (<c>scripts/gate.py</c> <c>FIRST_CLASSES</c>,
    /// the same name; its prefix is one <c>scripts/test_population.py</c> scrubs, so only the gate hands it to a host).</summary>
    public const string FirstVariable = "WISEOWL_COBOL_GATE_FIRST";

    public IReadOnlySet<string> First => first;

    /// <summary>The orderer of <paramref name="environment"/>: the first classes its file names (none when the variable
    /// is unset or empty). Throws when the file it names cannot be read: the executor then keeps xunit's order.</summary>
    public static GateOrderer For(string assemblyKey, TestWeights weights, Func<string, string?> environment)
    {
        string? path = environment(FirstVariable);
        HashSet<string> named = string.IsNullOrEmpty(path)
            ? []
            : File.ReadAllLines(path).Select(l => l.Trim()).Where(l => l.Length > 0).ToHashSet(StringComparer.Ordinal);
        return new GateOrderer(assemblyKey, weights, named);
    }

    /// <summary>The collection orderer of one run's cases: each collection's rank from the classes of its cases.</summary>
    public ITestCollectionOrderer Collections(IEnumerable<ITestCase> cases)
    {
        var classes = new Dictionary<Guid, HashSet<string>>();
        foreach (ITestCase c in cases)
        {
            Guid id = c.TestMethod.TestClass.TestCollection.UniqueID;
            if (!classes.TryGetValue(id, out HashSet<string>? set))
            {
                classes[id] = set = new HashSet<string>(StringComparer.Ordinal);
            }

            set.Add(GateShard.ClassOf(c));
        }

        return new GateCollectionOrderer(classes.ToDictionary(e => e.Key, e => Rank(e.Value)));
    }

    /// <summary>A collection's sort key: first-named before the rest, then heavier before lighter.</summary>
    internal (bool Later, long Lighter) Rank(IEnumerable<string> classes)
    {
        var all = classes.ToList();
        long tenths = all.Sum(c => weights.Tenths.TryGetValue((assemblyKey, c), out long w) ? w : weights.DefaultTenths);
        return (!all.Any(first.Contains), -tenths);
    }
}

/// <summary>Orders collections by their rank (<see cref="GateOrderer.Rank"/>); equal ranks keep xunit's order.</summary>
internal sealed class GateCollectionOrderer(IReadOnlyDictionary<Guid, (bool Later, long Lighter)> ranks) : ITestCollectionOrderer
{
    public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections) =>
        GateOrder.Permutation(testCollections, cs => cs
            .OrderBy(c => ranks.TryGetValue(c.UniqueID, out var r) ? r : (true, 0L)));
}

/// <summary>⛔ An orderer's output is CHECKED to be a permutation of its input. xunit trusts an orderer's result:
/// a case an orderer dropped would silently never run and one it repeated would run twice. The check throws instead,
/// and xunit then logs the orderer's exception and keeps its own (complete) order — ordering is best effort,
/// completeness is not.</summary>
internal static class GateOrder
{
    public static List<T> Permutation<T>(IEnumerable<T> input, Func<IReadOnlyList<T>, IEnumerable<T>> order)
    {
        var inList = input.ToList();
        var outList = order(inList).ToList();
        var count = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
        foreach (T item in inList)
        {
            object key = item!;
            count[key] = count.GetValueOrDefault(key) + 1;
        }

        foreach (T item in outList)
        {
            object key = item!;
            if (!count.TryGetValue(key, out int n) || n == 0)
            {
                throw new InvalidOperationException(
                    $"a gate orderer's output is not a permutation of its input ({inList.Count} in, {outList.Count} out; "
                    + $"an item it returned was not in the input or was returned twice)");
            }

            count[key] = n - 1;
        }

        if (outList.Count != inList.Count)
        {
            throw new InvalidOperationException(
                $"a gate orderer's output is not a permutation of its input ({inList.Count} in, {outList.Count} out)");
        }

        return outList;
    }
}
