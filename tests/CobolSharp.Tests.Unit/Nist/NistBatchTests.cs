using System.Collections.Concurrent;
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Lexing;
using CobolSharp.Compiler.Parsing;
using CobolSharp.Compiler.Preprocessor;
using Xunit;
using Xunit.Abstractions;

namespace CobolSharp.Tests.Unit.Nist;

public class NistBatchTests
{
    private readonly ITestOutputHelper _output;
    private static readonly string NistDir = Path.Combine("E:\\CobolSharp", "tests", "nist", "programs");

    public NistBatchTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void CompileAllNistPrograms()
    {
        if (!Directory.Exists(NistDir))
        {
            _output.WriteLine($"NIST directory not found: {NistDir}");
            return;
        }

        var files = Directory.GetFiles(NistDir, "*.cob").OrderBy(f => f).ToArray();
        _output.WriteLine($"Found {files.Length} NIST programs");

        var results = new ConcurrentDictionary<string, (string? phase, string? error)>();

        // Run all files concurrently, each with a 10s timeout
        Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            file =>
            {
                string name = Path.GetFileNameWithoutExtension(file);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                try
                {
                    var result = CompileOneProgram(file, cts.Token);
                    results[name] = result;
                }
                catch (OperationCanceledException)
                {
                    results[name] = ("TIMEOUT", "Cancelled after 10s — likely infinite loop");
                }
                catch (Exception ex)
                {
                    results[name] = ("CRASH", Truncate(ex.Message, 100));
                }
            });

        // Tally results
        int passed = results.Values.Count(r => r.phase == null);
        var failures = results.Where(r => r.Value.phase != null)
            .Select(r => (file: r.Key, phase: r.Value.phase!, error: r.Value.error!))
            .OrderBy(f => f.file).ToList();

        _output.WriteLine($"\nNIST Batch: {passed}/{files.Length} passed ({100.0 * passed / files.Length:F1}%)");
        _output.WriteLine($"Failures: {failures.Count}");

        // By phase
        foreach (var group in failures.GroupBy(f => f.phase).OrderByDescending(g => g.Count()))
            _output.WriteLine($"  {group.Key}: {group.Count()}");

        // By module
        _output.WriteLine("\nBy module:");
        foreach (var group in failures.GroupBy(f => GetModule(f.file)).OrderBy(g => g.Key))
        {
            _output.WriteLine($"  {group.Key} ({group.Count()} failures):");
            foreach (var (file, phase, error) in group.Take(5))
                _output.WriteLine($"    {file,-12} [{phase}] {Truncate(error, 80)}");
            if (group.Count() > 5)
                _output.WriteLine($"    ... and {group.Count() - 5} more");
        }
    }

    private static (string? phase, string? error) CompileOneProgram(string file, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            string raw = File.ReadAllText(file);
            string normalized = ReferenceFormatProcessor.NormalizeToFreeForm(raw);
            var copyProc = new CopyProcessor();
            normalized = copyProc.Process(normalized, Path.GetDirectoryName(file) ?? ".");

            ct.ThrowIfCancellationRequested();
            var source = SourceText.From(normalized, file);
            var diagnostics = new DiagnosticBag();
            var lexer = new Lexer(source, diagnostics);
            var tokens = lexer.Tokenize();

            ct.ThrowIfCancellationRequested();
            var parser = new Parser(tokens, diagnostics, source);
            var ast = parser.ParseCompilationUnit();

            if (diagnostics.HasErrors)
            {
                var errors = diagnostics.Diagnostics.Where(d => d.IsError).ToList();
                return ("PARSE-ERRORS", $"{errors.Count} errors: {errors[0]}");
            }
            return (null, null);
        }
        catch (OperationCanceledException) { throw; }
        catch (InvalidOperationException ex) when (ex.Message.Contains("iterations"))
        {
            return ("INFINITE-LOOP", Truncate(ex.Message, 100));
        }
        catch (Exception ex)
        {
            return ("CRASH", Truncate(ex.Message, 100));
        }
    }

    private static string GetModule(string fileName)
    {
        if (fileName.StartsWith("OB", StringComparison.OrdinalIgnoreCase))
            return fileName[..4].ToUpperInvariant();
        if (fileName.Length >= 2)
            return fileName[..2].ToUpperInvariant();
        return "??";
    }

    private static string Truncate(string msg, int max = 120) =>
        msg.Replace('\n', ' ').Replace('\r', ' ') is var m && m.Length > max ? m[..max] + "..." : m;
}
