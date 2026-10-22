## Entry 2336 — 2026-10-21 15:10 PDT — GO TO ... DEPENDING out of a PERFORM range transfers again (PB3491)

A GO TO ... DEPENDING ON inside a performed paragraph, whose selected target was the paragraph that physically follows the PERFORM's exit paragraph, silently ended the PERFORM: the target never ran, control resumed after the PERFORM, and the program exited 0 with no output. The overnight differential fuzz run (PB3488 harness, program fz01959) found it; GnuCOBOL printed the two lines and ISO/IEC 1989:2023 14.9.17.4 GR2 says control is transferred to the selected procedure-name.

**Re-probe.** The filed reduction printed nothing on a fresh build of 2881947e8, as the note said.

**Cause.** The emitter has two ways out of a paragraph body. A completion (fall-through, EXIT PARAGRAPH, EXIT SECTION, NEXT SENTENCE in a last sentence) leaves with the bare jump, and the dispatcher's return test (`__atExit && __pc == __exitPc + 1`, the 14.6.3 rule 1 implied return) is meant to fire for it. An explicit transfer (GO TO) leaves through `ExplicitTransferOut`, which first clears `__atExit`, because a GO TO to the paragraph after the exit paragraph does not complete the exit paragraph (14.9.28.4 GR5: the return mechanism sits after the last statement). The format-1 GO TO and the ALTERed GO TO used the explicit form (PB1148). `EmitGoToDepending` still used the bare jump, so its selected target was taken for the exit paragraph completing. Two arms of one dispatch, one of them wrong.

**Fix.** `EmitterState.ExplicitTransferJump()` is the jump half of `ExplicitTransferOut` (now built on it), and the DEPENDING in-range jump is `if (in range) { __atExit = false; goto __xfer; }`. An out-of-range selector still falls through with the flag untouched, so the paragraph still completes and the PERFORM still returns (variant V6 of the golden).

**Sibling sweep.** `TransferJump()` has two callers outside EmitterState: this arm (now fixed) and EXIT SECTION, a completion by 14.9.14.4 GR7. `DispatcherTransferIdiomDriftTests.OnlyACompletionCallsTheBareTransferJump` closes that list, and `GoToDependingLeavesThroughTheExplicitTransfer` measures the generated C#. `docs/COBOLNET_CONTROL_FLOW_DESIGN.md` D1a now states completion versus explicit transfer.

**Golden.** `conformance-suite/cases/14.9.17-GO-TO/pb3491_go_to_depending_out_of_perform_range`, accepted at all four editions with one `.out` (the behaviour does not differ): six shapes in one chain (single target, three targets with the second selected, the format-1 twin, PERFORM THRU with the target inside the range, PERFORM THRU with the DEPENDING in the exit paragraph's last statement, and an out-of-range selector that must still return). Expected lines come from the cited rules. No refused-below negative: nothing is edition-gated.

**Rows.** GR-14.9.17.4-2 and GR-14.9.28.4-1 were already OK; a record_verdicts batch adds the golden and the drift tests as witnesses. `closes_rows` is empty with its reason.

