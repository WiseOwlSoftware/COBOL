// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using WiseOwl.Cobol.Cli;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE HAND-WRITTEN USER MANUAL STAYS TRUE (kb/Work PB1610, PB4453; DESIGN-USER-DOCUMENTATION §4, §5). ISO/IEC 1989:2023
/// §4.2.16 makes <c>docs/manual/</c> the implementation's user documentation, and a manual that names an option the
/// command line lacks, a diagnostic code the catalogue never allocated, a page that does not exist, or a program that no
/// longer compiles is documentation that misleads.
/// <para>
/// NO COMPILER TEST OPENS A MARKDOWN FILE (owner 2026-11-02). The pages are checked by <c>scripts/docchecks/manual.py</c>
/// in the DOC AUDITS gate audit: their frontmatter, their links and headings, every diagnostic code they name against the
/// codes the compiler allocates, and the command-line page against the command line's options and exit statuses (the
/// compiler's side of those two is <c>tests/doc-facts/compiler-facts.json</c>, held equal to <c>Program.BuildParser</c>,
/// <c>Program.ExitCode</c> and the catalogues by <see cref="DocumentedFactsDriftTests"/>). What THIS class holds is the
/// part only the compiler can: every complete COBOL program the pages print compiles, check-only, at the default edition.
/// The programs are the files of <c>tests/doc-facts/manual-programs/</c>, extracted from the pages by
/// <c>python scripts/doc_audits.py --write-manual</c> and held identical to them by the audit, so a program a page changes
/// reaches this test and a word around it does not. The generated chapters of <c>docs/manual/reference/</c> (kb/Work
/// PB1611, PB1612) have drift tests of their own and are not scanned.
/// </para>
/// </summary>
public sealed class UserManualDriftTests
{
    private static string ProgramsDirectory => TestRepo.At("tests", "doc-facts", "manual-programs");

    [Fact]
    public void EveryCompleteProgramInTheManual_Compiles()
    {
        string dir = ScratchDirectory.Create("usermanual");
        try
        {
            var offenders = new List<string>();
            var programs = Directory.EnumerateFiles(ProgramsDirectory, "*.cob").Order(StringComparer.Ordinal).ToList();
            foreach (string program in programs)
            {
                string src = Path.Combine(dir, Path.GetFileName(program));
                File.WriteAllText(src, File.ReadAllText(program).Replace("\r\n", "\n"), new UTF8Encoding(false));
                var result = CompilerDriver.Compile(new CompilerDriver.Options(src, CheckOnly: true));
                if (!result.Success)
                    offenders.Add($"{Path.GetFileName(program)}: {string.Join(" | ", result.Errors)}");
            }
            Assert.True(programs.Count > 0, "no complete COBOL program in tests/doc-facts/manual-programs/: the example extraction matched nothing");
            Assert.True(offenders.Count == 0, "a complete program printed in the manual does not compile:\n" + string.Join("\n", offenders));
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
