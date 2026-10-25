## Entry 2581 — 2026-10-25 15:59 PDT — PB3750 — status 91 is fatal by WiseOwl's definition, not by its first digit

A witness-test comment in `DocA1Item103WitnessTests.cs` (added with PB3681) said I-O status 91 is fatal "by its first digit (§9.1.13.1)". §9.1.13.1 makes a status beginning with 3, 4 or 7 fatal by its first digit, but a status beginning with 9 only when the implementor defines it as fatal. 91 is fatal because WiseOwl COBOL defines it so (DOC-A.1-110).

Re-probe: the comment reproduced as the note says, and the DOC-A.1-110 row of `docs/CONFORMANCE.md` §7 carried the same phrase ("a '9' first digit, §9.1.13.1") in its sentence about 91. Both are reworded to the rule's own text (`cite.py --check 9.1.13.1` OK). The other comments in the witness file reason from first digits 3, 4 and 1 only, which are right; a repository-wide search for the phrasing found no other site.

Comment and documentation text only: no behavior change, no golden, no inventory row (`closes_rows: []` with its reason). Self-review of the diff (comment-analyzer lens): the new wording states the rule, names the determination row and keeps the status-to-condition fact (EC-I-O-IMP) separate from the fatality.

