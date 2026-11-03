## The backend seam, wave 20261848 group A: the OO symbols carry no parse context, and a method's declaratives are bound-tree data (PB2401)

**PB2401 — what the OO symbols name that the model may not.** The re-probe on this tree matched the note:
`OoClassSymbol.Ctx`, `OoInterfaceSymbol.Ctx` and `OoMethodSymbol.Ctx` held the CLASS-ID, INTERFACE-ID and METHOD-ID parse
contexts (49 reads, every one binder-side), `OoMethodBinding.Declaratives` held bound nodes on the method's signature record,
and `OptionsModel.cs` carried the two float-phrase factories over parse contexts. It found one thing the note did not list: the
class and interface symbols also carried `Expansion`, an `ExpansionOrigin` that names the REPOSITORY entry and the skeleton's
parse context, which PB2402's move would have carried into the model.

The fix extends the ONE map PB2398 made rather than adding a second: the bind session's `SourceMap` now also holds each Source
class's, interface's and method's definition and what made a class or an interface an expansion (`Add`, `Of`, `ExpansionOf`),
recorded by `OoClassTable.Build` as it makes each symbol. `Add` refuses a symbol whose origin is not Source and `Of` throws for
one, naming its origin, so PB4459's loud fault survives the move; each symbol now has one constructor, taking its origin.
Readers take the map from where they already stand: the class table keeps the session's map for its own later questions
(`MethodAt`, `ResolveOverrides`); `OoConformance`'s four validators, `CheckingFold.Method` and `ExpansionPlacementPass.Run` take
it as a parameter; `OoDriver` and `BinderDriver` read `session.Sources`. `OoDataBinder.BindMethodData` is handed the method's
definition and keeps its body (`BodyOf`, loud for a method this binder never bound), which the class-body binder and the
data-pointer pass read, so `StatementBinder` (still in the bound namespace, PB2347) names no `SourceMap`: the first shape
passed the map to `BindMethodRoster`, and the architecture walk refused the new pair on the tolerated `Binding.Bound → Binding`
row. A method's declaratives are `BoundMethod.Declaratives`, on the class's bound program beside its statements, and
`OoEmitter.EmitMethod` reads `bound.Methods[i]` with `roster[i]`. `FloatFormatPhrase` is its own binder file. Every caller
changed in the same change; no forwarder is left.

Behavior-neutral (no ISO rule changes; no golden). Neutrality: `=== ARCH-ORACLE: IDENTICAL — 16440 cases (baseline 5f50cd7dbb04, new 5f50cd7dbb04-dirty) ===` (the baseline is the branch head before this change, so the comparison isolates it from the predecessors it carries). The architecture walk and the edges file are
unchanged: every row the change touched is allowed, and PB2402's move is what removes the tolerated rows. Witness:
`OoMethodSymbolOriginTests` now pins the map (a Source symbol's definition is in it, a non-Source one is refused and answers a
fault naming its origin, a Source symbol the map never saw is a fault). Docs: DESIGN-backend-abstraction §1.3 L6, OO_DESIGN and
CONDITIONS_EXCEPTIONS_DESIGN describe the map and `BoundMethod.Declaratives`. Gate: GATE_LINE.
