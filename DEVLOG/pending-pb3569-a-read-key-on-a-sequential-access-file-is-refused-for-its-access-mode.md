## PB3569: a READ with a KEY phrase on a sequential-access indexed file is refused for its access mode, not for phrases it did not write

Wave 20261715 group B, under the owner's standing campaign rules ("Fix the root causes, never simply making the
diagnostic pass"; "Always be consistent with ourselves barring an explicit requirement differently").

**What the re-probe showed.** The fuzz lead (fz00018, `READ FS-RND KEY IS WS-KEY-DATA INVALID KEY ...` on an indexed file
with no ACCESS MODE clause) reproduced on a fresh build: COBOL0864 "is a Format-2 phrase and cannot combine with
NEXT/PREVIOUS/AT END" for a statement that wrote none of those phrases. The binder's test was `kind != ReadKind.Random`,
and that holds for two different faults: a sequential-read phrase written beside the KEY phrase (`READ f NEXT KEY IS k`,
where the message was right) and a file whose access mode is SEQUENTIAL, written or assumed (where it named words the
statement did not contain). Probes at 85/2002/2014/2023: default access, `ACCESS MODE IS SEQUENTIAL`, `NEXT KEY`,
`PREVIOUS KEY`, `ADVANCING ON LOCK KEY` refused; RANDOM and DYNAMIC accepted; `AT END` and `NOT AT END` beside KEY never
reach the binder (the parser refuses them, COBOL2269, PB3830). GnuCOBOL 3.2.0 accepts every one of the sequential shapes
with the warning "KEY ignored with sequential READ".

**The analysis question (is Format 2 legal under sequential access?), answered.** No syntax rule says so either way:
§14.9.30.3 SR8 (NEXT implied under ACCESS MODE SEQUENTIAL) and SR6 sit under Format 1. The statement has no meaning in
the standard, derived from three rules: GR19 makes a READ with a KEY phrase and no NEXT or PREVIOUS a random read;
§14.9.30.1 says a READ under sequential access "makes available the next logical record"; §12.4.5.5.3 GR2 has such a
file's records "accessed in the sequence dictated by the file organization" (GR1: sequential when the clause is absent),
and GR3 and GR4 are what admit random access. The refusal stays, now a documented determination
(docs/CONFORMANCE.md D-READ-KEY-ACCESS). The alternative, GnuCOBOL's ignored key, would read a different record than the
KEY phrase selects, which CLAUDE.md rule 1 does not allow as latitude. Every citation ran through `cite.py --check`.

**The fix.** `StatementValidation.CheckReadKeyFormat` is the one check, beside its SR10 sibling `CheckReadKeyOrganization`
and called from `KeyedIoBinder.BindRead` after it. It has two messages. A sequential-read phrase beside KEY: the message
names exactly the phrases the statement wrote (`NEXT`, `ADVANCING / PREVIOUS`, ...), from the one list SR6 itself
names, now `StatementValidation.WrittenSequentialReadPhrases`, shared with `CheckReadRandomAccessPhrases`. A SEQUENTIAL
file and none of those phrases: the message names the access mode, says when it is assumed because the clause is absent,
and names no phrase. An ACCESS MODE RANDOM file is not asked, since SR6 has already named the words (COBOL1720). No new
diagnostic code was needed (COBOL5233-COBOL5235 returned): it stays COBOL0864, and the one raw literal moved from the
binder to `StatementValidation`.

**Goldens.** `conformance-suite/cases/14.9.30-READ/pb3569-read-key-sequential-access-default`, `...-written` and
`pb3569-read-next-key`, each refused at 85/2002/2014/2023 with the wording as `expect` in `diagnostics.json`; the legal
twin on RANDOM and DYNAMIC files is the existing `l1_read_key_of_reference`. `ReadKeyFormatTests` pins the messages at
every edition (what each names, what it must not name), the RANDOM and DYNAMIC acceptance, and that
`RANDOM ... NEXT KEY` draws SR6's COBOL1720 once and no COBOL0864.

**Rows.** None claimed or closed (`closes_rows: []` with its reason). PB3569 flipped to landed.

**Sibling sweep.** The sequential-organization READ arm refuses any KEY through the same SR10 check, whose message was
already exact; no other diagnostic in `src` lists a fixed set of READ phrases. `READ f INVALID KEY` with no KEY phrase on
a sequential-access file stays accepted (it selects no record; the emitter binds the phrase dead, the CCVS leniency).

**Self-review.** Architecture: one check, one phrase list, one raw literal moved rather than added. Full code: the
message implements GR19's two cases, not a paraphrase of the old text. Performance: bind-time, a handful of booleans.
Duplication: the phrase list that SR6 and the KEY check both need is one helper. Silent failure: the refusal is an error
at every edition and never rerouted to a sequential read.

**Gate.** Run `TestResults/build-local/000001-20261027T005759Z` at HEAD 8d929f328:
`=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — ... Conformance 6,100/18,998 · Unit 37,108/38,456 ·
Characterization 36/36 cases ran ... Linux half (WSL clone of HEAD) GREEN ...`. The twelve goldens (three cases at four
editions) and `ReadKeyFormatTests` are all `outcome="Passed"` in `leg-1-Conformance.trx`. `semgrep/verify.py` PASS
(`raw-diagnostic-code-literal` 225 to 225). `generate.py --check` GREEN, `work.py check` OK.
