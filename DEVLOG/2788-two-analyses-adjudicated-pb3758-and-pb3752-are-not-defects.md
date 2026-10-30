## Entry 2788 — 2026-10-30 11:12 PDT — Two analyses adjudicated: PB3758 and PB3752 are not defects, and four new notes record what the adjudications found (PB4107-PB4110)

PB3758 said INVOKE method matching wrongly refuses a restricted program-pointer passed BY REFERENCE into a formal
restricted to another prototype, since SET and CALL accept the pair. The first adjudicator agreed, and a refuter
overturned it. A third adjudicator, reading both, decided for the refuter. The BY REFERENCE and clause-identity rules
require identity: §14.8.2.3.2 says both pointers are "of the same type" (§8.5.3.1: the same type-name), and §9.3.6 3 e)
and §14.8.3.3 say "the same USAGE clauses". Only SET and CALL SR14 compare signatures. So INVOKE's refusals are correct,
and PB3758 closes with no code change. The defect runs the other way and is filed as PB4107 (under-rejects). The
BY REFERENCE and RETURNING checks of CALL and INVOKE answer the identity rules with the signature relation and never
compare the items' own type. Three landed goldens and docs/OO_DESIGN.md encode the accepting behavior.

PB3752 listed six optional words the grammar audit flagged. The adjudicator and the refuter each rendered the printed
format pages and probed every spelling, and all six conform. The refuter found two defects beside them.
- PB4108 (rejects legal source): SYMBOLIC CHARACTERS refuses `SC1 SC2 66 K2`, a literal ordinal before a constant-name
  ordinal, because the grammar ends the entry at the last literal and the binder regroups entry by entry.
- PB4109 (misleading diagnostic): a multiple LINE clause on a GENERATEd level-01 report group reports the group as
  described twice in one RD.

PB4110 records the audit's three causes of the false candidates. It ignores another alternative of the same rule, it
maps §13.18.19.2 to the wrong rule, and it has no marker for the binder's IS/ARE decision. That marker waits for PB4108.
Every quote was checked with `cite.py --check`; the rulings are under `E:/COBOL-coord/scratch/adj/`.

