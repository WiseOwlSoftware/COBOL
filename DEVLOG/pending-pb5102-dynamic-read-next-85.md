## PB5102: a COBOL-85 READ of a dynamic-access file that writes AT END must write NEXT

**The defect.** At `--std 85`, `READ file RECORD AT END ...` on an ACCESS MODE IS DYNAMIC indexed or relative file compiled
clean and ran as the implied-NEXT sequential read that COBOL-2002 and later define (§2023:14.9.30.3 SR9). The 1985 READ
prints AT END in format 1 only, the sequential retrieval, and §1985:IX.4.5.3 SR5 and §1985:VIII.4.5.3 SR3 say "The NEXT
phrase must be specified for files in dynamic access mode, when records are to be retrieved sequentially".

**Re-probe.** Both repros of the note (`1985-SR-IX.4.5.3-5`, `1985-SR-VIII.4.5.3-3`) compiled with exit 0 on this tree and
printed `BRAVO` and `AAAA`; after the fix both exit 65 with COBOL7144 at the READ line, and the same indexed program at
`--std 2002` is refused only for its LABEL RECORDS clause.

**The fix.** One rule, `StatementValidation.CheckReadDynamicNextWritten`, asked by the one keyed `KeyedIoBinder.BindRead`
(both organizations pass through it; a dynamic file cannot be SEQUENTIAL organization, so the sequential binder has no
sibling arm). It refuses, below 2002, a READ of a DYNAMIC file that writes AT END or NOT AT END without NEXT. The bind
continues as the implied NEXT so later diagnostics are not hidden. A bare READ and a READ ... INVALID KEY on a dynamic file
stay the format-2 random read, which the 1985 module allows. New code COBOL7144 (`read-dynamic-next-required`).

**Goldens.** Two refusals at 85 (`1985-SR-IX_4_5_3-5`, `1985-SR-VIII_4_5_3-3`, with their `wiseowl/diagnostics` files) and
one accepted program at 2002, 2014 and 2023 (`14.9.30-READ/pb5102_dynamic_at_end_implies_next`: ALPHA, BRAVO, EOF) that
pins the later editions' implied NEXT. Rows `1985-SR-IX.4.5.3-5` and `1985-SR-VIII.4.5.3-3` recorded CONFORMS
(GAP 229 to 227).

**Group note.** The dispatch also named `MoveBinder.BindIntoPhrase` and `SortEmitter.EmitReturn`; PB5102 has no site there
(those belong to the landed PB5077), so nothing was changed in them.
