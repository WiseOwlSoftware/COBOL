// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.CodeGen;

/// <summary>
/// The emit entry of the Roslyn backend (P7 Step 9n — the recorded deviation from the original
/// "CSharpEmitter is gone" slogan): the emitter god class is dissolved — emission lives on
/// <see cref="ProgramEmitter"/> (run-unit orchestration) → <see cref="UnitEmitters"/> (the per-unit
/// composition root) → <see cref="DispatchEmitter"/>/<see cref="StatementEmitter"/>/<c>Verbs/*Emitter</c> —
/// and THIS class keeps only the emit entry (<see cref="EmitBound"/>). It binds nothing and names no parse context: every
/// caller binds with <c>BinderDriver.Bind</c> itself, and the bound compilation is the backend's whole input (kb/Work PB2398,
/// which deleted the bind facade this class carried). It emits NOTHING.
/// </summary>
public sealed partial class CSharpEmitter
{
    /// <summary>Render typed-native C# from an already-bound immutable <see cref="BoundCompilation"/> (the emit
    /// half of the bind/emit split) — a fresh <see cref="ProgramEmitter"/> per call; the compilation carries
    /// everything emission needs (incl. the OO class table + interface forests), so the emit side reads NO
    /// bind-host state. Whether the module carries its units' signature attributes is the compilation's own fact
    /// (<see cref="BoundCompilation.PublishesSignatures"/>, DESIGN-external-repository §9.1; kb/Work PB4319).</summary>
    internal string EmitBound(BoundCompilation comp, string assemblySimpleName, WiseOwl.Cobol.Frontend.CompilationInputs? inputs = null)
        => new ProgramEmitter(inputs).Emit(comp, assemblySimpleName);

}
