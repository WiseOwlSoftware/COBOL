// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.CommandLine;
using System.Diagnostics;
using WiseOwl.Cobol;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Cli;

/// <summary>
/// The <c>cobol</c> command-line driver: a thin shell over <see cref="CompilerDriver"/> — it parses arguments
/// (via <c>System.CommandLine</c>, Microsoft's first-party parser), prints diagnostics, optionally runs the
/// compiled program, and maps the compile outcome to a process exit code. All compilation logic lives in the
/// WiseOwl.Cobol.Compiler library (so it is testable without an exe).
/// </summary>
/// <remarks>
/// Usage: <c>cobol &lt;source.cob&gt; [-o out.dll] [--nist [NAME]] [--std 85|2002|2014|2023] [--permissive]
/// [--copy DIR] [--run] [--sign-encoding ibm|ascii] [--numeric-display decimal|digits] [--source-format fixed|free|auto] [--flag-extensions] [--repository-update on|off]
/// [--repository DIR] [--reference FILE.dll] [--repository-check on|off]</c>. Every value option accepts BOTH <c>--opt value</c> and <c>--opt=value</c>; no
/// option can swallow a following flag. The generated C# is written next to the output assembly
/// (<c>&lt;name&gt;.g.cs</c>) so the translation is directly inspectable.
/// </remarks>
internal static class Program
{
    private static int Main(string[] args)
    {
        // ⛔ BEFORE ANYTHING IS WRITTEN. Diagnostics carry ISO citations — `(ISO §11.10.3 SR5–6)` — and on a
        // default Windows console (cp437) the section sign left this process as the single byte 0x15 and the
        // en dash as '-', in every redirected capture and every CI log, because this entry point never set an
        // encoding while WiseOwl.Cobol.Runtime's did (kb/Work PB899 — the two-arm dispatch with one arm fixed). One
        // rule, one place: WiseOwl.Cobol.Runtime.IO.StandardStreams, which the other process entry, a compiled program's
        // Main (ProgramTable.RunModule), calls too; a host entry never does (kb/Work PB3193).
        WiseOwl.Cobol.Runtime.IO.StandardStreams.EnsureUtf8();
        return Parse(args).Invoke();
    }

    /// <summary>The name of the batch-check command, the one command line whose first word is not a source file.</summary>
    internal const string CheckBatchName = "check-batch";

    /// <summary>⛔ THE ONE ROUTE FROM A COMMAND LINE TO ITS PARSE (kb/Work PB4340). The compile and the batch check are
    /// two verbs, not a command and its child: <c>System.CommandLine</c> writes a parent command's arguments before
    /// a subcommand, both in the subcommand's usage line and in its grammar, so while check-batch was a subcommand of
    /// the compile root its help listed <c>&lt;source&gt;</c> and printed <c>cobol &lt;source&gt; check-batch
    /// &lt;manifest&gt;</c>, and <c>cobol x.cob check-batch m.tsv</c> ran the batch and ignored <c>x.cob</c>. Each verb
    /// is therefore its own command tree, and the first word chooses the tree.</summary>
    internal static ParseResult Parse(IReadOnlyList<string> args) =>
        args is [CheckBatchName, ..] ? BuildCheckBatchParser().Parse(args) : BuildParser().Command.Parse(args);

    /// <summary>Build the argument parser (exposed for unit testing the grammar without spawning a process):
    /// the configured <see cref="RootCommand"/> plus a pure resolver that maps a <see cref="ParseResult"/> to the
    /// <see cref="CliOptions"/> DTO (the <c>--nist</c>/<c>--std</c> defaulting is here, testable in isolation).</summary>
    internal static (RootCommand Command, Func<ParseResult, CliOptions> Resolve) BuildParser()
    {
        var sourceArgument = new Argument<string>("source")
        {
            Description = "The COBOL source file to compile.",
        };
        var outputOption = new Option<string?>("-o", "--output")
        {
            Description = "Output assembly path (default: the source path with a .dll extension).",
            HelpName = "out.dll",
        };
        var nistOption = new Option<string?>("--nist")
        {
            Description = "Enable NIST CCVS placeholder preprocessing; the optional test name defaults to the "
                + "source file's base name.",
            Arity = ArgumentArity.ZeroOrOne,   // the name is optional — `--nist` alone is legal
            HelpName = "NAME",
        };
        // kb/Work PB4126: a STRING option whose one validator owns both the accepted spellings and the message, like every
        // sibling value option below. It was an Option<int?>, and reading a token the parser could not convert
        // (`--std COBOL-2002`, `--std abc`, `--std ""`) from inside the validator threw an unhandled
        // InvalidOperationException instead of reporting a usage error.
        var stdOption = new Option<string?>("--std")
        {
            Description = $"Target ISO edition: {string.Join(", ", StdSpellings[..^1])}, or {StdSpellings[^1]} (default: 2023, or 85 with --nist).",
            HelpName = "YEAR",
        };
        stdOption.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<string?>() is { } v && !TryParseStd(v, out _))
                result.AddError($"--std must be one of {string.Join(", ", StdSpellings)} (got {v}).");
        });
        var permissiveOption = new Option<bool>("--permissive")
        {
            Description = "Accept constructs the targeted edition removed, warning instead of rejecting (the "
                + "migration mode; orthogonal to --std/--nist).",
        };
        var copyOption = new Option<string[]>("--copy")
        {
            Description = "A COPY library-text search directory (repeatable), searched in order after the current "
                + "working directory; the source file's own directory is searched only when named here.",
            Arity = ArgumentArity.OneOrMore,
            AllowMultipleArgumentsPerToken = false,   // `--copy A --copy B`, never grabbing the source positional
            HelpName = "DIR",
        };
        var runOption = new Option<bool>("--run")
        {
            Description = "Run the compiled assembly after a successful compile.",
        };
        // kb/Work PB803 (owner decision 2026-09-09): the DISPLAY over-punch convention, Annex A.1 items 177/178.
        // A REPRESENTATION option, orthogonal to --std/--permissive — every edition grants the same latitude
        // (ISO §13.18.52.4 GR4 / GR5 b). An unknown value is refused by the parser exactly as --std's is: a CLI
        // argument error, never a COBOL#### source diagnostic (nothing about the SOURCE is wrong).
        var signEncodingOption = new Option<string?>("--sign-encoding")
        {
            Description = "DISPLAY operational-sign convention for a signed item whose sign is not SEPARATE: "
                + "ibm (default — {A-I / }J-R, IBM / Micro Focus compatible) or ascii (plain digits / p-y).",
            HelpName = string.Join("|", ZonedSign.OptionSpellings),
        };
        signEncodingOption.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<string?>() is { } v && !ZonedSign.TryParseOption(v, out _))
                result.AddError($"--sign-encoding must be one of {string.Join(", ", ZonedSign.OptionSpellings)} (got {v}).");
        });

        // kb/Work PB3505 (owner decision PB3467): the form DISPLAY gives a numeric operand, Annex A.1 item 56. ISO
        // §14.9.11.4 GR1 leaves the device conversion to the implementor; the default follows GnuCOBOL 3.2.0 (the
        // decimal point, a leading sign) and `digits` keeps the digit image. A REPRESENTATION option, orthogonal to
        // --std/--permissive, refused by the parser when unknown exactly as --sign-encoding's value is.
        var numericDisplayOption = new Option<string?>("--numeric-display")
        {
            Description = "How DISPLAY shows, and ACCEPT reads, a numeric item: decimal (default — the decimal point and a "
                + "leading +/-, 00012.50 and -00012.50) or digits (the digit image, 0001250 and 000125}, ACCEPT's digit "
                + "window — see --sign-encoding).",
            HelpName = string.Join("|", NumericDisplayFormOption.OptionSpellings),
        };
        numericDisplayOption.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<string?>() is { } v && !NumericDisplayFormOption.TryParse(v, out _))
                result.AddError($"--numeric-display must be one of {string.Join(", ", NumericDisplayFormOption.OptionSpellings)} (got {v}).");
        });

        // kb/Work PB1362: the reference format the source starts in. ISO §7.3.24.3 2) makes FIXED form the default;
        // free form and the structural auto-detection are the implementor-defined selection mechanism §4.2.10 3)
        // requires for a nonstandard behavior (docs/CONFORMANCE.md DOC-A.1-158). A >>SOURCE FORMAT directive in the
        // text switches the format whichever one it started in. An unknown value is a CLI argument error.
        var sourceFormatOption = new Option<string?>("--source-format")
        {
            Description = "Reference format the source starts in, before any >>SOURCE FORMAT directive: fixed "
                + "(default — ISO 7.3.24.3), free, or auto (detect fixed or free form from the text's layout).",
            HelpName = string.Join("|", InitialReferenceFormatOption.OptionSpellings),
        };
        sourceFormatOption.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<string?>() is { } v && !InitialReferenceFormatOption.TryParse(v, out _))
                result.AddError($"--source-format must be one of "
                    + $"{string.Join(", ", InitialReferenceFormatOption.OptionSpellings)} (got {v}).");
        });

        // kb/Work PB1525: ISO §4.2.10 — "An implementation shall provide a warning mechanism that optionally may be
        // invoked by the user at compile time to indicate use of a nonstandard extension in a compilation group."
        // This is that mechanism. It is a pure opt-in: without it a compile prints exactly what it printed before.
        var flagExtensionsOption = new Option<bool>("--flag-extensions")
        {
            Description = "Warn (COBOL2894) at every use of a nonstandard extension this compiler accepts — a "
                + "construct ISO/IEC 1989 does not define (ISO 4.2.10). The register is docs/CONFORMANCE.md 3.2.",
        };

        // kb/Work PB2100: ISO §8.13 — "The implementor shall provide a mechanism that allows the user to specify whether to
        // update the external repository when a compilation unit is compiled." This is that mechanism
        // (DESIGN-external-repository §9.1): on (the default) writes every published unit's signature into the module, off
        // writes its index and unit identities only. An unknown value is a CLI argument error.
        var repositoryUpdateOption = new Option<string?>("--repository-update")
        {
            Description = "Whether the compilation updates the external repository (ISO 8.13): on (default — the module "
                + "carries every unit's signature, so a later compilation can bind against it) or off (the module runs and "
                + "is found by name, but carries no signatures).",
            HelpName = string.Join("|", OnOffSpellings),
        };
        repositoryUpdateOption.Validators.Add(result => ValidateOnOff(result, "--repository-update"));

        // kb/Work PB2103: the external repository the compilation reads (DESIGN-external-repository §9.3; Annex A.1 item 66):
        // a REPOSITORY specifier the compilation group cannot answer is looked for in the --repository directories in the
        // order given (by default the output directory), then in the --reference assemblies in order (ISO §12.3.8.4 GR10 c) /
        // GR11 c)). A place that does not exist is REPO-10 (a bind diagnostic, with every other).
        var repositoryOption = new Option<string[]>("--repository")
        {
            Description = "An external repository directory (repeatable), searched in order for a program or function a "
                + "REPOSITORY paragraph names and this compilation group does not define (ISO 8.13, 12.3.8.4); default: the "
                + "output directory.",
            Arity = ArgumentArity.OneOrMore,
            AllowMultipleArgumentsPerToken = false,   // `--repository A --repository B`, never grabbing the source positional
            HelpName = "DIR",
        };
        var referenceOption = new Option<string[]>("--reference")
        {
            Description = "A compiled WiseOwl COBOL assembly (repeatable) searched, in order, after the --repository "
                + "directories.",
            Arity = ArgumentArity.OneOrMore,
            AllowMultipleArgumentsPerToken = false,
            HelpName = "FILE.dll",
        };
        // ISO §8.13 — "The implementor shall provide a mechanism that allows the user to specify whether to flag differences
        // in prototypes and definitions in the compilation group from the information in the external repository."
        // (DESIGN-external-repository §9.2, REPO-9.)
        var repositoryCheckOption = new Option<string?>("--repository-check")
        {
            Description = "Whether the compilation flags a program or function whose signature differs from the information "
                + "the external repository holds for it (ISO 8.13): on (default — a warning naming the entry and the first "
                + "difference) or off.",
            HelpName = string.Join("|", OnOffSpellings),
        };
        repositoryCheckOption.Validators.Add(result => ValidateOnOff(result, "--repository-check"));

        // The batch check is a separate tree (Parse), so this description is where the compile's help names it.
        var root = new RootCommand("cobol — translate a COBOL source unit to typed-native .NET (the Roslyn backend). "
            + $"`cobol {CheckBatchName} <manifest>` checks many sources without emitting (`cobol {CheckBatchName} --help`).")
        {
            sourceArgument, outputOption, nistOption, stdOption, permissiveOption, copyOption, runOption,
            signEncodingOption, numericDisplayOption, sourceFormatOption, flagExtensionsOption, repositoryUpdateOption,
            repositoryOption, referenceOption, repositoryCheckOption,
        };

        CliOptions Resolve(ParseResult parse)
        {
            string source = parse.GetValue(sourceArgument)!;
            // The NIST test name is OPTIONAL: `--nist` present means NIST mode (GetResult is non-null even with
            // no value); an absent name is derived from the source file's base name (the CCVS convention).
            bool nistEnabled = parse.GetResult(nistOption) is not null;
            string? nistName = nistEnabled
                ? parse.GetValue(nistOption) ?? Path.GetFileNameWithoutExtension(source)
                : null;
            // --std wins when given; else the NIST CCVS corpus is COBOL-85, otherwise the 2023 default (DEVLOG 519).
            // The validator above has already refused any other spelling, so a false here can only be the ABSENT option.
            int std = TryParseStd(parse.GetValue(stdOption), out int stdYear) ? stdYear : nistEnabled ? 85 : 2023;

            // The validator above has already refused any unparseable value, so a false here can only be the
            // ABSENT option — the documented IBM default (kb/Work PB803).
            _ = ZonedSign.TryParseOption(parse.GetValue(signEncodingOption), out var signEncoding);
            // Likewise: false can only be the absent option — the decimal form, the documented default (kb/Work PB3505).
            _ = NumericDisplayFormOption.TryParse(parse.GetValue(numericDisplayOption), out var numericDisplay);
            // Likewise: false can only be the absent option — fixed form, the standard default (kb/Work PB1362).
            _ = InitialReferenceFormatOption.TryParse(parse.GetValue(sourceFormatOption), out var sourceFormat);
            // Likewise: false can only be the absent option — on, the documented default (DESIGN-external-repository §9.1).
            _ = TryParseOnOff(parse.GetValue(repositoryUpdateOption), out bool repositoryUpdate);
            // Likewise: on, the documented default (DESIGN-external-repository §9.2).
            _ = TryParseOnOff(parse.GetValue(repositoryCheckOption), out bool repositoryCheck);
            // No --repository is the output directory (null), never an empty search space.
            string[]? repositories = parse.GetValue(repositoryOption) is { Length: > 0 } named ? named : null;

            return new CliOptions(
                source, parse.GetValue(outputOption), nistName, std,
                parse.GetValue(copyOption) ?? [], parse.GetValue(runOption), parse.GetValue(permissiveOption),
                signEncoding, sourceFormat, parse.GetValue(flagExtensionsOption), numericDisplay, repositoryUpdate,
                repositories, parse.GetValue(referenceOption) ?? [], repositoryCheck);
        }

        // The root's OWN action: a bare `cobol <source>` compiles.
        root.SetAction(parse => RunCompile(Resolve(parse)));

        return (root, Resolve);
    }

    /// <summary>The batch-check verb's own command tree, <c>cobol check-batch &lt;manifest&gt;</c> (see <see cref="Parse"/>):
    /// a no-emit BATCH check (the INV-1 continuity sweep fast path) that parses, edition-validates and bind-checks many
    /// (source, edition) entries in ONE warm process, skipping the Roslyn backend and the ~2 s-per-file <c>dotnet</c>
    /// cold start that made the sweep ~1400 cold full compiles. Its root carries no argument, so the usage line and the
    /// grammar are exactly the subcommand's.</summary>
    internal static RootCommand BuildCheckBatchParser()
    {
        var manifestArgument = new Argument<string>("manifest")
        {
            Description = "A TSV file; each line `source<TAB>std<TAB>nist-name<TAB>permissive(0|1)` is "
                + "parse+bind-checked (no Roslyn emit). Prints `source<TAB>std<TAB>PASS|FAIL` per line.",
        };
        var checkBatch = new Command(CheckBatchName, CheckBatchDescription)
        {
            manifestArgument,
        };
        checkBatch.SetAction(parse => RunCheckBatch(parse.GetValue(manifestArgument)!));
        var root = new RootCommand(CheckBatchDescription);
        root.Subcommands.Add(checkBatch);
        return root;
    }

    private const string CheckBatchDescription =
        "Parse + edition-validate + bind-check many (source, edition) entries in one process (no Roslyn emit).";

    /// <summary>Parse + edition-validate + bind-check every entry of a TSV manifest in one warm process, in
    /// parallel (CompilerDriver.Compile is proven concurrency-safe — ConcurrentCompilationTests). Each entry
    /// uses the no-emit CheckOnly path (no Roslyn backend, no file writes), so what were ~1400 cold-`dotnet`
    /// full compiles collapse to a single parallel in-process pass. Prints one `source&lt;TAB&gt;std&lt;TAB&gt;
    /// PASS|FAIL` line per entry and always exits 0 — the per-entry verdicts are the output the caller aggregates
    /// (e.g. scripts/version-continuity-sweep.sh classifies SKIP85 / OK / BREAKS).</summary>
    private static int RunCheckBatch(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            Console.Error.WriteLine($"error: manifest not found: {manifestPath}");
            return 66;
        }
        string[] entries = File.ReadAllLines(manifestPath).Where(l => l.Trim().Length > 0).ToArray();
        var results = new System.Collections.Concurrent.ConcurrentBag<string>();
        Parallel.ForEach(entries, line => results.Add(CheckBatchVerdict(line)));
        foreach (string line in results)
            Console.Out.WriteLine(line);
        return 0;
    }

    /// <summary>The verdict line of one check-batch manifest entry, <c>source TAB std TAB PASS|FAIL</c>. An absent std field is
    /// the 2023 default; a PRESENT field that is not an edition (<see cref="TryParseStd"/>) is a FAIL entry that compiles
    /// nothing, never silently checked as 2023 (kb/Work PB4126).</summary>
    internal static string CheckBatchVerdict(string manifestLine)
    {
        string[] f = manifestLine.Split('\t');
        string source = f[0];
        bool stdGiven = f.Length > 1 && f[1].Length > 0;
        int std = 2023;
        if (stdGiven && !TryParseStd(f[1], out std))
            return $"{source}\t{f[1]}\tFAIL";
        string? nist = f.Length > 2 && f[2].Length > 0 ? f[2] : null;
        bool permissive = f.Length > 3 && f[3] == "1";
        var r = CompilerDriver.Compile(new CompilerDriver.Options(
            source, NistTestName: nist, DialectLevel: std, Permissive: permissive, CheckOnly: true));
        return $"{source}\t{std}\t{(r.Success ? "PASS" : "FAIL")}";
    }

    /// <summary>The spellings <c>--std</c> accepts: the years of <see cref="EditionInfo.All"/>, the one list of the editions this
    /// compiler targets, written as plain decimal years, so <c>COBOL-2002</c>, <c>+85</c> and an empty value are refused. Derived,
    /// never retyped: an edition added to <see cref="EditionInfo.All"/> is accepted here with no second edit.</summary>
    internal static readonly string[] StdSpellings = EditionInfo.All.ToArray()
        .Select(year => year.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray();

    /// <summary>Parse a <c>--std</c> value (or a check-batch manifest's std field) to its edition year; an absent value answers
    /// false.</summary>
    internal static bool TryParseStd(string? value, out int year)
    {
        year = 0;
        return value is not null && Array.IndexOf(StdSpellings, value) >= 0 && int.TryParse(value, out year);
    }

    /// <summary>The spellings an on/off option (<c>--repository-update</c>, <c>--repository-check</c>) accepts.</summary>
    internal static readonly string[] OnOffSpellings = ["on", "off"];

    /// <summary>Parse an on/off option's value (case-insensitive); an absent value is the default, on, and answers
    /// false.</summary>
    internal static bool TryParseOnOff(string? value, out bool on)
    {
        on = !string.Equals(value, "off", StringComparison.OrdinalIgnoreCase);   // not a COBOL word: a command-line option value
        return value is not null && (!on || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase));   // not a COBOL word: a command-line option value
    }

    /// <summary>The validator of an on/off option named <paramref name="option"/>: any other value is an argument error.</summary>
    private static void ValidateOnOff(System.CommandLine.Parsing.OptionResult result, string option)
    {
        if (result.GetValueOrDefault<string?>() is { } v && !TryParseOnOff(v, out _))
            result.AddError($"{option} must be one of {string.Join(", ", OnOffSpellings)} (got {v}).");
    }

    /// <summary>Compile per the resolved options, print diagnostics, and map the outcome to a POSIX exit code.</summary>
    private static int RunCompile(CliOptions options)
    {
        var result = CompilerDriver.Compile(new CompilerDriver.Options(
            options.SourcePath, options.OutputPath, options.NistTestName, options.DialectLevel, options.CopyPaths,
            options.Permissive, SignEncoding: options.SignEncoding, SourceFormat: options.SourceFormat,
            FlagExtensions: options.FlagExtensions, NumericDisplay: options.NumericDisplay,
            RepositoryUpdate: options.RepositoryUpdate, RepositoryPaths: options.RepositoryPaths,
            ReferencePaths: options.ReferencePaths, RepositoryCheck: options.RepositoryCheck));

        // What the compilation itself transferred — the lines of its DISPLAY directives (ISO §7.3.12.4 GR1, DOC-A.1-53,
        // kb/Work PB1538) — each to the compiler stream it names, in the order the directives were met.
        foreach (var transfer in result.CompileOutput)
            (transfer.Stream == WiseOwl.Cobol.Frontend.Diagnostics.CompileOutputStream.Error ? Console.Error : Console.Out)
                .WriteLine(transfer.Text);

        // Edition warnings (obsolete/archaic 0903 flags; removed constructs under --permissive) print to stderr
        // ALWAYS — success or failure — so migration users see them without a failing build (P2.1).
        foreach (string w in result.Warnings)
            Console.Error.WriteLine($"  {w}");

        if (!result.Success)
        {
            if (result.Status == CompilerDriver.Outcome.BackendError)
                Console.Error.WriteLine(BackendFailureHeadline(result.GeneratedCsPath));
            foreach (string e in result.Errors)
                Console.Error.WriteLine($"  {e}");

            return ExitCode(result.Status);
        }

        if (options.Run && result.Library)
        {
            // A group with no main program is a library (DESIGN-external-repository §7.2): there is nothing to launch.
            Console.Error.WriteLine($"error: --run: {result.OutputDll} is a library (the source has no program to run as "
                + "the main program); a .NET host calls its host entries.");
            return LibraryRunExitCode;
        }
        return options.Run ? Run(result.OutputDll) : 0;
    }

    /// <summary>The line that introduces a backend failure's diagnostics. It names the generated C# only when there is a file to open: a
    /// compilation that failed before it wrote any C# (an expansion assembly's, for one) has no path, and a path the user cannot open is
    /// worse than none (kb/Work PB4457).</summary>
    internal static string BackendFailureHeadline(string? generatedCsPath) =>
        generatedCsPath is { Length: > 0 } && File.Exists(generatedCsPath)
            ? $"error: backend compilation failed (generated C# at {generatedCsPath}):"
            : "error: backend compilation failed:";

    /// <summary>The exit status of <c>--run</c> given for a library: POSIX sysexits EX_USAGE, a usage error. The manual's
    /// command-line page lists it and <c>UserManualDriftTests</c> reads it from here.</summary>
    internal const int LibraryRunExitCode = 64;

    /// <summary>The process exit code of a compile outcome, POSIX sysexits: 0 success, 66 no input, 73 an output that cannot be
    /// created (a path naming no file in a directory, or a file the host refuses to write; kb/Work PB4502), 65 a data/format
    /// error IN THE SOURCE (a parse error or a binding/conformance diagnostic — the program is wrong), 70 an internal
    /// software error (the backend refused the C# this compiler generated — the compiler is wrong). Every outcome
    /// is named and an unnamed one throws, never a silent 70: the binding phase's refusal of the source once fell to
    /// that default and reported a wrong program as a compiler failure (kb/Work PB3175).</summary>
    internal static int ExitCode(CompilerDriver.Outcome status) => status switch
    {
        CompilerDriver.Outcome.Success => 0,
        CompilerDriver.Outcome.SourceNotFound => 66,
        CompilerDriver.Outcome.OutputNotWritable => 73,
        CompilerDriver.Outcome.FrontendError or CompilerDriver.Outcome.BindError => 65,
        CompilerDriver.Outcome.BackendError => 70,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "an outcome with no exit code"),
    };

    /// <summary>Launch the compiled assembly via the shared <c>dotnet</c> host and forward its exit code.</summary>
    private static int Run(string dllPath)
    {
        var psi = new ProcessStartInfo("dotnet", $"\"{dllPath}\"") { UseShellExecute = false };
        using var proc = Process.Start(psi);
        proc!.WaitForExit();
        return proc.ExitCode;
    }
}
