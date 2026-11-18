// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using BenchmarkDotNet.Attributes;
using WiseOwl.Cobol.Benchmarks.Collation;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Benchmarks.Compile;

/// <summary>
/// COMPILE THROUGHPUT (kb/Work PB2117, the instrument kb/Work A6 asked for): the warm, in-process cost of compiling
/// the largest programs in the NIST corpus and its most copybook-heavy one, through <see cref="CompilerDriver"/>, the
/// entry point the CLI and every test harness call. Each program is compiled exactly as the NIST harness compiles it
/// (CCVS X-card preprocessing, COBOL 85; <c>NistDifferentialTests</c>), so the input is real, not synthetic.
/// <para>
/// The two methods SPLIT one compile rather than compare two candidates: <see cref="FrontEndAndBind"/> is
/// <see cref="CompilerDriver.Options.CheckOnly"/> (preprocess, parse, bind, the version-conformance pass), the
/// baseline; <see cref="FullCompile"/> adds emit and the Roslyn C#-to-IL backend and writes the assembly. The Ratio
/// column is therefore "how many times the front end the whole compile costs".
/// </para>
/// <para>
/// This is the WARM number (JIT and the backend's reference-assembly cache already populated by the warm-up
/// iterations). The COLD number, a fresh <c>cobol</c> process per compile, is what a user waits for; it is timed by
/// <c>scripts/arch/perf_baseline.py</c> as a whole process.
/// </para>
/// <para>
/// THE LISTING'S COST (DESIGN-listing.md section 9; kb/Work PB5923): <see cref="ListingMode"/> compiles each program with no
/// listing (<see cref="ListingMode.None"/>, the path every caller that asks for none takes) and with the paginated text
/// listing (<see cref="ListingMode.Text"/>, written beside the assembly). The budgets: None within the run-to-run noise of
/// the parent commit, Text at most 10 % over None. Later steps add <c>All</c>. Allocated bytes come from the
/// <c>MemoryDiagnoser</c> of <see cref="BenchmarkConfig"/>.
/// </para>
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class CompileBenchmarks
{
    /// <summary>The NIST programs compiled. IX113A, NC105A and NC218A are the three largest NIST sources by size
    /// (5,816 / 3,117 / 3,077 lines); SM201A makes the most COPY statements of any NIST program (13, from
    /// <c>tests/nist/copylib</c>, several of them with REPLACING), so it measures the library-text path.</summary>
    [Params("IX113A", "NC105A", "NC218A", "SM201A")]
    public string Program { get; set; } = "";

    /// <summary>Which listing each compile writes (DESIGN-listing.md section 9).</summary>
    public enum ListingMode { None, Text }

    /// <summary>The listing asked of every compile of the case.</summary>
    [Params(ListingMode.None, ListingMode.Text)]
    public ListingMode Listing { get; set; }

    private CompilerDriver.Options _full = null!;
    private CompilerDriver.Options _checkOnly = null!;
    private string _outputDirectory = "";

    /// <summary>The WITNESS (dotnet-engineering: a benchmark proves it did the work before its number is quoted): both
    /// shapes of the compile must succeed on this program, the full compile must write a non-empty assembly, and the
    /// copybook program must really have read its library texts. A failure throws, and BenchmarkDotNet reports the
    /// case as failed instead of timing a compile that stopped at its first diagnostic.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _outputDirectory = ScratchDirectory.Create("pb2117-compile-");
        string source = TestRepo.Nist("programs", Program + ".cob");
        _full = new CompilerDriver.Options(source, Path.Combine(_outputDirectory, Program + ".dll"),
            NistTestName: Program, DialectLevel: 85,
            Listing: Listing == ListingMode.Text ? new WiseOwl.Cobol.Listing.ListingRequest() : null);
        _checkOnly = _full with { CheckOnly = true };

        var full = CompilerDriver.Compile(_full);
        if (!full.Success || new FileInfo(full.OutputDll).Length == 0)
            throw new InvalidOperationException(
                $"{Program}: the full compile did not produce an assembly ({full.Status}): {string.Join("; ", full.Errors)}");
        var checkOnly = CompilerDriver.Compile(_checkOnly);
        if (!checkOnly.Success)
            throw new InvalidOperationException(
                $"{Program}: the check-only compile failed ({checkOnly.Status}): {string.Join("; ", checkOnly.Errors)}");

        // The copybook witness: the source plus at least one library text per COPY-making program.
        int copybooksRead = full.Inputs.FilesRead.Count(f => !string.Equals(Path.GetFullPath(f),
            Path.GetFullPath(source), StringComparison.OrdinalIgnoreCase));
        if (Program.StartsWith("SM", StringComparison.Ordinal) && copybooksRead == 0)
            throw new InvalidOperationException($"{Program}: the compile read no COPY library text");
        // The listing witness: the Text case wrote a listing that lists every line of the source; None wrote none.
        string listing = Path.ChangeExtension(full.OutputDll, WiseOwl.Cobol.Listing.ListingRequest.TextExtension);
        if (Listing == ListingMode.Text && (full.Listing is null || !File.Exists(listing)
                || full.Listing.Lines.Count(l => l.Inclusion is null) != File.ReadAllText(source).Split('\n').Length))
            throw new InvalidOperationException($"{Program}: the Text case wrote no listing of every source line");
        if (Listing == ListingMode.None && (full.Listing is not null || File.Exists(listing)))
            throw new InvalidOperationException($"{Program}: the None case wrote a listing");
        Console.WriteLine($"// witness {Program} {Listing}: {full.Inputs.FilesRead.Count} files read ({copybooksRead} library "
                          + $"texts), assembly {new FileInfo(full.OutputDll).Length} bytes"
                          + (Listing == ListingMode.Text ? $", listing {new FileInfo(listing).Length} bytes" : ""));
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        ScratchDirectory.Remove(_outputDirectory);
    }

    /// <summary>Preprocess, parse, bind and the version-conformance pass: everything before C# exists.</summary>
    [Benchmark(Baseline = true)]
    public CompilerDriver.Result FrontEndAndBind() => CompilerDriver.Compile(_checkOnly);

    /// <summary>The whole compile: the front end, emit, Roslyn, and the assembly written to disk.</summary>
    [Benchmark]
    public CompilerDriver.Result FullCompile() => CompilerDriver.Compile(_full);
}
