// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Repository;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Repository;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A SKELETON PUBLISHES EVERY COMPILE-TIME INPUT THAT CHANGES WHAT ITS SOURCE MEANS, AND THE LIST IS EXHAUSTIVE BY CONSTRUCTION
/// (DESIGN-external-repository §6.1, §17.2; kb/Work PB2104). An expansion is made by a compilation that is not the one the skeleton
/// was written for, so the skeleton states the edition, the options, the words map, the directive folds and the compilation variables
/// it was compiled under (<see cref="CompileState"/>) and the expansion is made under those. A member of
/// <c>CompilerDriver.Options</c> or of <c>BindSession</c> added without one of them is a skeleton that silently forgets the new input:
/// this test is red until the member is a <see cref="CompileState"/> field or is named here, with its reason, as changing no meaning.
/// </summary>
public sealed class CompileStateDriftTests
{
    /// <summary>Each <c>CompilerDriver.Options</c> member that decides what a source means → the <see cref="CompileState"/> field that holds it.</summary>
    private static readonly Dictionary<string, string> OptionsMeaning = new()
    {
        [nameof(CompilerDriver.Options.DialectLevel)] = nameof(CompileState.Edition),
        [nameof(CompilerDriver.Options.Permissive)] = nameof(CompileState.Permissive),
        [nameof(CompilerDriver.Options.SignEncoding)] = nameof(CompileState.SignEncoding),
        [nameof(CompilerDriver.Options.FlagExtensions)] = nameof(CompileState.FlagExtensions),
        [nameof(CompilerDriver.Options.NumericDisplay)] = nameof(CompileState.NumericDisplay),
    };

    /// <summary>Each <c>CompilerDriver.Options</c> member that changes no meaning, and why not.</summary>
    private static readonly Dictionary<string, string> OptionsNotSemantic = new()
    {
        [nameof(CompilerDriver.Options.SourcePath)] = "harness naming: where the text came from",
        [nameof(CompilerDriver.Options.OutputPath)] = "harness naming: where the assembly goes",
        [nameof(CompilerDriver.Options.NistTestName)] = "harness naming: the NIST placeholder preprocessing's test name",
        [nameof(CompilerDriver.Options.CopyPaths)] = "the recorded source is the text AFTER text manipulation, so the library search never reaches it",
        [nameof(CompilerDriver.Options.SourceFormat)] = "the recorded source is free-form token text, so the reference format it began in never reaches it",
        [nameof(CompilerDriver.Options.CheckOnly)] = "no output: it decides whether anything is emitted, not what a source means",
        [nameof(CompilerDriver.Options.RepositoryUpdate)] = "what the build writes, not what the source means",
        [nameof(CompilerDriver.Options.RepositoryPaths)] = "where the build reads the repository, not what the source means",
        [nameof(CompilerDriver.Options.ReferencePaths)] = "where the build reads the repository, not what the source means",
        [nameof(CompilerDriver.Options.RepositoryCheck)] = "whether the build flags differences, not what the source means",
        ["PublishAs"] = "the identity an expansion's own compilation publishes under; the expansion's key already states what it means",
    };

    /// <summary>Each <c>BindSession</c> member that carries a directive state or an option → the <see cref="CompileState"/> field that holds it.</summary>
    private static readonly Dictionary<string, string> SessionMeaning = new()
    {
        ["Turn"] = nameof(CompileState.DirectiveFolds),
        ["Propagate"] = nameof(CompileState.DirectiveFolds),
        ["RefModZeroLength"] = nameof(CompileState.DirectiveFolds),
        ["LeapSecond"] = nameof(CompileState.DirectiveFolds),
        ["CobolWords"] = nameof(CompileState.CobolWordsMap),
        ["CompilationVariables"] = nameof(CompileState.CompilationVariables),
        ["Edition"] = "the per-compilation EditionContext: the options above (edition, permissive, sign encoding, numeric display, flag-extensions)",
    };

    /// <summary>Each other <c>BindSession</c> member, and why it is no input of meaning.</summary>
    private static readonly Dictionary<string, string> SessionNotSemantic = new()
    {
        ["DirectiveSites"] = "the positions the >>TURN / >>PUSH / >>POP bans read, recorded in the source itself",
        ["Retypes"] = "re-derived from the edition and the words map by the re-parse (FragmentParse.ParseUnit)",
        ["OoClasses"] = "the group's class table: bound, not an input",
        ["Repository"] = "the group's resolver: bound, not an input",
        ["SearchSpace"] = "where the build reads the repository, not what the source means",
        ["External"] = "the group's repository provider: bound, not an input",
        ["ClaimScans"] = "a scan cache of the binder",
        ["Sources"] = "the parse context each unit was bound from (kb/Work PB2398): made by the parse, not an input",
    };

    private const System.Reflection.BindingFlags All = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

    [Fact]
    public void EveryOptionsMember_IsACompileStateFieldOrNamedNotSemantic()
    {
        var members = typeof(CompilerDriver.Options).GetProperties(All).Select(p => p.Name)
            .Where(n => n is not ("EqualityContract")).ToHashSet();
        var known = OptionsMeaning.Keys.Concat(OptionsNotSemantic.Keys).ToHashSet();
        Assert.True(members.Count >= 14, "the scan found no Options members: it measured nothing");
        Assert.Equal([], members.Except(known).Order(StringComparer.Ordinal).ToList());   // a new option: a CompileState field, or its reason here
        Assert.Equal([], known.Except(members).Order(StringComparer.Ordinal).ToList());   // a stale entry
    }

    [Fact]
    public void EveryBindSessionMember_IsACompileStateFieldOrNamedNotSemantic()
    {
        var members = typeof(BindSession).GetProperties(All).Select(p => p.Name).ToHashSet();
        var known = SessionMeaning.Keys.Concat(SessionNotSemantic.Keys).ToHashSet();
        Assert.True(members.Count >= 10, "the scan found no BindSession members: it measured nothing");
        Assert.Equal([], members.Except(known).Order(StringComparer.Ordinal).ToList());
        Assert.Equal([], known.Except(members).Order(StringComparer.Ordinal).ToList());
    }

    [Fact]
    public void EveryCompileStateField_HoldsAnInput()
    {
        var held = OptionsMeaning.Values.Concat(SessionMeaning.Values).ToHashSet();
        Assert.Equal([], typeof(CompileState).GetProperties().Select(p => p.Name).Where(n => n != "EqualityContract" && !held.Contains(n)).ToList());
    }

    [Fact]
    public void TheStateAtALine_IsTheOptionsTheWordsTheFoldsAndTheVariables()
    {
        var edition = new EditionContext(2014, permissive: true)
            { SignEncoding = SignEncoding.Ascii, NumericDisplay = NumericDisplayForm.Digits, FlagExtensions = true };
        var words = new CobolWordsMap([new CobolWordsOp(CobolWordsAction.Equate, "DISPLAY", "SHOW", default),
            new CobolWordsOp(CobolWordsAction.Reserve, null, "FIDDLE", default)]);
        var inputs = new CompileInputs(edition, words, TurnState.Empty,
            PropagateState.Build([new PropagateEvent(3, true)]), RefModZeroLengthState.Build([new RefModZeroLengthEvent(4, false)]),
            LeapSecondState.Build([new LeapSecondEvent(5, true)]), DirectiveTimeline<CompilationVariableEvent>.Empty);

        var before = inputs.At(3);   // a directive governs the text that FOLLOWS it (ISO §7.3.4 GR5)
        var after = inputs.At(10);

        Assert.Equal(new CompileState(2014, true, SignEncoding.Ascii, true, RepositoryNumericDisplay.Digits,
            ">>COBOL-WORDS EQUATE \"DISPLAY\" WITH \"SHOW\"\n>>COBOL-WORDS RESERVE \"FIDDLE\"\n", "", ""), before);
        Assert.Equal(">>PROPAGATE ON\n>>REF-MOD-ZERO-LENGTH OFF\n>>LEAP-SECOND ON\n", after.DirectiveFolds);
        Assert.Equal(before with { DirectiveFolds = after.DirectiveFolds }, after);
    }

    [Fact]
    public void AWordsMap_RoundTripsThroughItsDirectiveText()
    {
        var words = new CobolWordsMap([new CobolWordsOp(CobolWordsAction.Equate, "DISPLAY", "SHOW", default),
            new CobolWordsOp(CobolWordsAction.Undefine, "GOBACK", null, default),
            new CobolWordsOp(CobolWordsAction.Substitute, "ADD", "PLUS", default),
            new CobolWordsOp(CobolWordsAction.Reserve, null, "FIDDLE", default)]);
        var again = CobolWordsMap.FromDirectiveText(words.DirectiveText);
        Assert.NotNull(again);
        Assert.Equal(words.DirectiveText, again!.DirectiveText);
        Assert.Equal(words.Synonyms, again.Synonyms);
        Assert.Equal(words.DeReserved.Order().ToList(), again.DeReserved.Order().ToList());
        Assert.Equal(words.Reserved.Order().ToList(), again.Reserved.Order().ToList());
        Assert.Same(CobolWordsMap.Empty, CobolWordsMap.FromDirectiveText(""));
        Assert.Null(CobolWordsMap.FromDirectiveText("not a directive\n"));
    }
}
