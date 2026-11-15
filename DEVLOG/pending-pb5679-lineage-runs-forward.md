## PB5679: the rule lineage runs forward, from each edition to the next

The owner decided on 2026-11-15 that a later COBOL edition includes an earlier edition's rule as is, alters it, removes
it or adds a new one, so the record of that act belongs to the later edition. Today every stored change record sits on
an older row and names 2023 over a bridge (2,260 1985 records and 704 2002 records), a shape that exists only because
2023 was adjudicated first; the endpoint comparison was wrong for 16 and 13 of 100 word-identical 2002 pairs in two blind
samples. This landing adds the design, DESIGN-spec-conformance-review.md section 10.7 (staged): the successor row's
`from` record, the line of as-is hops as the unit, a state that reads no other row (every row its own verdict and
own-edition witness; a line shares the analysis, never the verdict, and a LINE CONSISTENCY audit reddens line-mates
that disagree), a later-found difference recorded as a screen trigger that reddens every hop it touches, one mechanism
per pair of editions, and ONE switch that moves every stored record to the new format, gives each row closed today on a
read-backed KEPT record its own verdict, re-derives the edition gates and deletes `next` in the same change, with every
row's state proven identical before and after. The steps before the switch work in today's format and withdraw
nothing, and the switch's preconditions are counts a command prints. 2014 is planned and waits
until 2023, 2002 and 1985 are complete (the owner, 2026-11-15); until then the 2002-to-2023 hop is a skipping hop naming
2014, and nothing before or in the switch needs a 2014 catalog. It files decision PB5679 and its steps PB5680 to PB5684,
and notes beside PB5574, PB5583 and PB4991 what it supersedes. Measured on the way: the 1985 KEPT records are many to
many (451 of 1,373 name two or more 2023 rules), the 2002 KEPT records one to one, and of the 312 proven edition gates
(`edition_gates.py --list`) 24 rest outside the lineage, 38 on a 1985 CHANGED or REMOVED record, 198 on a 2023 rule NEW
against 1985 and 52 on 2023 rules a 1985 CHANGED record names. No code, schema data, test or inventory record changes.
