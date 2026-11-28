// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.CommandLine;
using System.Diagnostics;
using WiseOwl.Cobol.Compiler;
using WiseOwl.Cobol.Compiler.Binding.Model;
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
/// Usage: <c>cobol &lt;source.cob&gt; [-o out.dll] [--nist [NAME]] [--std YEAR] [--permissive]
/// [--copy DIR] [--run] [--sign-encoding ibm|ascii] [--numeric-display decimal|digits] [--source-format fixed|free|auto] [--flag-extensions] [--repository-update on|off]
/// [--repository DIR] [--reference FILE.dll] [--repository-check on|off] [--listing [on|off]] [--listing-output BASE]
/// [--listing-format text|json|html]... [--listing-sections LIST] [--listing-page-length N] [--listing-page-width N]</c>. Every value option accepts BOTH <c>--opt value</c> and <c>--opt=value</c>; no
/// option can swallow a following flag. The generated C# is written next to the output assembly
/// (<c>&lt;name&gt;.g.cs</c>) so the translation is directly inspectable.
/// </remarks>
internal static class Program
{
    private static int Main(string[] args)
    {
        // ⛔ BEFORE ANYTHING IS WRITTEN. Diagnostics carry ISO citations — `(ISO §2023:11.10.3 SR5–6)` — and on a
        // default Windows console (cp437) the section sign left this process as the single byte 0x15 and the
        // en dash as '-', in every redirected capture and every CI log, because this entry point never set an
        // encoding while WiseOwl.Cobol.Runtime's did (kb/Work PB899 — the two-arm dispatch with one arm fixed). One
        // rule, one place: WiseOwl.Cobol.Runtime.IO.StandardStreams, which the other process entry, a compiled program's
        // Main (ProgramTable.RunModule), calls too; a host entry never does (kb/Work PB3193).
        WiseOwl.Cobol.Runtime.IO.StandardStreams.EnsureUtf8();
        return RunGuarded(args, () => Parse(args).Invoke(InvocationWithoutDefaultHandler), Console.Error);
    }

    /// <summary>System.CommandLine's own handler catches an exception an action throws, prints its text and answers 1, the status of a
    /// generic failure: switched off, the exception reaches <see cref="RunGuarded"/>, the one handler (kb/Work PB6060).</summary>
    internal static readonly InvocationConfiguration InvocationWithoutDefaultHandler = new() { EnableDefaultExceptionHandler = false };

    /// <summary>The exit status of a compiler fault: POSIX sysexits EX_SOFTWARE, the code <see cref="ExitCode"/> gives the backend's
    /// refusal of the C# this compiler generated. Both say the compiler is wrong, never the program: a refusal of the source is 65.</summary>
    internal const int InternalErrorExitCode = 70;

    /// <summary>⛔ THE ONE LAST LINE OF DEFENCE of every verb (kb/Work PB6060): the compile, <c>--run</c>, the listing and both batch verbs
    /// all start from <see cref="Main"/>, and an exception a compiler pass throws used to leave it as the runtime's raw unhandled-exception
    /// text, which a user cannot tell from a refusal of the program. This names the fault (the command line, and the pass: the innermost
    /// compiler type on the stack) and prints the whole exception, stack included, so nothing is hidden or swallowed; the status is
    /// <see cref="InternalErrorExitCode"/>. It is not a licence to throw: the fault is a defect of its pass. A run's own exit status passes
    /// through unchanged, and an exception of the compiled program is its own process's, never this one's.</summary>
    internal static int RunGuarded(IReadOnlyList<string> args, Func<int> invoke, TextWriter error)
    {
        try
        {
            return invoke();
        }
        catch (Exception e)
        {
            error.WriteLine($"internal compiler error: {FaultingPass(e)} threw {e.GetType().Name} while running `cobol {string.Join(' ', args)}`."
                + " This is a defect in the compiler, not in the COBOL source; please report it with the source and this text.");
            error.WriteLine(e.ToString());
            return InternalErrorExitCode;
        }
    }

    /// <summary>The compiler type that threw: the innermost stack frame in a <c>WiseOwl.Cobol</c> namespace (past the parallel-loop wrapper's
    /// <see cref="AggregateException"/>), else the exception's own declaring site, else <c>the compiler</c>.</summary>
    private static string FaultingPass(Exception e)
    {
        Exception innermost = e;
        while (innermost is AggregateException { InnerExceptions: [var only] }) innermost = only;
        string? type = new StackTrace(innermost, fNeedFileInfo: false).GetFrames()
            .Select(f => f.GetMethod()?.DeclaringType)
            .FirstOrDefault(t => t?.Namespace is { } ns && ns.StartsWith("WiseOwl.Cobol", StringComparison.Ordinal))?.FullName;
        return type ?? "the compiler";
    }

    /// <summary>The name of the batch-check command, a verb whose first word is not a source file.</summary>
    internal const string CheckBatchName = "check-batch";

    /// <summary>The name of the parse-reading batch command (kb/Work PB6050), the other such verb.</summary>
    internal const string PointsBatchName = "points-batch";

    /// <summary>⛔ THE ONE ROUTE FROM A COMMAND LINE TO ITS PARSE (kb/Work PB4340). The compile and the batch check are
    /// two verbs, not a command and its child: <c>System.CommandLine</c> writes a parent command's arguments before
    /// a subcommand, both in the subcommand's usage line and in its grammar, so while check-batch was a subcommand of
    /// the compile root its help listed <c>&lt;source&gt;</c> and printed <c>cobol &lt;source&gt; check-batch
    /// &lt;manifest&gt;</c>, and <c>cobol x.cob check-batch m.tsv</c> ran the batch and ignored <c>x.cob</c>. Each verb
    /// is therefore its own command tree, and the first word chooses the tree.</summary>
    internal static ParseResult Parse(IReadOnlyList<string> args) => args switch
    {
        [CheckBatchName, ..] => BuildCheckBatchParser().Parse(args),
        [PointsBatchName, ..] => BuildPointsBatchParser().Parse(args),
        _ => BuildParser().Command.Parse(args),
    };

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
            Description = $"Target ISO edition: {string.Join(", ", StdSpellings[..^1])}, or {StdSpellings[^1]} (default: {EditionInfo.Latest.Year}, or {NistEdition} with --nist).",
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
        // (ISO §2023:13.18.52.4 GR4 / GR5 b). An unknown value is refused by the parser exactly as --std's is: a CLI
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
        // §2023:14.9.11.4 GR1 leaves the device conversion to the implementor; the default follows GnuCOBOL 3.2.0 (the
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

        // kb/Work PB1362: the reference format the source starts in. ISO §2023:7.3.24.3 GR2 makes FIXED form the default;
        // free form and the structural auto-detection are the implementor-defined selection mechanism §2023:4.2.10 GR3
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

        // kb/Work PB1525: ISO §2023:4.2.10 — "An implementation shall provide a warning mechanism that optionally may be
        // invoked by the user at compile time to indicate use of a nonstandard extension in a compilation group."
        // This is that mechanism. It is a pure opt-in: without it a compile prints exactly what it printed before.
        var flagExtensionsOption = new Option<bool>("--flag-extensions")
        {
            Description = "Warn (COBOL2894) at every use of a nonstandard extension this compiler accepts — a "
                + "construct ISO/IEC 1989 does not define (ISO 4.2.10). The register is docs/CONFORMANCE.md 3.2.",
        };

        // kb/Work PB2652 (owner decision D20): ISO §2023:4.2.2's warning mechanism is one "that optionally may be invoked by the user
        // at compile time", and this is the user's choice of which of its warnings to see. A name no diagnostic answers to is a CLI
        // argument error (WarningSuppression.Problem): a misspelling that muted nothing would pass for a warning the compiler kept.
        var suppressOption = new Option<string[]>("--suppress")
        {
            Description = "Do not report a warning (repeatable): NAME is a diagnostic code (COBOL2187), a diagnostic id or a suppress "
                + "key (recognized-not-implemented), as docs/DIAGNOSTICS.md lists them. An error is never suppressed.",
            Arity = ArgumentArity.OneOrMore,
            AllowMultipleArgumentsPerToken = false,   // `--suppress A --suppress B`, never grabbing the source positional
            HelpName = "NAME",
        };
        suppressOption.Validators.Add(result =>
        {
            foreach (string v in result.GetValueOrDefault<string[]>() ?? [])
                if (WiseOwl.Cobol.Frontend.Diagnostics.WarningSuppression.Problem(v) is { } problem)
                    result.AddError($"--suppress: {problem}.");
        });

        // kb/Work PB2100: ISO §2023:8.13 — "The implementor shall provide a mechanism that allows the user to specify whether to
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
        // order given (by default the output directory), then in the --reference assemblies in order (ISO §2023:12.3.8.4 GR10c) /
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
        // ISO §2023:8.13 — "The implementor shall provide a mechanism that allows the user to specify whether to flag differences
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

        // kb/Work PB5923 (DESIGN-listing.md section 8): the source listing. §1985:IV.7.1 requires "an output listing of the source
        // program in reference format" of each compiler, so it is ON by default at --std 85; the later editions leave it to the
        // implementor (§2023:7.3.18.3 GR1), so it is produced there on request. Every value is checked here, an argument error.
        var listingOption = new Option<string?>("--listing")
        {
            Description = $"Produce a source listing (BASE.lst): on (bare --listing) or off. Default: on at --std {ListingDefault(on: true)}, "
                + $"off at {ListingDefault(on: false)}.",
            Arity = ArgumentArity.ZeroOrOne,   // a bare `--listing` is on
            HelpName = string.Join("|", OnOffSpellings),
        };
        listingOption.Validators.Add(result => ValidateOnOff(result, "--listing"));
        var listingOutputOption = new Option<string?>("--listing-output")
        {
            Description = "Where the listing goes: BASE plus each format's extension; - writes the one format requested to standard "
                + "output (default: the output assembly's path without its extension). Implies --listing on.",
            HelpName = "BASE",
        };
        var listingFormatOption = new Option<string[]>("--listing-format")
        {
            Description = $"A listing format to write ({WiseOwl.Cobol.Listing.ListingOutput.Spellings}): text is BASE.lst, json is "
                + "BASE.lst.json (the versioned JSON document, with its published schema), html is BASE.lst.html (the hyperlinked "
                + "page). Repeatable; default text. Implies --listing on.",
            Arity = ArgumentArity.OneOrMore,
            AllowMultipleArgumentsPerToken = false,   // one format per occurrence: `--listing-format json a.cob` keeps the source
            HelpName = WiseOwl.Cobol.Listing.ListingOutput.Spellings,
        };
        listingFormatOption.Validators.Add(result =>
        {
            foreach (string v in result.GetValueOrDefault<string[]>() ?? [])
                if (!WiseOwl.Cobol.Listing.ListingOutput.TryParse(v, out _))
                    result.AddError($"--listing-format must be one of {WiseOwl.Cobol.Listing.ListingOutput.Spellings.Replace('|', ',')} (got {v}).");
        });
        var listingSectionsOption = new Option<string?>("--listing-sections")
        {
            Description = $"The listing's sections, a comma list of {WiseOwl.Cobol.Listing.ListingSections.Spellings} (default "
                + $"{WiseOwl.Cobol.Listing.ListingSections.StandardSpelling}, which is "
                + string.Join(",", WiseOwl.Cobol.Listing.ListingSectionSet.Standard.Each().Select(WiseOwl.Cobol.Listing.ListingSections.Spelling))
                + "). Implies --listing on.",
            HelpName = "LIST",
        };
        listingSectionsOption.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<string?>() is { } list && !WiseOwl.Cobol.Listing.ListingSections.TryParse(list, out _, out string? problem))
                result.AddError($"--listing-sections: {problem}.");
        });
        var listingPageLengthOption = new Option<int?>("--listing-page-length")
        {
            Description = $"Listing lines per page, the heading included; 0 = unpaginated (default {WiseOwl.Cobol.Listing.ListingRequest.DefaultPageLength}). Implies --listing on.",
            HelpName = "N",
        };
        listingPageLengthOption.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<int?>() is { } n && WiseOwl.Cobol.Listing.ListingRequest.PageLengthProblem(n) is { } problem)
                result.AddError($"--listing-page-length: {problem}.");
        });
        var listingPageWidthOption = new Option<int?>("--listing-page-width")
        {
            Description = $"Listing columns, {WiseOwl.Cobol.Listing.ListingRequest.MinimumPageWidth} to {WiseOwl.Cobol.Listing.ListingRequest.MaximumPageWidth} (default {WiseOwl.Cobol.Listing.ListingRequest.DefaultPageWidth}). Implies --listing on.",
            HelpName = "N",
        };
        listingPageWidthOption.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<int?>() is { } n && WiseOwl.Cobol.Listing.ListingRequest.PageWidthProblem(n) is { } problem)
                result.AddError($"--listing-page-width: {problem}.");
        });

        // The batch check is a separate tree (Parse), so this description is where the compile's help names it.
        var root = new RootCommand("cobol — translate a COBOL source unit to typed-native .NET (the Roslyn backend). "
            + $"`cobol {CheckBatchName} <manifest>` checks many sources without emitting (`cobol {CheckBatchName} --help`).")
        {
            sourceArgument, outputOption, nistOption, stdOption, permissiveOption, copyOption, runOption,
            signEncodingOption, numericDisplayOption, sourceFormatOption, flagExtensionsOption, suppressOption, repositoryUpdateOption,
            repositoryOption, referenceOption, repositoryCheckOption,
            listingOption, listingOutputOption, listingFormatOption, listingSectionsOption, listingPageLengthOption,
            listingPageWidthOption,
        };
        // Two options together (DESIGN-listing.md section 8): a listing written to standard output is written in one format.
        root.Validators.Add(result =>
        {
            if (result.GetValue(listingFormatOption) is { Length: > 0 } named
                && WiseOwl.Cobol.Listing.ListingRequest.FormatsProblem(result.GetValue(listingOutputOption), ListingFormatsOf(named)) is { } problem)
                result.AddError($"--listing-format: {problem}.");
        });

        CliOptions Resolve(ParseResult parse)
        {
            string source = parse.GetValue(sourceArgument)!;
            // The NIST test name is OPTIONAL: `--nist` present means NIST mode (GetResult is non-null even with
            // no value); an absent name is derived from the source file's base name (the CCVS convention).
            bool nistEnabled = parse.GetResult(nistOption) is not null;
            string? nistName = nistEnabled
                ? parse.GetValue(nistOption) ?? Path.GetFileNameWithoutExtension(source)
                : null;
            // --std wins when given; else the NIST CCVS corpus is COBOL-85, otherwise the latest edition (DEVLOG 519).
            // The validator above has already refused any other spelling, so a false here can only be the ABSENT option.
            int std = TryParseStd(parse.GetValue(stdOption), out int stdYear) ? stdYear
                : nistEnabled ? NistEdition : EditionInfo.Latest.Year;
            // The composition root's one construction of the edition: validated here, carried as a value from here on (kb/Work PB2619).
            var edition = EditionInfo.Of(std, parse.GetValue(permissiveOption));

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

            // The listing (DESIGN-listing.md section 8): --listing off wins; a bare --listing, --listing on, or any other listing
            // option asks for one; otherwise the edition decides (on at 85, §1985:IV.7.1).
            string? output = parse.GetValue(listingOutputOption);
            int? pageLength = parse.GetValue(listingPageLengthOption), pageWidth = parse.GetValue(listingPageWidthOption);
            bool listingNamed = parse.GetResult(listingOption) is not null;
            string[]? formats = parse.GetValue(listingFormatOption) is { Length: > 0 } namedFormats ? namedFormats : null;
            string? sections = parse.GetValue(listingSectionsOption);
            bool listingOn = listingNamed ? TryParseOnOff(parse.GetValue(listingOption), out bool on) ? on : true
                : output is not null || formats is not null || sections is not null || pageLength is not null || pageWidth is not null
                  || edition.RequiresSourceListing;
            // The validator above has already refused a list it cannot read, so a false here can only be the absent option.
            var sectionSet = sections is not null && WiseOwl.Cobol.Listing.ListingSections.TryParse(sections, out var namedSections, out _)
                ? namedSections : WiseOwl.Cobol.Listing.ListingSectionSet.Standard;
            var listing = listingOn
                ? new WiseOwl.Cobol.Listing.ListingRequest(output,
                    pageLength ?? WiseOwl.Cobol.Listing.ListingRequest.DefaultPageLength,
                    pageWidth ?? WiseOwl.Cobol.Listing.ListingRequest.DefaultPageWidth,
                    formats is null ? WiseOwl.Cobol.Listing.ListingFormats.Text : ListingFormatsOf(formats)) { Sections = sectionSet }
                : null;

            return new CliOptions(
                source, parse.GetValue(outputOption), nistName, edition,
                parse.GetValue(copyOption) ?? [], parse.GetValue(runOption),
                signEncoding, sourceFormat, parse.GetValue(flagExtensionsOption), numericDisplay, repositoryUpdate,
                repositories, parse.GetValue(referenceOption) ?? [], repositoryCheck, listing,
                parse.GetValue(suppressOption) is { Length: > 0 } suppress ? suppress : null);
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

    internal static RootCommand BuildPointsBatchParser()
    {
        var manifestArgument = new Argument<string>("manifest")
        {
            Description = "A TSV file; each line `source<TAB>std<TAB>source-format` is parsed under that edition and reference format.",
        };
        var framesOption = new Option<string>("--frames")
        {
            Description = "The grammar rules (comma-separated, lower camel case) whose parse nodes are read.",
            Required = true,
            HelpName = "RULES",
        };
        var operandsOption = new Option<string>("--operands")
        {
            Description = "The grammar rules (comma-separated) whose contexts, inside a node, stand as one placeholder token when they hold more than one.",
            Required = true,
            HelpName = "RULES",
        };
        var flatOption = new Option<string>("--flat")
        {
            Description = "The frames (comma-separated) whose nodes keep every token of their operands, none standing for its context.",
            DefaultValueFactory = _ => "",
            HelpName = "RULES",
        };
        var pointsBatch = new Command(PointsBatchName, PointsBatchDescription)
        {
            manifestArgument, framesOption, operandsOption, flatOption,
        };
        pointsBatch.SetAction(parse => RunPointsBatch(parse.GetValue(manifestArgument)!, new ParseNodeReader.Selection(
            SplitRules(parse.GetValue(framesOption)!), SplitRules(parse.GetValue(operandsOption)!), SplitRules(parse.GetValue(flatOption)!))));
        var root = new RootCommand(PointsBatchDescription);
        root.Subcommands.Add(pointsBatch);
        return root;
    }

    private const string PointsBatchDescription =
        "Parse many (source, edition) entries in one process and print, per entry, the token stream of each parse node of the "
        + "named rules as one JSON line (the conformance suite's coverage measure, DESIGN-conformance-suite.md section 3.4).";

    private static HashSet<string> SplitRules(string list) =>
        [.. list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    /// <summary>Read the parse of every entry of a TSV manifest in one warm process, in parallel, and print one JSON line per
    /// entry in the manifest's order: <c>{"source", "std", "parsed", "nodes": [{"rule", "line", "tokens": [[text, type], ...]}]}</c>.
    /// An entry whose std or source format is not one the compiler accepts, or whose source is missing, prints
    /// <c>"parsed": false</c> and no nodes, never a default; the exit status is 0 whatever the entries say, the lines being the answer.</summary>
    private static int RunPointsBatch(string manifestPath, ParseNodeReader.Selection selection)
    {
        if (!File.Exists(manifestPath))
        {
            Console.Error.WriteLine($"error: manifest not found: {manifestPath}");
            return 66;
        }
        string[] entries = File.ReadAllLines(manifestPath).Where(l => l.Trim().Length > 0).ToArray();
        var lines = new string[entries.Length];
        Parallel.For(0, entries.Length, i => lines[i] = PointsBatchLine(entries[i], selection));
        foreach (string line in lines) Console.Out.WriteLine(line);
        return 0;
    }

    /// <summary>The JSON line of one points-batch manifest entry (see <see cref="RunPointsBatch"/>).</summary>
    internal static string PointsBatchLine(string manifestLine, ParseNodeReader.Selection selection)
    {
        string[] f = manifestLine.Split('\t');
        string source = f[0];
        string std = f.Length > 1 ? f[1] : "";
        ParseNodeReader.Reading reading = new(false, []);
        if (TryParseStd(std, out int year) && File.Exists(source)
            && InitialReferenceFormatOption.TryParse(f.Length > 2 && f[2].Length > 0 ? f[2] : null, out var format))
        {
            reading = ParseNodeReader.Read(new CompilerDriver.Options(
                source, EditionInfo.Of(year), CopyPaths: [Path.GetDirectoryName(Path.GetFullPath(source))!],
                SourceFormat: format), selection);
        }
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            source,
            std,
            parsed = reading.Parsed,
            nodes = reading.Nodes.Select(n => new
            {
                rule = n.Rule,
                line = n.Line,
                tokens = n.Tokens.Select(t => new[] { t.Text, t.Type }),
            }),
        });
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
    /// the default edition (<see cref="EditionInfo.Latest"/>); a PRESENT field that is not an edition (<see cref="TryParseStd"/>)
    /// is a FAIL entry that compiles nothing, never silently checked at the default (kb/Work PB4126).</summary>
    internal static string CheckBatchVerdict(string manifestLine)
    {
        string[] f = manifestLine.Split('\t');
        string source = f[0];
        bool stdGiven = f.Length > 1 && f[1].Length > 0;
        int std = EditionInfo.Latest.Year;
        if (stdGiven && !TryParseStd(f[1], out std))
            return $"{source}\t{f[1]}\tFAIL";
        string? nist = f.Length > 2 && f[2].Length > 0 ? f[2] : null;
        bool permissive = f.Length > 3 && f[3] == "1";
        var r = CompilerDriver.Compile(new CompilerDriver.Options(
            source, EditionInfo.Of(std, permissive), NistTestName: nist, CheckOnly: true));
        return $"{source}\t{std}\t{(r.Success ? "PASS" : "FAIL")}";
    }

    /// <summary>The spellings <c>--std</c> accepts: the years of <see cref="EditionInfo.All"/>, the one list of the editions this
    /// compiler targets, written as plain decimal years, so <c>COBOL-2002</c>, <c>+85</c> and an empty value are refused. Derived,
    /// never retyped: an edition added to <see cref="EditionInfo.All"/> is accepted here with no second edit.</summary>
    internal static readonly string[] StdSpellings = EditionInfo.All.ToArray()
        .Select(year => year.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray();

    /// <summary>The edition of the NIST CCVS corpus, <c>--nist</c>'s default <c>--std</c>: the suite is COBOL-85's.</summary>
    internal const int NistEdition = 85;

    /// <summary>The <c>--std</c> spellings at which the listing is on (or off) by default — <see cref="EditionInfo.RequiresSourceListing"/>
    /// read over <see cref="EditionInfo.All"/>, so the help text cannot drift from the default it describes (kb/Work PB2619).</summary>
    private static string ListingDefault(bool on)
    {
        string[] years = StdSpellings.Where(y => EditionInfo.Of(int.Parse(y, System.Globalization.CultureInfo.InvariantCulture))
            .RequiresSourceListing == on).ToArray();
        return years.Length <= 1 ? string.Concat(years) : $"{string.Join(", ", years[..^1])} and {years[^1]}";
    }

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

    /// <summary>The formats <c>--listing-format</c> names; the option's validator has already refused any other spelling.</summary>
    private static WiseOwl.Cobol.Listing.ListingFormats ListingFormatsOf(IEnumerable<string> spellings)
    {
        WiseOwl.Cobol.Listing.ListingFormats formats = 0;
        foreach (string spelling in spellings)
            if (WiseOwl.Cobol.Listing.ListingOutput.TryParse(spelling, out var format)) formats |= format;
        return formats;
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
            options.SourcePath, options.Edition, options.OutputPath, options.NistTestName, options.CopyPaths,
            SignEncoding: options.SignEncoding, SourceFormat: options.SourceFormat,
            FlagExtensions: options.FlagExtensions, NumericDisplay: options.NumericDisplay,
            RepositoryUpdate: options.RepositoryUpdate, RepositoryPaths: options.RepositoryPaths,
            ReferencePaths: options.ReferencePaths, RepositoryCheck: options.RepositoryCheck, Listing: options.Listing,
            Suppress: options.Suppress));

        // `--listing-output -`: the one format requested goes to standard output (DESIGN-listing.md section 8), before the
        // diagnostics; the request admits exactly one format there (ListingRequest.FormatsProblem). It is streamed (kb/Work PB5941)
        // through Console.Out, the process's one standard output writer, whose encoding StandardStreams.EnsureUtf8 set from Main:
        // UTF-8 when redirected (the bytes the format's file would hold), the console's wide characters otherwise (PB5941 review R1-1).
        if (options.Listing is { WritesFiles: false } request && result.Listing is { } document)
            WiseOwl.Cobol.Listing.ListingOutput.Write(request.Formats, document, request, Console.Out);

        // What the compilation itself transferred — the lines of its DISPLAY directives (ISO §2023:7.3.12.4 GR1, DOC-A.1-53,
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
