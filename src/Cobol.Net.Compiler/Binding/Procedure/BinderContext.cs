// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Binding.Validation;

namespace CobolNet.Binding.Procedure;

/// <summary>
/// The binder collaborators' shared spine (P7 Step 10c — the phase doc's §Step 10 AS-BUILT PLAN; the
/// <c>EmitContext</c> precedent: core refs + derived accessors, ONE instance per bound unit / class roster,
/// constructed and held by <c>StatementBinder</c> — the transitional host until the 10t final wiring). The
/// as-built shape supersedes the design sketch's names: <see cref="Edition"/> (the ONE diagnostic sink AND
/// edition surface — there is no separate <c>IDiagnosticSink</c>/<c>EditionInfo</c> split), <see cref="Symbols"/>
/// (the ONE per-binder scope-aware <c>SymbolTable</c> from Exec Step B — the sketch's <c>SymbolTableBuilder</c>
/// was superseded), and <see cref="ActiveScope"/> (recomputed per READ — callers must never capture it at
/// construction; the scoped <c>EnterMethodScope</c> model lands with the OO conversion, batch 10s).
/// Collaborator handles register here as their batches land — exactly ONE instance each, because per-verb
/// counters/memos are per-unit lifetime.
/// </summary>
internal sealed class BinderContext(DataBinder data, ReferenceResolver refs)
{
    /// <summary>The unit's bound data model (files/reports/alphabets/options/collected facts).</summary>
    public DataBinder Data => data;

    /// <summary>The unit's reference resolver (dataReference → <see cref="Place"/>).</summary>
    public ReferenceResolver Refs => refs;

    /// <summary>The ONE diagnostic sink AND edition surface (Error/Removed/Warning · DialectLevel ·
    /// CheckDigitCapacity) — every verbatim-moved gate, lifted check, and diagnostic flows through this
    /// single instance, preserving emission order.</summary>
    public EditionContext Edition => data.Edition;

    /// <summary>The ONE scope-aware name resolver (per binder — P6 Exec Step B).</summary>
    public SymbolTable Symbols => data.Symbols;

    /// <summary>The CURRENT resolution scope — recomputed per read (the method overlay changes as the OO
    /// roster binds); never capture this at construction.</summary>
    public Scope ActiveScope => data.ActiveScope;

    /// <summary>Compilation options (arithmetic mode for the composite cap, DEFAULT ROUNDED mode).</summary>
    public OptionsModel Options => data.Options;

    /// <summary>The edition-invariant SR check catalog (P7 Step 10 — pure checks only: each reports to
    /// <see cref="Edition"/> and returns the verdict; the verb binder owns all error+placeholder control
    /// flow).</summary>
    public StatementValidation Validation { get; } = new(data);

    /// <summary>The per-unit SPECIAL-NAMES mnemonic registry (10h — ACCEPT-FROM and the WRITE SR13 /
    /// ADVANCING zero-advance legs share the ONE lazily built map).</summary>
    public MnemonicRegistry Mnemonics { get; } = new();
}
