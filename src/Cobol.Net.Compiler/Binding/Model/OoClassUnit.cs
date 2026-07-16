// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;

using CobolNet.Compiler.Oo;

namespace CobolNet.Binding.Model;

/// <summary>One CLASS-ID unit of the compilation group (the ClassUnit counterpart of <see cref="BoundUnit"/>):
/// its pass-1 symbol, its OBJECT-paragraph data model, and its bound method bodies.
/// <para>Relocated from the emitter (rearch PHASE-06 Step 2) — the DATA TYPE lives with the bound model; the OO
/// bind LOGIC lives on <see cref="Compiler.Oo.OoDriver"/> (P9 Step 4; emission on <c>Verbs/OoEmitter</c>) — the OO
/// subsystem.</para></summary>
internal sealed class OoClassUnit
{
    public required OoClassSymbol Symbol;
    public string Name => Symbol.Name;
    public string CsName => Symbol.CsName;
    public DataBinder Data = null!;
    public ReferenceResolver Refs = null!;
    public BoundProgram Bound = null!;
    // The FACTORY half (§11.4; brief D11 — a sibling singleton class): its OWN data forest (factory and
    // instance data are separate source elements, §10.6 :12752-12770 — name separation is structural).
    public DataBinder FactoryData = null!;
    public ReferenceResolver FactoryRefs = null!;
    public BoundProgram FactoryBound = null!;
}
