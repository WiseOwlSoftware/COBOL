## PB2130: the unread IBoundRead interface is deleted and IArithmeticStatement becomes a pure marker

The R0 census (kb/Work PB2115) listed three bound-node interface members that only SymbolFinder's implementation
cascade kept alive: `IBoundRead.Kind`, `IBoundRead.AdvancingOnLock` and `IArithmeticStatement.SizeError`.

**Re-probe.** A caller query on main 223cd4fab5 (`git grep` over every tracked `.cs`, with an LSP `findReferences`
that returned nothing) found no read of any of the three through its interface. `IBoundRead` was named only in the two
READ nodes' base lists and in comments: its one consumer, the `VersionConformancePass.GateStatement` arm, moved to the
parse arms `VisitReadDirection` and `VisitReadAdvancingOnLock` in kb/Work PB353. `IArithmeticStatement` has one
reader, the type test `ec.Inner is IArithmeticStatement` in `EcEmitter`, and every `.SizeError` read goes through
the concrete node.

**Fix.** `IBoundRead` is deleted whole, so `BoundRead` and `BoundKeyedRead` keep `Kind` and `AdvancingOnLock` as their
own members. `BoundIoPhraseConsumptionDriftTests` (kb/Work PB340) already holds each READ node's phrase members to its
emitter, the job the interface once did. `IArithmeticStatement` keeps its marker role as `public interface
IArithmeticStatement;`, the same shape as `IBoundTransfersControl`, because
`EcSizeGuardDriftTests.EveryStatementWithASizeErrorPhrase_IsAnArithmeticStatement` checks marker and phrase in both
directions. The comments and design docs that named `IBoundRead` as the live gate arm now name the parse arm
(`docs/FILES_DESIGN.md`, `docs/rearchitecture/DESIGN-version-conformance-pipeline.md`, `RelativeConnector.ReadPrevious`,
`KeyedIoBinder.BindRead`, two conformance-case comments). The frozen census records and the inventory rows' closing
narratives are history and stay as written.

No golden and no inventory row change: the change is behavior-neutral (`closes_rows: []`). Neutrality is checked by
the R0 oracle (`scripts/arch/compare_oracle.py`) and by the gate.
