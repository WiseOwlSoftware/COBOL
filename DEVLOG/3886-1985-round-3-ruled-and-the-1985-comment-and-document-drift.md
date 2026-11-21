## Entry 3886 — 2026-11-20 23:41 PST — 1985 round 3 ruled, and the 1985 comment and document drift group lands: eight notes, NOT AT END refusal goldens, every quotation checked

Round 3 of the open 1985 questions (one adjudicator, main at 8244f1aa4b) found two wrong answers on a RECORD DELIMITER IS
LINE-SEQUENTIAL file at `--std 85`. PB5984, reproduced: three WRITE ... AFTER ADVANCING records read back as five, the two extras
of length 0 with status 04, because the ADVANCING write treats the file as a print stream and writes the delimiter as
positioning. PB5915: a REWRITE with a shorter record returns 00 and pads the line, where §1985:VII.4.5.4 GR10 requires 44 and the
file left unchanged. Both are in the line-sequential connector and are planned as one group. PB4244 (statement verbs missing
from a condition-boundary list) misparses no legal 1985 program and is retired; PB5321's code is right and its two documents
are owed their corrections. A triage of the 37 process-only notes that touch 1985 found 25 still true, which now sit in three
fix groups (the citation checkers, comment and document drift, two missing goldens) and one held group (catalog tooling, which
shares files with PB5449). PB657 and PB765 are obsolete and PB4872 is done: retired.

The comment and document drift group (wave 20262122, group A) then landed in this change, each note re-probed on the branch's
build and every citation written run through `cite.py --check`:

- **PB5321.** The code already agreed with every edition's text (the ODO-group SORT and MERGE key goldens at `--std 85` draw
  COBOL1757 quoting §1985:XI.4.4.3 SR4c / §1985:XI.4.1.3 SR4c). D-SKE now rests on each edition's own rule, not on GnuCOBOL and
  Micro Focus; D-IO-STATUS-07-OPEN-CLOSE and VCR row 22 quote the 1985, 2002 and 2014 '07' definitions, which name only OPEN and
  CLOSE, and read E.2 item 16 as 2023's tightened lead-in. Row 22 stays `ref-only`.
- **PB5520, PB5545.** `EvaluateOperandCombinations.IsPermitted` quoted "a valid operand for comparison ..." as §1985:VI.6.13.3
  SR6a, text the 1985 standard does not hold; it now quotes "must be valid operands for comparison ...". The GR1b quotation was
  genuine and gains its sub-item. The sweep fixed the same quotation in docs/conformance/3.12.
- **PB5521.** The `negatedAbbreviatedRelation` grammar comments, and the gate's summary, said the node is the 85-only form that
  2002 refuses; they now describe its two shapes (2002 keeps `NOT IS > C` with the NOT in the operator, §2002:8.8.4.2.4.3 GR2b).
- **PB5525.** NOT AT END alone on a dynamic relative or indexed file had no refusal golden. Two now refuse it at 85 (COBOL7144,
  each with a USE procedure so it is the one diagnostic), and `pb5525_dynamic_not_at_end_implies_next` runs it as the implied
  NEXT read at 2002, 2014 and 2023. Found on the way: the relative refusal quoted the indexed rule's comma; each organization now
  quotes its own SR.
- **PB5546.** The `AreaPlacementPass` summary placed area-A items by where they begin their line and pinned §1985:IV.7.3.3 (the
  paragraph header's rule) on all of them; it now says they are placed by where they start, with DECLARATIVES and END DECLARATIVES
  the line-alone exception (§1985:IV.7.5). A section header after a paragraph on its line compiles and runs at 85, so the END
  DECLARATIVES golden no longer claims only a paragraph-name may follow text.
- **PB5634.** DOC-A.1-71, row 1985-FMT-XI.2.3.2 and `FileControlKeyRules.Screen` count three ASSIGN refusals; the key resolver
  local `InRecords` is `ResolveKeyOperand` with its stale SR2 comments rewritten; pb5421's golden tags the Format 2 SET rules it
  exercises; three unused `warns` locals are discards again.
- **PB5055.** PB5046's `closes_rows_reason` names its own row, 1985-SR-XIII.3.19.3-3, and PB5047's drops a pointer to a landed note.

Rows: the new goldens join `1985-SR-VIII.4.5.3-3`, `1985-SR-IX.4.5.3-5`, `SR-14.9.30.3-9`, `GR-14.9.39.4-3` and `-4` (all
CONFORMS); GAP unchanged. No compiled program changes behavior except the COBOL7144 message's quotation for a relative file.

