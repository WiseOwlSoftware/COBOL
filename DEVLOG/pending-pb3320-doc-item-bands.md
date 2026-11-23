## An Annex A.1 item's band no longer starts before the rule it documents

kb/Work PB3320 (with the BASE85 addendum, PB3322 finding). No mechanical check tied a DOC-A.1 item's `editions` band to the rules
its A.1 text names. Re-probe: a port of the note's `docsweep2.py` to the per-rule store printed 26 of 222 items whose start differs
from the earliest rule they name: 18 start later (each a facility PB3173 decided and PB3322 upheld) and 8 earlier.

The reading is docs/VERSION_TEST_MATRIX_DESIGN.md P2.10: an item's band never starts before the earliest rule row its text names (the
obligation to document a determination cannot predate the rule that leaves it to the implementor); a later start is the
adjudicator's reading; an 85 start means the 1985 text leaves the same latitude (1985 has no Annex A), which answers the BASE85
addendum without re-banding its rows.

Of the 8 early rows, DOC-A.1-68 and -117 name whole clauses; DOC-A.1-64, -43 and -131 are exemptions on a recorded determination of
the behaviour at the earlier edition (DOC-A.1-77 is the same PB670 determination and is not read); DOC-A.1-65 is also exempt: the item is "EXIT and GOBACK statements", its EXIT PROGRAM arm is 1985 text (§1985:II.6.1) and only the GOBACK arm starts at 2002 (review R1-1 reversed my first re-band to 2002)
(GOBACK is §2002:14.8.17.3 GR2 text). No row changes band (GAP 2 to 2). My first batch moved -43, -65, -77 and -131 to 2002; the
first gate refused it (their 85 goldens witness recorded determinations) and review R1-1 showed -65 covers EXIT PROGRAM too, so
`adjudication/pb3320-doc-item-bands/batch.json` keeps -65 at 85 with the reading in its notes.

`DocItemBandDriftTests` holds the early direction, with a self-test of the reference reader and a test that fails when an exemption
stops being needed.
