// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.IR;
using CobolSharp.Compiler.Semantics;
using CobolSharp.Compiler.Semantics.Bound;

namespace CobolSharp.Compiler.CodeGen.Lowering;

/// <summary>
/// Shared mutable state for all lowering passes.
/// Constructed by the Binder and passed to every lowerer.
/// Owns all state that was formerly scattered as private fields across Binder.cs.
/// </summary>
internal sealed class LoweringContext
{
    // ── Core services ──

    public SemanticModel Semantic { get; }
    public IrValueFactory ValueFactory { get; }
    public DiagnosticBag Diagnostics { get; }
    public CompilationOptions Options { get; }

    // ── Paragraph mapping ──

    public Dictionary<string, IrMethod> ParagraphMethods { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> ParagraphIndices { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public List<string> ParagraphsByIndex { get; } = new();

    // ── ALTER support ──

    public Dictionary<string, int> AlterSlots { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public List<int> AlterDefaults { get; } = new();

    // ── Current-paragraph tracking ──

    public string? CurrentParagraphName { get; set; }
    public IrBasicBlock? CurrentSentenceEnd { get; set; }
    public IrBasicBlock? ParagraphEndBlock { get; set; }
    public int? SectionExitReturnIndex { get; set; }

    // ── PERFORM loop stacks ──

    public Stack<IrBasicBlock> PerformExitStack { get; } = new();
    public Stack<IrBasicBlock> PerformContinueStack { get; } = new();

    // ── Cache key allocator (for IrCachedLocation) ──

    private int _nextCacheKey;
    public int NextCacheKey() => _nextCacheKey++;

    // ── Lowerer references (set after construction) ──

    public LocationResolver Location { get; set; } = null!;
    public ExpressionLowerer Expression { get; set; } = null!;
    public ConditionLowerer Condition { get; set; } = null!;
    public ControlFlowLowerer ControlFlow { get; set; } = null!;
    public ArithmeticLowerer Arithmetic { get; set; } = null!;
    public DataMovementLowerer DataMovement { get; set; } = null!;
    public FileIoLowerer FileIo { get; set; } = null!;
    public StringLowerer String { get; set; } = null!;

    // ── Recursive statement lowering delegate ──
    // Allows extracted lowerers to call back into Binder.LowerStatement
    // without depending on the Binder class directly.

    public Func<BoundStatement, IrMethod, IrBasicBlock, IrBasicBlock> LowerStatement { get; set; } = null!;

    // ── Constructor ──

    public LoweringContext(SemanticModel semantic, DiagnosticBag diagnostics,
        CompilationOptions options, IrValueFactory valueFactory)
    {
        Semantic = semantic;
        Diagnostics = diagnostics;
        Options = options;
        ValueFactory = valueFactory;
    }
}
