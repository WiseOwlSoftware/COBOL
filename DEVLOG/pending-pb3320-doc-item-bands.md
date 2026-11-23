## An Annex A.1 item's band no longer starts before the rule it documents

kb/Work PB3320 (with the BASE85 addendum, PB3322 finding). No mechanical check tied a DOC-A.1 item's `editions` band to the rules
its A.1 text names. Re-probe: a port of the note's `docsweep2.py` to the per-rule store printed 26 of 222 items whose start differs
from the earliest rule they name: 18 start later (each a facility PB3173 decided and PB3322 upheld) and 8 earlier.

The reading is docs/VERSION_TEST_MATRIX_DESIGN.md P2.10: an item's band never starts before the earliest rule row its text names (the
obligation to document a determination cannot predate the rule that leaves it to the implementor); a later start is the
adjudicator's reading; an 85 start means the 1985 text leaves the same latitude (1985 has no Annex A), which answers the BASE85
addendum without re-banding its rows.

Of the 8 early rows, DOC-A.1-68 and -117 name whole clauses, DOC-A.1-64 is the one exemption (§2002:11.8.5.3 GR2 states the item; the
cited rule row carries the 2014 COBOL arm), and DOC-A.1-43, -65, -77, -131 were re-banded to 2002 in one `record_verdicts` batch
(`adjudication/pb3320-doc-item-bands/batch.json`, GAP 2 to 2): CURRENCY SIGN equivalence (§2002:12.2.6.2 SR21), GOBACK
(§2002:14.8.17.3 GR2) and the sharing mode (§2002:14.8.26.3 GR22) are 2002 text.

`DocItemBandDriftTests` holds the early direction (3 of the 4 rows were red before the batch; DOC-A.1-77 chains whole-clause references
and is adjudicated only), with a self-test of the reference reader and a test that fails when an exemption stops being needed.
