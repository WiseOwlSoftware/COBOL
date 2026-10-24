// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CobolNet.Tests.Shared;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The conformance harness's smoke: the greenfield compiler driver compiles a trivial program through the
/// compiled-program cache. The NIST corpus runs in <see cref="NistDifferentialTests"/> against
/// <c>tests/nist/valid/*.txt</c>, and the differential cases in the <c>*DifferentialTests</c> classes against
/// <c>tests/differential/</c> (<see cref="DifferentialGolden"/>).
/// </summary>
public sealed class DifferentialHarness
{
    [Fact]
    public void Scaffold_NewCompilerCompilesATrivialProgram()
    {
        string tempDir = ScratchDirectory.Create("CobolNet_Conf_");
        try
        {
            string src = Path.Combine(tempDir, "p.cob");
            File.WriteAllText(src,
                "IDENTIFICATION DIVISION.\nPROGRAM-ID. P.\nPROCEDURE DIVISION.\nMAIN.\n    DISPLAY \"OK\".\n    STOP RUN.\n");
            var result = CompiledProgramCache.Compile(new CompilerDriver.Options(src, Path.Combine(tempDir, "p.dll"), SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(result.Success, $"{result.Status}: {string.Join("\n", result.Errors)}");
        }
        finally
        {
            ScratchDirectory.Remove(tempDir);
        }
    }
}
