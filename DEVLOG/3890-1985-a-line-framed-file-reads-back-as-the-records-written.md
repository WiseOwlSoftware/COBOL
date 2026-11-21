## Entry 3890 — 2026-11-21 01:18 PST — 1985: a line-framed file reads back as the records written, and its REWRITE keeps the record's size (PB5984, PB5915); round 3's rulings and the 1985 tooling triage

Round 3 of the open 1985 questions (one adjudicator, main at 8244f1aa4b) found two wrong answers on a RECORD DELIMITER IS
LINE-SEQUENTIAL file at `--std 85`. PB5984, reproduced: three WRITE ... AFTER ADVANCING records read back as five, the two extras
of length 0 with status 04, because the ADVANCING write treats the file as a print stream and writes the delimiter as
positioning. PB5915: a REWRITE with a shorter record returns 00 and pads the line, where §1985:VII.4.5.4 GR10 requires 44 and the
file left unchanged. Both are in the line-sequential connector and were planned as one group. PB4244 (statement verbs missing
from a condition-boundary list) misparses no legal 1985 program and is retired; PB5321's code is right and its two documents
are owed their corrections.

A triage of the 37 process-only notes that touch 1985 found 25 still true, which now sit in three fix groups (the citation
checkers, comment and document drift, two missing goldens) and one held group (catalog tooling, which shares files with
PB5449). PB657 and PB765 are obsolete and PB4872 is done: retired. The rest belong to PB479, to another edition, or to an
upstream report.

**The fixes (wave 20262120, group A).** Both re-probed on this branch's build before any change: the AFTER 1/2/1 program
read five records, BEFORE 1/2/1 four, AFTER 0 / PAGE / 0 one record of 19 characters with the form feed and the carriage
return inside it; the short REWRITE answered 00 and left `XYZ  ` in the file.

One property, `SequentialConnector.LineDelimiterOnlyFrames` (a line-framed connector of an edition before 2023, the other
side of PB5903's `LineSequentialFileRules`), now answers both. On the write side the vertical positioning has no image on
that medium: `Present` writes the record and its delimiter and nothing else, and the two travel primitives `Advance` and
`AdvanceLines` write nothing, so each WRITE releases exactly one line in WRITE order (§1985:VII.4.7.4 GR12, §1985:VII.2.6.3
GR2), a plain WRITE and AFTER ADVANCING 1 LINE write the same file (GR16), and a LINAGE page still moves LINAGE-COUNTER and
raises its end-of-page conditions. On the REWRITE side `RewriteRecord` dispatches once to the new `RewriteFramedRecord`: '44'
with nothing changed unless the record has the character positions of the whole line the READ took (§1985:VII.4.5.4 GR10,
§1985:VII.1.3.5), so the REWRITE after a truncating READ is '44' by GR10 itself rather than by 2023's GR17 a). The
determination (the positioning is not represented on a delimited file before 2023; GnuCOBOL's blank lines contradict GR12
and are not taken) is in `docs/conformance/7/DOC-A.1-150.md`, with VCR row 7.88, `DialectBehavior.LineSequentialFileRules`
and `docs/FILES_DESIGN.md` carrying the two new edges. The 2023 line sequential file is unchanged.

Goldens at 85: `VII.4.7@1985-WRITE-STATEMENT/pb5984_line_delimited_advancing_one_record_per_write_85` (three passes, ten
WRITEs, every read-back exact), its LINAGE twin `pb5984_line_delimited_linage_one_record_per_write_85` (LINAGE-COUNTER 2, 1, 1, 1 and three end-of-page imperatives, computed from §1985:VII.3.7.4 GR9, four records read back) and `VII.4.5@1985-REWRITE-STATEMENT/pb5915_line_delimited_rewrite_size_85` (shorter 44,
longer 44, equal 00 and replaced, after a truncating READ 44, the file read back). No inventory row was re-verdicted: the
1985 rows these rules hold are KEPT bridges or CONFORMS. Branch review round 1 (R1-1) found the REWRITE's two other refusals unpinned; `pb5915_line_delimited_rewrite_medium_85` now pins them and their order: a record of the line's character positions whose characters take more or fewer UTF-8 bytes is 44, a record holding the line delimiter is 99 (2023's '71' reported at 85), and a record that does both is 44, each leaving the file unchanged.

