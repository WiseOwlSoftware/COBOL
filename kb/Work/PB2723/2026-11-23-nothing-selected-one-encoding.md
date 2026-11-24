**2026-11-23, wave 20262154 group B: "nothing selected" has one encoding (the merged finding `runtime-control-2--architecture--f1#2`). The note stays open for the three other remainders.**

**Re-probe (tree 8e304114aa, main ccb23d13e4 plus the cc1-19 commits).** The exception-checking PERFORM said "no WHEN
selected" in two ways beside the protocol's own `DispatchResult.NoHandler`: the frame matcher returned
`PerformFrame.NoMatch` (`int.MinValue`, declared outside the protocol type), and `ExceptionEngine.RunTopFrame` returned
that sentinel together with an `out bool handled`. The emitted code translated by hand in two places:
`EcEmitter.EmitIoCheckEc` wrote `bool __wh`, `RunTopFrame(..., out __wh)` and `if (!__wh) __sel = DispatchResult.NoHandler`,
and `EcEmitter.EmitEcPerformMember` wrote `__h ? __a : ...`.

**The fix.** `DispatchResult.NoHandler` is the one value. No path that runs a handler returns it: a WHEN handler runs
through `__RunF3` and `__RunUse`, which answer `Normal`, `NotNormal`, `ResumeNext` or a RESUME pc. This is the meaning
that `DispatchResult.RanAHandler` already states.
- `PerformFrame.NoMatch` is deleted. The matcher returns `DispatchResult.NoHandler`, and `ControlFlowEmitter.EmitMatcherArms` emits it.
- `RunTopFrame(string ec, string? file)` has no `out` parameter. It returns the first frame's action for which
  `RanAHandler` is true, or `NoHandler`. The `ExceptionState` facade and `PublicAPI.Unshipped.txt` follow.
- `EmitIoCheckEc` emits `__sel = RunTopFrame(...)` followed by `if (__sel == DispatchResult.NoHandler) { <the USE tiers> }`.
  `EmitEcPerformMember` emits `__a != NoHandler ? __a : __EcDispatch(...)`. A unit with no USE Format 3 returns
  `RunTopFrame(...)` directly.
- `DispatchResultProtocolDriftTests.TheProtocolHasOneDefinition` now refuses `PerformFrame.NoMatch` and a `RunTopFrame(... out ...)`
  in the compiler and runtime sources. `PerformFrameStackTests` asserts the protocol names.

**Neutrality.** `=== ARCH-ORACLE: DIFFERENT — 35 of 23499 cases differ (baseline 8e304114aa11, new 8e304114aa11-dirty) ===`.
All 35 differences are CSHARP text in units that contain a Format-3 PERFORM, and they are exactly the three emitted
shapes above. No run output and no diagnostic differs.

**Still open on this note:** (1) the grammar rule pairs `performWhenEcItem`/`useEcEntry` and
`performWhenModeList`/`useOnTarget`; (3) the cost of the GOBACK / EXIT PROGRAM exception; (4) the Tier-A REDEFINES
alias rule. See `2026-11-22-gr3-selector-unified.md`.
