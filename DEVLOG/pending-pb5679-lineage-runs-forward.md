## PB5679: the rule lineage runs forward, from each edition to the next

The owner decided on 2026-11-15 that a later COBOL edition includes an earlier edition's rule as is, alters it, removes
it or adds a new one, so the record of that act belongs to the later edition. Today every stored change record sits on
an older row and names 2023 over a bridge (2,260 1985 records and 704 2002 records), a shape that exists only because
2023 was adjudicated first; the endpoint comparison was wrong for 16 and 13 of 100 word-identical 2002 pairs in two blind
samples. This landing adds the design, DESIGN-spec-conformance-review.md section 10.7 (staged): the successor row's
`from` record, the line of as-is hops as the unit, one adjudication per line held where it was made with an own-edition
witness on every member, own verdicts for every 1985 row, one mechanism per neighbour pair, and ONE switch that moves
every stored record to the new format, re-derives the edition gates and deletes `next` in the same change, so no landed
head carries two formats. The stages before the switch work in today's format (1985 own verdicts, neighbour-pair reads
kept as tracked read records), and the switch's preconditions are counts a command prints. 2014 is planned and waits
until 2023, 2002 and 1985 are complete (the owner, 2026-11-15); until then the 2002-to-2023 hop is a skipping hop naming
2014, and nothing before or in the switch needs a 2014 catalog. It files decision PB5679 and its steps PB5680 to PB5684,
and notes beside PB5574, PB5583 and PB4991 what it supersedes. Measured on the way: the 1985 KEPT records are many to
many (451 of 1,373 name two or more 2023 rules), the 2002 KEPT records one to one, and all 312 proven edition gates rest
on the 1985 records today. No code, schema data, test or inventory record changes.
