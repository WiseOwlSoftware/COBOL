// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE RUNTIME ASKS THE OPERATING ENVIRONMENT ABOUT A PHYSICAL FILE IN EXACTLY ONE PLACE — <c>HostFile</c>,
/// in <c>Cobol.Net.Runtime/IO/FileSupport.cs</c> — AND THE OPEN CONTRACT ASKS EACH QUESTION ONCE, ABOVE THE
/// ORGANIZATIONS. There are two such questions and this class guards both: <c>HostFile.Probe</c> ("is the file
/// there, and may this process observe it?") and <c>HostFile.PermitsWrite</c> ("may this process write it?").
/// <para>The presence rule exists because a two-valued probe cannot state the answer the standard requires. ISO
/// §9.1.13.6 item 5 sets '35' when <i>"the physical file is not present"</i>; §14.9.27.4 GR3 sets '37' when
/// <i>"the file ... is present and insufficient authority exists to open the file"</i>. <c>File.Exists</c>
/// swallows every access error and returns <c>false</c>, so it reports the second case as the first — and
/// silently, since a program cannot tell an invented empty OPTIONAL file from a real one. Every other file
/// under <c>Cobol.Net.Runtime/IO</c> is therefore forbidden <c>File.Exists</c>, <c>FileInfo.Exists</c> and the
/// raw <c>File.GetAttributes</c> the probe is built from.</para>
/// <para>The write-capability rule exists because §14.9.27.4 GR16 and §9.1.13.6 item 6 a) name no organization:
/// a file that <i>"will not support write operations"</i> is '37' whichever organization is reading it. Asked
/// inside an organization's OPEN arm it is asked only by the organizations that happen to touch a stream —
/// which is what kb/Work PB328 was, and why <c>PermitsWrite</c> may be called only from <c>FileConnector</c>.
/// </para>
/// <para>⛔ NEITHER IS HYPOTHETICAL, AND NEITHER IS A ONE-OFF. The DELETE FILE presence arm was fixed in
/// kb/Work PB140 with a comment naming the exact mechanism; the OPEN arm of the same sweep was never done and
/// kept answering '35'/'05' in THREE connectors for another five months (kb/Work PB323). PB328 is the same
/// shape one question over: the sequential arm answered '37' for a read-only file and the two keyed arms
/// answered '00' (the repo's most reproducible defect shape — a dispatch with two arms, only one of them
/// fixed). A hand-written probe is cheap to add and invisible in review, so the guard is structural: there is
/// one place each question is asked, and it is named.</para>
/// </summary>
public sealed class HostFileProbeDriftTests
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

    // ── The write-capability question: asked in the OPEN contract, never in an organization arm (PB328) ──────

    /// <summary>The file that owns the OPEN contract — the ONE caller §14.9.27.4 GR16's question is allowed to
    /// have.</summary>
    private const string OpenContractHome = "FileConnector.cs";

    /// <summary>⛔ §14.9.27.4 GR16 IS AN OPEN-CONTRACT RULE, NOT AN ORGANIZATION'S RULE. GR16 and §9.1.13.6
    /// item 6 a) name no organization, so the question <i>"will this file support write operations?"</i> has to
    /// be asked once, above <c>OpenCore</c>, or it is asked only by whichever organizations happen to open a
    /// stream — which is exactly what kb/Work PB328 was: the sequential arm's I-O and EXTEND streams ARE write
    /// opens, so a read-only file answered '37' there, while the relative and indexed arms only read their
    /// store and answered '00' on the SAME file, believed the success, and surfaced the loss as a '30' at CLOSE.
    /// A future fourth organization inherits the answer only for as long as nobody re-asks it locally.</summary>
    [Fact]
    public void OnlyTheOpenContractAsksWhetherAPresentFileMayBeWritten()
    {
        string io = TestRepo.Src("Cobol.Net.Runtime", "IO");
        Assert.True(Directory.Exists(io), $"The IO subsystem moved: {io} is not a directory.");

        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(io, "*.cs", SearchOption.AllDirectories))
        {
            string leaf = Path.GetFileName(file);
            if (string.Equals(leaf, ProbeHome, StringComparison.Ordinal)) continue;          // the definition
            if (string.Equals(leaf, OpenContractHome, StringComparison.Ordinal)) continue;   // the one caller
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.TrimStart().StartsWith("//", StringComparison.Ordinal)) continue;
                if (line.TrimStart().StartsWith("///", StringComparison.Ordinal)) continue;
                if (line.Contains("PermitsWrite", StringComparison.Ordinal))
                    offenders.Add($"{Path.GetRelativePath(io, file)}:{i + 1}: {line.Trim()}");
            }
        }

        Assert.True(offenders.Count == 0,
            "A write-capability probe outside the OPEN contract. ISO §14.9.27.4 GR16 and §9.1.13.6 item 6 a) "
            + "name no organization, so the question belongs once in FileConnector.Open, above OpenCore, where "
            + "every organization — including one added later — inherits its answer (kb/Work PB328). Offending "
            + "sites:\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>The positive complement, for the same reason as
    /// <see cref="TheProbeItselfStillLivesInItsNamedHome"/>: the sweep above is a ban, and a ban over a
    /// subsystem that asks the question NOWHERE passes just as green as one that asks it in the right place.
    /// This pins the probe to its home and the single call to the OPEN contract.</summary>
    [Fact]
    public void TheWriteCapabilityQuestionIsAskedExactlyOnce_InTheOpenContract()
    {
        string home = File.ReadAllText(TestRepo.Src("Cobol.Net.Runtime", "IO", ProbeHome));
        Assert.Contains("public static bool PermitsWrite(string hostPath)", home, StringComparison.Ordinal);

        string[] contract = File.ReadAllLines(TestRepo.Src("Cobol.Net.Runtime", "IO", OpenContractHome));
        int calls = contract.Count(l =>
            !l.TrimStart().StartsWith("//", StringComparison.Ordinal)
            && l.Contains("HostFile.PermitsWrite(", StringComparison.Ordinal));
        Assert.True(calls == 1,
            $"FileConnector must ask §14.9.27.4 GR16's write-capability question exactly once; found {calls}. "
            + "Two call sites are two rules (kb/Work PB328); zero means the '37' the standard requires for a "
            + "read-only I-O, EXTEND or OUTPUT target is not produced at all.");
    }
}
