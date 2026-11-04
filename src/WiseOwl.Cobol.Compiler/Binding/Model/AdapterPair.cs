// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>One covariant-return adapter pair (§9.3.8.2.3 rules 5a/5c2 — conforming COBOL that C# interface
/// implementation cannot express directly, because interface implementations demand the EXACT return type):
/// the emitter renders an EXPLICIT interface implementation <c>PROTO_RET I_CS.M(…) =&gt; this.M(…);</c> per
/// pair; <paramref name="Factory"/> marks the FACTORY side. Model data (kb/Work PB2403): it names only the OO symbols,
/// so the binder's <c>OoConformance.ValidateImplements</c> makes it and the backend reads it off
/// <see cref="BoundCompilation.OoAdapters"/>.</summary>
public readonly record struct AdapterPair(
    OoInterfaceSymbol Iface, OoMethodSymbol Proto, OoMethodSymbol Impl, bool Factory);
