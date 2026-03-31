// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Generated;

namespace CobolSharp.Compiler.Semantics.Bound.Binding;

/// <summary>
/// Shared state for all bound tree binding passes.
/// Constructed by BoundTreeBuilder and passed to every binder.
/// Owns all state that was formerly scattered as private fields across BoundTreeBuilder.cs.
/// </summary>
internal sealed class BindingContext
{
    // ── Core services ──

    public SemanticModel Semantic { get; }
    public DiagnosticBag Diagnostics { get; }
    public CompilationOptions Options { get; }

    // ── Built during visit ──

    public List<BoundParagraph> Paragraphs { get; } = new();

    // ── Static classification ──

    public static readonly HashSet<string> AlphanumericFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "LOWER-CASE", "UPPER-CASE", "REVERSE", "TRIM", "CONCATENATE",
        "SUBSTITUTE", "CHAR", "CURRENT-DATE", "WHEN-COMPILED"
    };

    // ── Binder references (set after construction) ──

    public ProcedureNameResolver ProcedureName { get; set; } = null!;
    public ExpressionBinder Expression { get; set; } = null!;
    public ConditionBinder Condition { get; set; } = null!;
    public ArithmeticStatementBinder Arithmetic { get; set; } = null!;
    public DataStatementBinder Data { get; set; } = null!;
    public ControlFlowBinder ControlFlow { get; set; } = null!;
    public FileIoBinder FileIo { get; set; } = null!;
    public CallBinder Call { get; set; } = null!;
    public StringStatementBinder String { get; set; } = null!;

    // ── Recursive statement binding delegate ──
    // Allows extracted binders to call back into BoundTreeBuilder.BindStatement
    // without depending on BoundTreeBuilder directly.

    public Func<CobolParserCore.StatementContext, BoundStatement?> BindStatement { get; set; } = null!;

    // ── Expression typing delegate ──
    // Allows extracted binders to call BoundTreeBuilder.Typed<T>().

    public Func<BoundExpression, BoundExpression> Typed { get; set; } = null!;

    // ── Constructor ──

    public BindingContext(SemanticModel semantic, DiagnosticBag diagnostics, CompilationOptions options)
    {
        Semantic = semantic;
        Diagnostics = diagnostics;
        Options = options;
    }
}
