// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A SOURCE COMMENT, DOC COMMENT, CREF, GRAMMAR COMMENT OR MESSAGE THAT NAMES <c>DataBinder.Something</c> NAMES A MEMBER
/// <c>DataBinder</c> HAS (kb/Work PB4468; docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1)).
/// <para>The PB2296 extract moved the file, forest, configuration, linkage, report and OO concerns off <c>DataBinder</c> onto
/// slices and binders of their own, and left behind roughly a hundred prose references to members that had moved
/// (<c>DataBinder.VisibleFiles</c>, <c>DataBinder.TypeDecls</c>, <c>DataBinder.BindMethodData</c>, a <c>cref</c> to
/// <c>DataBinder._lapsedTypeMembers</c>). The compiler cannot police them: documentation generation is off, so CS1574 never
/// fires, and a reader who follows a stale name finds nothing. Each <c>DataBinder.Name</c> in the product's source and in the
/// construct catalogue must therefore resolve to a member or nested type that <c>DataBinder</c> declares, or to a
/// partial that exists (<c>DataBinder.Name.cs</c>, or its stem <c>DataBinder.Name</c>).</para>
/// <para>A comment that records a moved member for the history says "the former <c>Name</c>" without the type prefix, or names the
/// member's present owner; the shape that is refused is the one that points a reader at a member that is not there. The design
/// documents under <c>docs/</c> and the tests are outside the scan: the documents carry dated narrative that names the
/// tree as it was, and the tests that guard a forbidden shape must be able to write the forbidden name.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed partial class DataBinderNameReferenceDriftTests
{
    private const BindingFlags AllDeclared = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
        | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    [GeneratedRegex(@"(?<![A-Za-z_0-9])DataBinder\.([A-Za-z_][A-Za-z_0-9]*)(\.cs)?")]
    private static partial Regex Reference();

    private static HashSet<string> MemberNames()
    {
        var type = typeof(DataBinder);
        return
        [
            .. type.GetMembers(AllDeclared).Select(m => m.Name),
            .. type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).Select(t => t.Name),
        ];
    }

    private static IEnumerable<string> ScannedFiles()
    {
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src(), "*", SearchOption.AllDirectories))
        {
            string ext = Path.GetExtension(file);
            if (ext is not (".cs" or ".g4")) continue;
            string sep = Path.DirectorySeparatorChar.ToString();
            if (file.Contains($"{sep}obj{sep}") || file.Contains($"{sep}bin{sep}")) continue;
            yield return file;
        }
        yield return TestRepo.VersionMatrix("constructs.json");
    }

    [Fact]
    public void EveryDataBinderNameInTheProductSource_IsAMemberOrAPartialFile()
    {
        var members = MemberNames();
        // the scan must see the type, or every name would be reported stale (or none)
        Assert.True(members.Count >= 200, $"DataBinder reflects only {members.Count} names — the scan lost them");
        var partials = Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding"), "DataBinder*.cs")
            .Select(Path.GetFileName).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("DataBinder.cs", partials);

        var stale = new SortedSet<string>(StringComparer.Ordinal);
        int references = 0;
        foreach (string file in ScannedFiles())
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                foreach (Match m in Reference().Matches(lines[i]))
                {
                    references++;
                    string name = m.Groups[1].Value;
                    bool isFile = m.Groups[2].Success;
                    // a partial is also named by its stem ("DataBinder.GroupValue" for DataBinder.GroupValue.cs)
                    bool ok = isFile
                        ? partials.Contains($"DataBinder.{name}.cs")
                        : members.Contains(name) || partials.Contains($"DataBinder.{name}.cs")
                            || (name == "cs" && partials.Contains("DataBinder.cs"));
                    if (!ok)
                        stale.Add($"{Path.GetRelativePath(TestRepo.Root, file)}:{i + 1}: DataBinder.{name}{(isFile ? ".cs" : "")}");
                }
            }
        }
        Assert.True(references >= 100, $"only {references} DataBinder.Name references were seen — the scan lost the tree");
        Assert.True(stale.Count == 0,
            "These name a DataBinder member (or partial file) that does not exist; name its present owner (a slice, a binder "
            + "or a pass) or drop the type prefix when recording history (kb/Work PB4468):\n" + string.Join("\n", stale));
    }
}
