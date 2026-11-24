## A GOBACK returns up the dispatch frames without a throw where every frame tests for it (PB6140)

**The mechanism (kb/Work PB6140, R2 `runtime-control-2--performance--f1#1`).** Every GOBACK, called-program EXIT PROGRAM, method
GOBACK and §2023:14.6.3 implicit GOBACK threw `ProgramReturn` / `MethodReturn`, caught at the activation entry, from whatever dispatch
frame it executed in. The re-probe on this tree (a main program CALLing four one-statement subprograms a million times each, timed with
FUNCTION CURRENT-DATE, `--std 2023`, Debug): top-level GOBACK 2760 ms, GOBACK one PERFORM deep 2930 ms, EXIT PROGRAM 2520 ms, fall-off
with no throw 520 ms, so about 2 us per CALL return went to the exception.

**The fix.** The dispatch methods take `bool __direct = false`. The activation entry calls the top frame with `__direct: true`;
`DispatchState.DispatchCall`, the one construction site of an out-of-line PERFORM's or SORT/MERGE procedure's bounded call, passes its
frame's `__direct` on and returns `ElementReturn.Returned` (a new runtime constant, `int.MinValue`, never a pc) when the range did.
`DispatchState.ReturnToActivator` is the one spelling of the return for all four statements: `if (__direct) return
ElementReturn.Returned; throw new ProgramReturn();`. `__RunUse` and `__RunDebug` pass no `__direct`, so a return below a USE procedure
or a debugging section still throws, through the exception filter `__RunUse` keeps for §2023:14.9.40.4 GR17. An operand-evaluation step
always throws. `EcEmitter`'s automatic-propagation returns keep the throw: their text also lands in the `__IoCheckEc` helper, which
has no dispatch frame. `DispatchEmitter.ReturnSignal` moved to `DispatchState`. After: 630 / 250 / 250 / 230 ms (the first loop carries
the JIT warm-up), the same `N=004000000`.

**Neutrality.** A `return` runs the same `finally` blocks a throw does, and the one catch clause in emitted code that takes the signal
(the GR17 filter) sits in `__RunUse`, below which the throw is kept. The emitted C# of every program changes (the dispatch header, each
PERFORM call, each return), so the §4 arch oracle differs in every case by construction and was not run; the behaviour oracle is the
gate's whole golden population, including the `pb3511_implicit_goback_*` goldens (nested PERFORM, SORT input and output procedures,
methods) and the new `14.9.18-GOBACK/pb6140_return_from_nested_frames` (GOBACK two PERFORMs deep with a RETURNING result, EXIT PROGRAM in
a THRU range, GOBACK from a paragraph a USE declarative PERFORMs; 2002, 2014, 2023). `ElementReturnProtocolDriftTests` pins that every
dispatch call passing `__direct` tests its result and that only `DispatchState` writes the return; `ProcedureRangeEmptinessDriftTests`
follows the new call shape. Docs: CONTROL_FLOW_DESIGN D7 and its samples, DESIGN §5.1/§5.2, CONDITIONS_EXCEPTIONS_DESIGN.

No inventory row (a restructuring; `closes_rows: []`).
