// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Architecture;

/// <summary>
/// ⛔ A MEMBER OF THE DATA BINDER, OR OF A BINDER CARVED FROM IT, IS NO WIDER THAN ITS READERS NEED, AND A NEW READER
/// OF ONE IS A CONSCIOUS CHANGE (kb/Work PB2361; docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1), step 11).
/// <para>The ten steps of the <c>DataBinder</c> extract (PB2296–PB2360) carved the concern binders out of it and widened
/// many of its members from <c>private</c> to <c>internal</c> so the new binders and passes could reach them. The C#
/// type system cannot say WHICH types may read an <c>internal</c> member, so this test says it:
/// <c>binder-member-readers.json</c> lists every non-private member of each owner type (the data binder and the binders
/// carved from it) with the outermost product types outside the owner whose code names it, and a metadata walk of the
/// built compiler (<see cref="TypeReferenceWalk.MemberReaders"/>) must measure exactly that. A test is not a reader: the
/// product is what the boundary describes, so a member only a test reads is wider than the product needs.</para>
/// <list type="bullet">
/// <item>a member no other type names is refused: make it <c>private</c> (nothing stays wider than its readers);</item>
/// <item>a reader the file does not list, or a member it does not list, is refused: a new reader of a binder's member is
/// a decision written into the file in the change that adds it, where review sees it;</item>
/// <item>a reader or member the walk no longer measures is refused too, so the file is the code's current picture and
/// never accumulates stale entries (on the unoptimized image the file measures; an optimized one is checked only for
/// growth, as the edges file is).</item>
/// </list>
/// <para>On a mismatch the test writes the measured picture beside the run's output (the path is in the message), so the
/// file is updated by copying the entries the change really meant, never by regenerating it blind.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class BinderMemberReadersDriftTests
{
    internal static readonly string ReadersFile = TestRepo.Tests("WiseOwl.Cobol.Tests.Unit", "Architecture", "binder-member-readers.json");

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        NewLine = "\n",
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    [Fact]
    public void EveryBinderMember_HasExactlyTheReadersTheFileLists_AndNoneIsWiderThanItsReaders()
    {
        BinderMemberReaders file = Load();
        Assert.NotEmpty(file.Owners);
        var walk = TypeReferenceWalk.Read([Path.Combine(AppContext.BaseDirectory, "WiseOwl.Cobol.Compiler.dll")],
            _ => TestRepo.Src("WiseOwl.Cobol.Compiler"), file.Owners.ToHashSet(StringComparer.Ordinal));
        SortedDictionary<string, string[]> measured = Measured(walk);

        // Vacuity: the walk found members of every owner, so an owner renamed or moved, or a walk that stopped reading
        // the compiler, is red rather than an empty agreement.
        foreach (string owner in file.Owners)
        {
            Assert.True(measured.Keys.Any(k => k.StartsWith(owner + "::", StringComparison.Ordinal)),
                $"the walk found no member of {owner}: the owner was renamed or moved, or the walk no longer reads the compiler");
        }

        List<string> findings = Check(file.Members, measured, walk.Optimized);
        if (findings.Count > 0)
        {
            string written = Path.Combine(AppContext.BaseDirectory, "binder-member-readers.measured.json");
            File.WriteAllText(written, JsonSerializer.Serialize(file with { Members = measured }, Json) + "\n");
            Assert.Fail($"{findings.Count} binder member(s) disagree with {Path.GetRelativePath(TestRepo.Root, ReadersFile)} "
                + $"(the measured picture is {written}; copy only the entries your change means):\n  "
                + string.Join("\n  ", findings.Take(60)) + (findings.Count > 60 ? $"\n  … and {findings.Count - 60} more" : ""));
        }
    }

    /// <summary>The checker over a planted assembly: a member read by another type keeps its reader, a member only its
    /// owner reads is refused as wider than its readers, a property and a nested type are keyed by their own names, a
    /// private member and an override are not declared, and a reader the file does not list is refused, as is one the
    /// walk no longer measures.</summary>
    [Fact]
    public void TheChecker_RefusesAWideMember_ANewReader_AndAStaleOne()
    {
        const string Source = """
            namespace P
            {
                internal sealed class Owner
                {
                    internal int ReadOutside() => Helper();
                    internal int ReadInside() => 1;
                    private int Helper() => ReadInside();
                    internal string Name { get; } = "";
                    internal sealed class Nested { internal static int Value() => 2; }
                    public override string ToString() => Name;
                }

                internal static class Reader
                {
                    internal static int Use(Owner o) => o.ReadOutside() + o.Name.Length + Owner.Nested.Value();
                }
            }
            """;
        string dir = ScratchDirectory.Create("pb2361readers");
        try
        {
            string dll = Path.Combine(dir, "Walked.dll");
            var compilation = CSharpCompilation.Create("Walked",
                [CSharpSyntaxTree.ParseText(SourceText.From(Source, System.Text.Encoding.UTF8), path: Path.Combine(dir, "Owner.cs"))],
                [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using (var pe = File.Create(dll))
            using (var pdb = File.Create(Path.ChangeExtension(dll, ".pdb")))
            {
                var emitted = compilation.Emit(pe, pdb, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
                Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics));
            }

            var walk = TypeReferenceWalk.Read([dll], _ => dir, new HashSet<string>(StringComparer.Ordinal) { "P.Owner" });
            SortedDictionary<string, string[]> measured = Measured(walk);
            Assert.Equal(
                ["P.Owner::Name", "P.Owner::Nested", "P.Owner::ReadInside", "P.Owner::ReadOutside"],
                measured.Keys);
            Assert.Equal(["P.Reader"], measured["P.Owner::ReadOutside"]);
            Assert.Equal(["P.Reader"], measured["P.Owner::Name"]);
            Assert.Equal(["P.Reader"], measured["P.Owner::Nested"]);
            Assert.Empty(measured["P.Owner::ReadInside"]);

            var listed = new SortedDictionary<string, string[]>(measured, StringComparer.Ordinal)
            {
                ["P.Owner::ReadOutside"] = [],
                ["P.Owner::Name"] = ["P.Reader", "P.Gone"],
            };
            List<string> findings = Check(listed, measured);
            Assert.Contains(findings, f => f.StartsWith("P.Owner::ReadInside is read by no type outside its owner", StringComparison.Ordinal));
            Assert.Contains(findings, f => f.StartsWith("P.Owner::ReadOutside is read by P.Reader, which the file does not list", StringComparison.Ordinal));
            Assert.Contains(findings, f => f.StartsWith("P.Owner::Name lists P.Gone, which no longer reads it", StringComparison.Ordinal));
            var read = measured.Where(m => m.Value.Length > 0).ToDictionary(StringComparer.Ordinal);
            Assert.Empty(Check(read, read));

            // An optimized image refuses only growth: a reader it lacks is not stale, a reader it adds still is.
            var debug = new SortedDictionary<string, string[]>(read, StringComparer.Ordinal) { ["P.Owner::Name"] = ["P.Reader", "P.Gone"] };
            var optimized = new SortedDictionary<string, string[]>(read, StringComparer.Ordinal) { ["P.Owner::Nested"] = [] };
            Assert.Empty(Check(debug, optimized, optimizedImage: true));
            Assert.Contains(Check(debug, optimized), f => f.StartsWith("P.Owner::Nested is read by no type", StringComparison.Ordinal));
            optimized["P.Owner::Name"] = ["P.Reader", "P.New"];
            Assert.Contains(Check(debug, optimized, optimizedImage: true),
                f => f.StartsWith("P.Owner::Name is read by P.New, which the file does not list", StringComparison.Ordinal));
        }
        finally
        {
            ScratchDirectory.Remove(dir);
        }
    }

    /// <summary>The walk's member readers, ordinal-sorted, a member's readers sorted too.</summary>
    private static SortedDictionary<string, string[]> Measured(TypeReferenceWalk walk) =>
        new(walk.MemberReaders.ToDictionary(m => m.Key.ToString(), m => m.Value.Order(StringComparer.Ordinal).ToArray()),
            StringComparer.Ordinal);

    /// <summary>Every disagreement between the file's members and the measured ones, and every member no type outside
    /// its owner reads. The file is the UNOPTIMIZED (Debug) image's measure; an optimized image can lack a reference
    /// Debug carries (a local's slot, <see cref="TypeReferenceWalk"/>'s "Two images"), so on one
    /// (<paramref name="optimizedImage"/>) only growth is refused: a reader or member the file does not list, and a member
    /// read by nobody that the file does not list with readers.</summary>
    private static List<string> Check(IReadOnlyDictionary<string, string[]> listed, IReadOnlyDictionary<string, string[]> measured,
        bool optimizedImage = false)
    {
        var findings = new List<string>();
        foreach (var (member, readers) in measured)
        {
            if (readers.Length == 0 && !(optimizedImage && listed.TryGetValue(member, out string[]? debugReaders) && debugReaders.Length > 0))
            {
                findings.Add($"{member} is read by no type outside its owner: make it private (kb/Work PB2361)");
                continue;
            }

            if (!listed.TryGetValue(member, out string[]? expected))
            {
                findings.Add($"{member} is a member the file does not list (read by {string.Join(", ", readers)}): list it with its readers, "
                    + "or keep it private");
                continue;
            }

            foreach (string reader in readers.Except(expected, StringComparer.Ordinal))
            {
                findings.Add($"{member} is read by {reader}, which the file does not list: a new reader of a binder's member is a "
                    + "decision; list it, or read what the binder publishes instead (DataBindResult, a slice)");
            }

            foreach (string gone in optimizedImage ? [] : expected.Except(readers, StringComparer.Ordinal))
            {
                findings.Add($"{member} lists {gone}, which no longer reads it: delete the entry");
            }
        }

        foreach (string member in listed.Keys.Except(measured.Keys, StringComparer.Ordinal))
        {
            findings.Add($"{member} is listed but the walk no longer finds it (renamed, deleted or made private): delete the entry");
        }

        return findings;
    }

    private static BinderMemberReaders Load() =>
        JsonSerializer.Deserialize<BinderMemberReaders>(File.ReadAllText(ReadersFile), Json)
        ?? throw new InvalidDataException($"{ReadersFile} is empty");

    /// <summary>The file's shape: the owner types, and each member's readers.</summary>
    private sealed record BinderMemberReaders(int Schema, string Rule, string[] Owners, IReadOnlyDictionary<string, string[]> Members);
}
