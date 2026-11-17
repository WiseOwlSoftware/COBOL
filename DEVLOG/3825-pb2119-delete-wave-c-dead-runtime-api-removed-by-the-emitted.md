## Entry 3825 — 2026-11-17 13:45 PST — PB2119 Delete wave C: dead runtime API removed by the emitted-set measure, joined names kept, the register re-measured

**What was measured.** Before any deletion, the arch-oracle capture of main 5459e4e876 compiled the whole population the
suites compile (conformance corpus, NIST green programs, version matrix, optional-word sweep: 22595 cases, 8473 emitted
`.g.cs` files) and kept the emitted C#; every identifier it holds that names a runtime member is the emitted set
(E:/COBOL-coord/scratch/my-20261115/w20262101/c/emitted-members.txt, 1212 names). A stale `tests/nist/output/*.g.cs`
was not used as evidence.

**Deleted (behaviour-neutral, no caller in src or tests, absent from the emitted set, no compiler site joins strings
into the name).** `Collator.Normalize` (PB2213), `LocaleInfo.Notes` (PB2214), `CobolContiguousLayout.StructureOf`
(PB2203), `CollationElement.IsUpper` / `Ignorable` / `IsPrimaryIgnorable` (PB2210), `CobolDynTable<T>.InitializeAll`
(PB2204), the four static `ExceptionState` forwarders (PB2216), `GraphemeCluster.Memory` / `EnumerateRunes` / `==` /
`!=` (PB2227), `CollationTable.HasContraction` / `Singles` and `WeightMap.IsIdentity` (PB2212; `Contractions` is live
and stays), `CobolEdit.FloatExtremeImage` (PB2205), and the nine `CobolFile` forwarders with the `FileRegistry` and
`SequentialConnector` members only they read (PB2220: the NIST output that named three of them was stale). Compiler
side: `RuntimeApi.Bool`, `NumParseImageU` (with `CobolNum.ParseImageU`), `DecDivLifted` and `NumRescale`, whose only
"use" was a drift test pinning its signature (PB2181); the unused one-argument `CobolParserCoreBase` constructor
(PB2198); and the two COBOL0899 descriptors no code raises, `arithmetic-standard-intrinsic` and
`construct-staged-not-implemented` (PB2186; the three edition-band rows stay, they are the catalog rows of the live
codes COBOL0900/0901/0903). 32 lines left `PublicAPI.Shipped.txt` and the runtime major is 30.

**Kept, because an input reaches them through a name the emitter builds by joining strings.** The nine `*Real` intrinsic
bodies (PB2206): `IntrinsicRenderer.RealMethod` turns `XxxScaled` into `XxxReal` and appends `Real` otherwise for a
binary64 argument, and `pb2_float_argument_exact_family` emits all nine. `FloatResultant.FromDecOrRaise` (PB2219):
`RuntimeApi.FloatResultantStoreOrRaise` writes `{stem}OrRaise`, and a probe (INVOKE BY CONTENT an expression into a
FLOAT-SHORT method formal, ARITHMETIC IS STANDARD-DECIMAL, EC-SIZE checking on) emits it, though no corpus program
does. That probe is the lesson: the corpus's emitted set does not hold `DispatchResult.RanAHandler` or
`CobolBool.OrAll` either, both emitted text, so a corpus can only confirm deadness alongside a read of the
construction code, never replace it. The census's own blind spot for joined names and constructors stays open in
kb/Work PB2756.

**The register.** Retired as live: PB2202, PB2209, PB2215, PB2226, PB2169, PB2211, PB2183, PB2238 (each dated file
quotes the reference), PB2197 (ANTLR output; only a grammar change deletes one) and PB2198 (the edition-predicate
vocabulary stays complete). Landed as already gone: PB2184, PB2175. PB2290 (the CSharpEmitter shim, still
constructed and threaded) and PB2756 stay open with the re-measure's finding.

**Neutrality.** `python scripts/arch/compare_oracle.py` over the captures before and after the deletions:
`=== ARCH-ORACLE: IDENTICAL — 22595 cases ===`, and the emitted member set after equals the set before. The solution
builds with 0 warnings; the Characterization snapshots (37) and the touched unit (6577) and conformance (365) filters pass.

