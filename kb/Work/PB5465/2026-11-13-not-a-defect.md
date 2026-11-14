## Re-verdict 2026-11-13 (wave 20262022 B): not a defect, retired unlanded

**Re-probe.** On this branch's Debug build of 367e05ab5c, `o2.cob` (`INVOKE O2CLS "SHOWG" USING BY CONTENT GS N`) prints
`N=-038` after the fourth call and `o3.cob` (`BY CONTENT GS BY CONTENT N`) prints `N=-039`: the behaviour the note
describes reproduces. It is the behaviour the standard requires.

**Authority.** The printed §2023:14.9.23.2 figure (PDF page 681, folio 651, rendered with `scripts/render-spec-page.py`)
puts the ellipsis after the brace of the three BY phrases and inside the USING bracket, and it has no inner ellipsis:
each repetition is `[BY phrase] operand`. CALL's figure is different, and CALL alone states a carry-over, as a FORMAT 1
rule (`§2023:14.9.4.4 GR5`, "Both the BY CONTENT and BY REFERENCE phrases are transitive across the parameters that
follow them"). INVOKE has no such rule in 2002, 2014 or 2023 (`grep -i "transitive across"` over the three
transcriptions finds only the procedure-division header, CALL, INSPECT and SORT/MERGE). An INVOKE argument written
without a phrase takes its mode from its formal: `§2023:14.9.23.3 SR14` ("If an argument is specified without any BY
phrase, BY REFERENCE is implied for that argument when", cite.py OK) and `§2023:14.9.23.4 GR6` ("If an argument is
specified without any of the keywords BY REFERENCE, BY CONTENT, or BY VALUE, the manner used for passing that argument
is determined as follows", cite.py OK), the twin of CALL Format 2's `§2023:14.9.4.4 GR9`. So in `USING BY CONTENT GS N`,
N meets SR9/SR10 and its formal is BY REFERENCE: GR6 a) 1. passes it BY REFERENCE, and the method's store reaches it.

**What a transitive reading would break.** `USING BY REFERENCE G 7` would make the literal an identifier-3 (SR9 refuses
it), and `USING BY CONTENT G V` against a BY VALUE formal would break SR5 a). Both are conforming source.

**Landed.** No code change. A comment in `InvocationArg.OfInvokeArgument` and a sentence in `docs/OO_DESIGN.md` record the
rule; the golden `conformance-suite/cases/14.9.23-INVOKE/pb5465_invoke_by_phrase_not_transitive` (accept 2002) pins all
five legs, and it joined the witnesses of GR-14.9.23.4-6, SR-14.9.23.3-14 and FMT-14.9.23.2 (all CONFORMS).

**Sweep.** Every INVOKE argument reader reads each argument alone: `InvocationArg.OfInvokeArgument`,
`OoBinder.OoBindInvoke`'s pre-screen loop, `StorageClaimScan` (its `InvokeUsingContext` arm) and
`VersionConformancePass.VisitInvokeArgument`. CALL Format 1 carries the mode (`CallBinder.CarriedModeAfter`, GR5), and
CALL Format 2 takes a bare argument's mode from the formal (GR9), as INVOKE does.

**Sites:** `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/OoBinder.InlineInvocation.cs` (`InvocationArg.OfInvokeArgument`, doc comment only), `docs/OO_DESIGN.md`.
