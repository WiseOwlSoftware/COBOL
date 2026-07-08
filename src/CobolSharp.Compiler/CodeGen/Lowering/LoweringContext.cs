// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Diagnostics;
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

    // ── Data-model migration: the typed-vs-byte representation map (docs/RECORD_STRUCT_STORAGE_DESIGN.md) ──
    // Produced by RecordClassificationPass in Binder.Bind (after the bound tree is built). Consumed today only
    // by the soundness-invariant check; the Stage-3 typed flip consults it in LocationResolver to choose a
    // TypedFieldSlot vs ByteWindowSlot. Null when classification has not run (e.g. direct lowerer unit tests).
    public RecordClassification? Classification { get; set; }

    /// <summary>Items flipped to typed-native fields (S3/S5): DataSymbol → (emitted leaf field/member name, byte
    /// width, struct-instance name or null for a flat S3a field, and — S5 — the chain of intermediate nested-struct
    /// member names from the instance to the leaf's parent, empty for a flat S3b member). Populated by
    /// Binder.CollectTypedFields; consulted by LocationResolver to build an <see cref="IR.IrTypedFieldLocation"/>.
    /// Empty unless flipping is on.</summary>
    public Dictionary<DataSymbol, (string Name, int Width, string? Instance, IReadOnlyList<string>? MemberPath)>
        TypedFieldRefs { get; } = new(ReferenceEqualityComparer.Instance);

    /// <summary>Fixed <c>OCCURS</c> tables flipped to typed .NET array fields (S4): the table element DataSymbol →
    /// (array field name, element byte width, element count). Populated by Binder.CollectTypedFields; consulted by
    /// LocationResolver to produce an <see cref="IR.IrTypedElementLocation"/> for a subscripted reference. Empty
    /// unless flipping is on.</summary>
    public Dictionary<DataSymbol, (string Name, int Width, int Count)> TypedArrayRefs { get; } =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>Pointer items (Stage-4, docs/RECORD_STRUCT_STORAGE_DESIGN.md §10): a <c>USAGE POINTER</c> item or a
    /// <c>BASED</c> item → its emitted <c>static ManagedPointer _PTR_&lt;name&gt;</c> field name. Populated by
    /// Binder.CollectPointerFields (always-on, NOT gated by EnableTypedFields); consulted by the SET / compare /
    /// BASED-deref lowering to address the pointer field. Empty only when the program declares no pointers.</summary>
    public Dictionary<DataSymbol, string> PointerFieldRefs { get; } =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>OO object references (docs/OO_IMPLEMENTATION_DESIGN.md §E): a <c>USAGE OBJECT REFERENCE</c> item →
    /// its emitted <c>static _OBJ_&lt;name&gt;</c> field name. Populated by Binder.CollectObjectRefFields (always-on);
    /// consulted by the SET-NULL / INVOKE-RETURNING / INVOKE-target lowering. Empty unless the program declares
    /// object references.</summary>
    public Dictionary<DataSymbol, string> ObjectRefFieldRefs { get; } =
        new(ReferenceEqualityComparer.Instance);

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

    // ── COBOL-2002 user-defined function invocation (WS-2002-UDF) ──

    /// <summary>True if <paramref name="name"/> names a user-defined function (a FUNCTION-ID unit in this
    /// compilation group) rather than an intrinsic.</summary>
    public bool IsUserFunction(string name) => Semantic.UserFunctionNames.Contains(name);

    /// <summary>
    /// Lower a <c>FUNCTION user-name(args)</c> invocation that is the entire source of an assignment
    /// (MOVE / single-target COMPUTE) into <c>CALL "user-name" USING args RETURNING destLoc</c>. The function
    /// unit was compiled as a callable program whose PROCEDURE DIVISION … RETURNING writes the result through the
    /// passed pointer (DEVLOG 365). Arguments are passed BY CONTENT — function arguments are values and the
    /// function must not mutate the caller's data (ISO §8.4.3). Returns false (emitting nothing) if any argument
    /// is not a resolvable storage location (e.g. an arithmetic-expression argument), which remains a documented
    /// follow-up so the caller can fall back to its normal lowering.
    /// </summary>
    public bool LowerUserFunctionCall(BoundFunctionCallExpression func, IrLocation destLoc, IrBasicBlock block)
    {
        var args = new List<IrCallArgument>(func.Arguments.Count);
        foreach (var a in func.Arguments)
        {
            var loc = Location.ResolveExpressionLocation(a);
            if (loc == null) return false;
            args.Add(new IrCallArgument(1 /* ByContent */, loc));
        }
        block.Instructions.Add(new IrCallProgram(func.FunctionName, isDynamic: false, args, returningTarget: destLoc));
        return true;
    }

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
