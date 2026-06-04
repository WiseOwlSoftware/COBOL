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

    /// <summary>Names of "trivial" paragraphs — those with no statements, or whose statements are solely
    /// bare EXIT/CONTINUE (both lower to BoundExitStatement) — i.e. usable as a COBOL "common end point"
    /// (ISO §14.9.17). A declarative section's USE procedure returns at its LAST such
    /// paragraph (the designated exit), NOT the section's physical last paragraph, so the handler's
    /// <c>GO TO exit-para</c> returns to the I/O continuation instead of falling through into an abort/
    /// termination tail that some CCVS declaratives place after the exit paragraph in the same section (the
    /// SQ212A bug). When the exit paragraph IS the section's last paragraph (the common case — e.g. an empty
    /// <c>END-DECLS.</c> or <c>EXIT-PARA. EXIT.</c> just before END DECLARATIVES) this equals the section's
    /// last paragraph, so behaviour is unchanged.</summary>
    public HashSet<string> ExitPointParagraphs { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Names of paragraphs that contain a run-unit/program terminator (STOP RUN, EXIT PROGRAM, or
    /// GOBACK). Used together with <see cref="ExitPointParagraphs"/> to detect a declarative section's
    /// "termination tail": when a terminating paragraph follows the section's last trivial exit paragraph,
    /// the USE procedure returns at that exit paragraph rather than falling through into the tail (the
    /// SQ212A bug). Without a trailing terminator the section's last paragraph remains the exit, so the
    /// common case — and CCVS declaratives whose last paragraph IS the handler's exit (SQ133A/SQ141A) — is
    /// unchanged.</summary>
    public HashSet<string> TerminatingParagraphs { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Maps ParagraphSymbol → IrMethod for section-qualified disambiguation.</summary>
    public Dictionary<ParagraphSymbol, IrMethod> ParagraphSymbolMethods { get; } = new();
    /// <summary>Maps ParagraphSymbol → paragraph index for section-qualified disambiguation.</summary>
    public Dictionary<ParagraphSymbol, int> ParagraphSymbolIndices { get; } = new();

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

    // ── Inherited GLOBAL USE declaratives (from containing programs) ──
    // GLOBAL USE AFTER ERROR declaratives declared in this program's containing programs (ISO §14.9.49.4
    // GR4 / §8.4.6.2.2). When an I/O statement here raises an exception this program has no USE declarative
    // for, the applicable containing-program GLOBAL declarative is dispatched at runtime via
    // GlobalUseDeclarativeRegistry. Scope: -1 file-name (FileName set); 0/1/2/3 INPUT/OUTPUT/I-O/EXTEND.
    // Empty for a top-level program or one whose ancestors declare no GLOBAL USE declarative.

    public IReadOnlyList<(int Scope, string? FileName)> InheritedGlobalUseDeclaratives { get; set; }
        = System.Array.Empty<(int, string?)>();

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
