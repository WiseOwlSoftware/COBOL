## An Annex A.1 item's band no longer starts before the rule it documents

kb/Work PB3320 (with the BASE85 addendum, PB3322 finding). No mechanical check tied a DOC-A.1 item's `editions` band to the rules
its A.1 text names. Re-probe: a port of the note's `docsweep2.py` to the per-rule store printed 26 of 222 items whose start differs
from the earliest rule they name: 18 start later (each a facility PB3173 decided and PB3322 upheld) and 8 earlier.

The reading is docs/VERSION_TEST_MATRIX_DESIGN.md P2.10: an item's band never starts before the earliest rule row its text names (the
obligation to document a determination cannot predate the rule that leaves it to the implementor); a later start is the
adjudicator's reading; an 85 start means the 1985 text leaves the same latitude (1985 has no Annex A), which answers the BASE85
addendum without re-banding its rows.

Of the 8 early rows, DOC-A.1-68 and -117 name whole clauses; DOC-A.1-64, -43 and -131 are exemptions on a recorded determination of
the behaviour at the earlier edition (DOC-A.1-77 is the same PB670 determination and is not read); DOC-A.1-65 was re-banded to 2002
(`adjudication/pb3320-doc-item-bands/batch.json`, GAP 2 to 2): GOBACK is §2002:14.8.17.3 GR2 text and 1985 has no GOBACK statement.
The first gate refused my first batch, which also moved DOC-A.1-43, -77 and -131: their 85 goldens witness recorded determinations
(PB670's option (i) and DOC-A.1-43's bare form), so those rows were restored and exempted instead.

`DocItemBandDriftTests` holds the early direction, with a self-test of the reference reader and a test that fails when an exemption
stops being needed.
