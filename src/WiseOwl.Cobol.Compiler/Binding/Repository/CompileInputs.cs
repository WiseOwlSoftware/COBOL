// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Expressions;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding.Repository;

/// <summary>
/// ⛔ THE ONE PLACE A <see cref="CompileState"/> IS PRODUCED (DESIGN-external-repository §6.1; kb/Work PB2104): the compile-time
/// inputs of one compilation group that change what its source means — the per-compilation <see cref="EditionContext"/> (the
/// edition and <c>--permissive</c>, <c>--sign-encoding</c>, <c>--numeric-display</c>, <c>--flag-extensions</c>), the
/// <c>&gt;&gt;COBOL-WORDS</c> map and the four directive folds and the compilation-variable table that
/// <see cref="BindSession"/> carries — gathered where the binder has them all, and asked for the state at one source line.
/// <para>The state at a line is the state a source element that begins on that line is bound under: the directives fold
/// strictly before the line (a directive occupies its own line and governs the text that follows, ISO §2023:7.3.4 GR5), so a skeleton
/// states the checking and propagation its definition was written under, not the group's last directive.</para>
/// <para><c>CompileStateDriftTests</c> holds the members of <c>CompilerDriver.Options</c> and <c>BindSession</c> against this
/// type and <see cref="CompileState"/>: a new option or directive state is red until it is a field here or is named as
/// changing no meaning.</para>
/// </summary>
internal sealed record CompileInputs(EditionContext Edition, CobolWordsMap Words, TurnState Turn, PropagateState Propagate,
    RefModZeroLengthState RefModZeroLength, LeapSecondState LeapSecond, DirectiveTimeline<CompilationVariableEvent> Variables)
{
    /// <summary>The state a source element beginning on <paramref name="line"/> (a resultant line, the parse tokens' frame) is
    /// compiled under.</summary>
    public CompileState At(int line) => new(Edition.DialectLevel, Edition.Permissive, Edition.SignEncoding, Edition.FlagExtensions,
        RepositoryVocabulary.Persisted(Edition.NumericDisplay), Words.DirectiveText, FoldsAt(line), VariablesAt(line));

    /// <summary><c>&gt;&gt;TURN</c>, <c>&gt;&gt;PROPAGATE</c>, <c>&gt;&gt;REF-MOD-ZERO-LENGTH</c> and <c>&gt;&gt;LEAP-SECOND</c> in
    /// force at <paramref name="line"/>, as the directive lines that state them (ISO §2023:7.3.25, §2023:7.3.21, §2023:7.3.23, §2023:7.3.17), in a
    /// fixed order: replayed over a default state they leave this one.</summary>
    private string FoldsAt(int line)
    {
        var text = new StringBuilder();
        foreach (var (ec, file, on, withLocation) in Turn.DirectivesInForceAt(line))
            text.Append(">>TURN ").Append(ec).Append(file is null ? "" : " " + file).Append(" CHECKING ")
                .Append(on ? (withLocation ? "ON WITH LOCATION" : "ON") : "OFF").Append('\n');
        if (Propagate.IsOnAt(line)) text.Append(">>PROPAGATE ON\n");
        if (!RefModZeroLength.IsUnspecifiedAt(line)) text.Append(RefModZeroLength.IsOnAt(line) ? ">>REF-MOD-ZERO-LENGTH ON\n" : ">>REF-MOD-ZERO-LENGTH OFF\n");
        if (LeapSecond.IsOnAt(line)) text.Append(">>LEAP-SECOND ON\n");
        return text.ToString();
    }

    /// <summary>The compilation variables defined at <paramref name="line"/> (ISO §2023:7.3.11.4 GR1), as <c>&gt;&gt;DEFINE</c> lines in
    /// the order their names were first defined: the value in force, as the operand that stated it.</summary>
    private string VariablesAt(int line)
    {
        var text = new StringBuilder();
        foreach (string name in Variables.Where(e => e.Line < line).Select(e => e.Name).Distinct(CobolNames.Comparer))
            if (Variables.DefinitionAt(name, line) is { Value: { } value } definition)
                text.Append(">>DEFINE ").Append(name).Append(" AS ").Append(Operand(definition, value)).Append(" OVERRIDE\n");
        return text.ToString();
    }

    private static string Operand(CompilationVariableEvent definition, CtValue value) =>
        definition.Written.Length > 0 ? definition.Written
        : value.WrittenLiteral ?? (value.Category == CtCategory.Numeric ? value.Text : $"\"{value.Text.Replace("\"", "\"\"")}\"");
}
