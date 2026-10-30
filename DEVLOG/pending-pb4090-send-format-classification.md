## PB4090 and PB4085: a SEND names its format once, so a server-name operand needs RETURNING and a phrase of each format is refused

**Mechanism.** §14.9.38.2 prints SEND as two formats the grammar merges into one rule: Format 1 (to-message-server) is
`SEND TO { literal-1 | message-server-name-1 } FROM identifier-1 RETURNING data-name-1`, Format 2 (message-server-response)
`SEND TO data-name-2 FROM identifier-1 [ RAISING ... ]`. PB2024's COBOL1850 screen knew the format only from a literal-1
operand, and the binder never read the TO name, because as a message-server-name-1 (§14.9.38.3 SR2) it is no data item.
Re-probed on the tree before the fix: `SEND TO SRV FROM X END-SEND`, `SEND SRV FROM X END-SEND` and `SEND TO TAG FROM X
END-SEND` (TAG a PIC X item) all compiled, and so did RETURNING together with RAISING, on a name and on a literal operand
(PB4085 defect 2). PB4085 defect 1 is PB4090 verbatim, so one change closes both notes.

**Fix.** `DeclinedFacilityBinder.ClassifySend` assigns the written SEND its format in one place. A literal-1 operand and
RETURNING are Format 1's evidence; RAISING and a TO name that silently resolves to a MESSAGE-TAG item are Format 2's.
The name is read through the new `ReferenceResolver.ProbeDeclaration`, a silent declaration lookup that builds no Place
(a MESSAGE-TAG item has none, so `Probe` could not answer). Evidence for both formats is the new COBOL5488
(`SendPhrasesOfTwoFormats`), once, and that statement is asked no per-format rule, so there is no cascade. Evidence for
neither is Format 1, because a name that is no message-tag item can only be message-server-name-1; its missing RETURNING
is COBOL1850, now for a server-name operand as well as a literal. A Format 2 TO name is a data-name-2 and resolves (an
undefined one is COBOL1639); a Format 1 name stays unresolved, so an undefined server name is no error. Below 2023 the
introduction gate (COBOL0900) answers first, as for every SEND.

**Design decisions recorded.** (1) The grammar stays one rule. PB4085 suggested one rule per format; the name after TO is a
bare word in both formats and the format is a fact about its declaration, so a split rule would still leave
`SEND TO name FROM x` to a binder lookup. The rule's comment and docs/CONFORMANCE.md §4 item 1 say so. (2) SR4's "shall be
the name of a message-tag data item" is not screened for data-name-1 or data-name-2: the usage is refused by name
(COBOL1943), so no compiled program holds such an item, and the existing goldens write a PIC X operand. Screening SR4 would
make every SEND with RETURNING unwritable in an accepted program; it is a decision for the owner, not a side effect here.

**Goldens.** `conformance-suite/wiseowl/cases/14.9.38-SEND/pb4090-*`: one accepted at 2023 (Format 1, undefined server
name, TO omitted; output derived from the spec: nothing is sent, so the reply keeps its VALUE) and five refused at 2023
(COBOL1850 for a bare server name; COBOL5488 for RETURNING with RAISING, a literal with RAISING and a message-tag TO name
with RETURNING; COBOL1639 for an undefined Format 2 name). `SendFormatClassificationTests` pins the absences no golden can:
a message-tag TO name needs no RETURNING, a mixed statement draws no COBOL1850, an undefined server name is no COBOL1639,
and below 2023 only COBOL0900 answers. Five existing programs wrote `SEND TO name FROM x` with no RETURNING as a "Format 2"
spelling with a PIC X name and now draw COBOL1850; each was rewritten as Format 1 (`optional_words_mcs`,
`wave_h_facilities_inert`, `pb374-async-messaging-below-2023`, `pb2024-send-end-send-omitted`, and the `async-messaging-2023`
source in `constructs.json`). The roster in `DeclinedFacilityOperandDriftTests` now counts SEND's TO operand as resolved.

**Rows.** None closed: the MCS SEND rows carry the DOCUMENTED-NON-SUPPORT verdict of their declined facility and stay so.
