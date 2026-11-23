// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
#nullable enable
using System.Reflection;
using System.Text.Json;

namespace WiseOwl.Cobol.Tests.Shared.Gate;

/// <summary>THE WEIGHTS FILE (<c>tests/_shared/test-weights.json</c>, written by <c>scripts/shard_weights.py
/// generate</c>; kb/Work PB4329): each test class's median seconds in the merge group, by assembly, and the default
/// for a class it does not name. tests/Directory.Build.props embeds it in every test assembly as
/// <see cref="ResourceName"/>, so a host deals by the weights it was built with.</summary>
internal sealed class TestWeights
{
    /// <summary>The schema this reader understands (<c>shard_weights.py</c> <c>WEIGHTS_SCHEMA</c>).</summary>
    public const int Schema = 1;

    public const string ResourceName = "WiseOwl.Cobol.Tests.Shared.Gate.test-weights.json";

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
    /// the shards over every assembly a shard job runs.</summary>
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
