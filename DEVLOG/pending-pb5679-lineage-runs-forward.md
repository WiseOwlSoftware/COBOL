## PB5679: the rule lineage runs forward: the decision, the switch's requirements, the open questions

The owner decided on 2026-11-15 that a later COBOL edition includes an earlier edition's rule as is, alters it, removes
it or adds a new one, so a lineage relation belongs to the later edition's row and names its predecessor. Today every
stored change record sits on an older row and names 2023 over a bridge (2,260 1985 records and 704 2002 records), and a
KEPT row takes its targets' state; the endpoint comparison was wrong for 16 and 13 of 100 word-identical 2002 pairs in
two blind samples. This landing adds DESIGN-spec-conformance-review.md section 10.7 in three parts. DECIDED: the
relation on the successor; a row closes on its own record and witness, no state reads another row; a verdict's
provenance (adjudicated or shared) is recorded and shown, and nothing is shared where line-mates disagree; 1985 rows end
adjudicated; one record format at every landed head, changed in one switch; 2014 planned only, as the owner asked.
REQUIREMENTS the switch's design must meet, each with the acceptance case and command that show today's behaviour (the
gate proofs of today kept, among them `end-accept-2002` held open and the 9 edges at 2014 and 2023 on 1985 records; every
row's state identical; no 2002 text read presented as an adjudication). OPEN: whether a shared verdict is enough to close
a row (PB5692), the switch's mechanics (PB5682), and the `examined` field with the tool-written difference trigger
(PB5693). It designs no switch: two adversarial review rounds broke every mechanism drafted for it on real records (the
many-to-many 1985 KEPT records, the general-format gates, the 2014 edges). It files decision PB5679, steps PB5680 to
PB5684 and analyses PB5692 and PB5693, and notes beside PB5574, PB5583 and PB4991 what it supersedes. Of the 312 proven
edition gates (`edition_gates.py --list`) 24 rest outside the lineage, 38 on a 1985 CHANGED or REMOVED record, 198 on a
2023 rule NEW against 1985 and 52 on 2023 rules a 1985 CHANGED record names. No code, schema data, test or inventory
record changes.
