// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE RUNTIME ASKS WHETHER A PHYSICAL FILE IS PRESENT IN EXACTLY ONE PLACE — <c>HostFile.Probe</c>, in
/// <c>Cobol.Net.Runtime/IO/FileSupport.cs</c>. Every other file under <c>Cobol.Net.Runtime/IO</c> is forbidden
/// <c>File.Exists</c>, <c>FileInfo.Exists</c> and the raw <c>File.GetAttributes</c> the probe is built from.
/// <para>The rule exists because a two-valued probe cannot state the answer the standard requires. ISO
/// §9.1.13.6 item 5 sets '35' when <i>"the physical file is not present"</i>; §14.9.27.4 GR3 sets '37' when
/// <i>"the file ... is present and insufficient authority exists to open the file"</i>. <c>File.Exists</c>
/// swallows every access error and returns <c>false</c>, so it reports the second case as the first — and
/// silently, since a program cannot tell an invented empty OPTIONAL file from a real one.</para>
/// <para>⛔ THIS IS NOT HYPOTHETICAL, AND IT IS NOT A ONE-OFF. The DELETE FILE arm was fixed in kb/Work PB140
/// with a comment naming the exact mechanism; the OPEN arm of the same sweep was never done and kept answering
/// '35'/'05' in THREE connectors for another five months (kb/Work PB323 — the repo's most reproducible defect
/// shape: a dispatch with two arms, only one of them fixed). A hand-written probe is cheap to add and
/// invisible in review, so the guard is structural: there is one probe, and it is named.</para>
/// </summary>
public sealed class FileExistenceProbeDriftTests
{
    /// <summary>Any host-filesystem existence probe. The <c>.Exists</c> half is deliberately spelled as the
    /// bare member read rather than as <c>File.Exists</c>/<c>FileInfo…Exists</c>: the FIRST draft of this guard
    /// matched the type names and sailed straight past <c>var info = new FileInfo(p); if (info.Exists)</c> —
    /// which is precisely the shape that sat in <c>SequentialConnector.NoticeIfLayoutDisagrees</c>. Nothing
    /// under this subsystem has a legitimate <c>.Exists</c> read, so the member itself is the rule.</summary>
    private static readonly Regex ExistenceProbe =
        new(@"\.Exists\b|\bFile\.GetAttributes\s*\(", RegexOptions.Compiled);

    /// <summary>The one file allowed to carry a probe — <c>HostFile.Probe</c> and the <c>FilePresence</c> it
    /// answers with live here.</summary>
    private const string ProbeHome = "FileSupport.cs";

    [Fact]
    public void OnlyHostFileProbe_AsksWhetherAPhysicalFileIsPresent()
    {
        string io = TestRepo.Src("Cobol.Net.Runtime", "IO");
        Assert.True(Directory.Exists(io), $"The IO subsystem moved: {io} is not a directory.");

        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(io, "*.cs", SearchOption.AllDirectories))
        {
            if (string.Equals(Path.GetFileName(file), ProbeHome, StringComparison.Ordinal)) continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.TrimStart().StartsWith("//", StringComparison.Ordinal)) continue;   // prose about the rule
                if (line.TrimStart().StartsWith("///", StringComparison.Ordinal)) continue;
                if (ExistenceProbe.IsMatch(line))
                    offenders.Add($"{Path.GetRelativePath(io, file)}:{i + 1}: {line.Trim()}");
            }
        }

        Assert.True(offenders.Count == 0,
            "A hand-written presence probe under Cobol.Net.Runtime/IO. File.Exists and FileInfo.Exists answer "
            + "FALSE for a file that is present but refused, which is ISO §9.1.13.6 item 5's '35' where "
            + "§14.9.27.4 GR3 requires '37' — and, for an OPTIONAL file, a SUCCESSFUL open over an invented "
            + "empty file (kb/Work PB323, the OPEN twin of PB140). Call HostFile.Probe and switch on the three "
            + "FilePresence states instead. Offending sites:\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>The complement of the rule above, and the reason it is worth asserting: a guard that names an
    /// allowed file is only as good as that file still being the probe. If <c>HostFile.Probe</c> is ever
    /// deleted or renamed, the sweep above would pass over a subsystem with NO presence probe at all — a green
    /// that proves nothing (feedback: green_gates_arent_evidence).</summary>
    [Fact]
    public void TheProbeItselfStillLivesInItsNamedHome()
    {
        string home = TestRepo.Src("Cobol.Net.Runtime", "IO", ProbeHome);
        string text = File.ReadAllText(home);
        Assert.Contains("public static FilePresence Probe(string hostPath)", text, StringComparison.Ordinal);
        Assert.Contains("File.GetAttributes(hostPath)", text, StringComparison.Ordinal);
        Assert.Contains("FilePresence.Unauthorized", text, StringComparison.Ordinal);
    }
}
