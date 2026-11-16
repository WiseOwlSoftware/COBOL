## PB5862: an architecture-oracle observation no longer depends on whether the compiled-program store is warm

`ArchOracleDriftTests.AnObservation_IsPortable_AndRepeatable`, added by PB4333, went RED in gate run 000001-20261116T140220Z of another branch with no regression: two `Observe` calls of one case disagreed, `ambient` against a digest.

**The cause.** With the store on, a MISS recorded the read of the case's source, which the store had staged under `.cache/compiled-programs/src/` (outside the case's scratch directory, so `ambient`). A HIT returned a result with no recorded inputs, so it got a digest.

**The fix.** Every oracle observation compiles for real: `CompiledProgramCache.Cold()` is an async-local scope that `OracleCase.Observe` opens. The store's staging directory, named by its content alone, is the compile's own place (`StagedRoot`). The definition of ambient is unchanged.

**The differential's capture is not exposed.** It refuses to run with the store on and is started with the store off, so every capture compile is cold.

**Proof.** `AnObservation_IsTheSame_WhetherTheStoreIsColdOrWarm` observes a never-seen program twice: it fails every time with the fix removed and passes with it.