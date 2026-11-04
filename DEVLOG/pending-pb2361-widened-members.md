## PB2361 and PB2293 close: every binder member has its listed readers, and the backend seam's L6 leak is gone

PB2361's carrier half (the entry above) put `DataBindResult` on the backend input. Its other half was the members the
ten `DataBinder` extract steps widened from `private` to `internal` so the carved binders and passes could reach them.
The note asked that none stay wider than its readers need, and that a drift test make a new reader a conscious change.

**Measured, not grepped.** Against main before the first extract landing (`1f60f2503`, before #1042) about 100 members
private then are non-private now, spread over `DataBinder` and the six binders carved from it. A text census over-counts:
names such as `Of`, `All` and `LiteralPosition` collide across types. So the edges test's metadata walk
(`TypeReferenceWalk`) learned one more answer. Given owner types, `MemberReaders` records each owner's non-private members
and the outermost types outside it whose IL tokens, signatures or attributes name each. A nested type counts as one member.
Constructors, `const`s, virtual members and compiler-synthesized members are left out, and a test is not a reader. On the
branch it measured 261 members, 15 of them read by no other type.

**The fix.** Those 15 are now `private` (16 declarations): the constant binder's `ConstantScope`, `DeclaredConstant` and
`ConstantBindState`; `DataBinder`'s `InheritGlobalSubtree`, `SymbolicRaw`, `ImpliedReportPicture` and
`NonConditionDeclarationNearerThan`; the file binder's `SelectedFiles`, `SuppressWhenOperands`, `ResolveApplyCommit` and
`ResolveFileCollating`; `OoDataBinder.OwnerClassName`; the report binder's `BareReferenceOf` and `IsParenthesized`; and
`SpecialNamesBinder.RegisterRangeCollation`. One member moved to its final owner. `DependingInsideTableRange`, the
§13.18.38.3 SR20 byte-range test, had one reader, `DependingAdmission` (the one admission of an OCCURS DEPENDING ON
object), and is now its private member. The other single-reader statics stay where they are, since their owner calls each
of them too. `CopyDescriptionToTemp` also stays: it is the door that keeps `CompilerTemps` inside the one copy scope it may
use. Two text pins in `GlobalNameClassDriftTests` followed the narrowing. That test now reads the private `ConstantScope`
by reflection.

**The drift test.** `BinderMemberReadersDriftTests` holds the walk to
`tests/WiseOwl.Cobol.Tests.Unit/Architecture/binder-member-readers.json`, which lists 245 members with their readers. It
refuses three things: a member no other type reads, a reader the file does not list, and a stale entry. On a mismatch it
writes the measured picture for copying. A planted assembly makes each refusal fire, and its keys pin the property,
nested-type and override rules.

**PB2293 closes.** It was the record of the L6 leak (the backend input carried the binder and the parse tree), and it
closes with PB2375, PB2361, PB2398 and PB2401. Re-measured, no bound unit, `BoundCompilation` or `EmitContext` field holds
a binder, a resolver or a parse context. Code generation names `DataBinder`, `ReferenceResolver` and `ResolveItem` only in
comments. DESIGN-backend-abstraction §1.3 now records L6 as DONE. It names two residues, each a tolerated edge with an open
remover: `OoEmitter`'s `BinderDriver.ExternalItemIdentity` call (PB2418) and `StatementBinder` in the bound layer (PB2347).
In the edges file, the pairs that named PB2361 next to other removers no longer name it. The two pairs it named alone are
binder-to-binder state. `StatementBinder → DataBindState` ends with PB2347, and `SymbolTable → DataBindState` with PB2399.

**PB4273 stays open, now blocked by what is left.** The assembly split cannot compile while code generation, the model and
the bound tree name compiler.binding types in tolerated rows (`CodeGen → Binding` 72 pairs, `CodeGen.Emit → Binding` 22,
`Binding.Model → Binding` 21, `Binding.Bound → Binding` 46, and three smaller rows). Its `blocked_by` now names those
rows' removers: PB2320, PB2347, PB2399, PB2405, PB2406, PB2407, PB2418, PB2419 and PB2420.

**Witnesses.** `=== ARCH-ORACLE: IDENTICAL — 16536 cases (baseline 5ee45df108fc, new 5ee45df108fc-dirty) ===`. No
inventory row changed (`closes_rows: []`, behavior-neutral). The gate's verdict line is in the wave report.
