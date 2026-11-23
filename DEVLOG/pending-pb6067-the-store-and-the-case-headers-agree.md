## The inventory's witnesses and the suite's case headers agree, and generate.py refuses them differing (PB6067)

PB6067 named three gaps the 1985 widening left. Re-probed on main ccb23d13e4:

1. **Free form at 85 in a refusal test.** The note's premise was stale. The 1985 inventory names one free-form case, already
   retired (PB5754), and `lineage_reads.format_problem` refuses a free-form witness at the writer, the audit and the planner.
   DESIGN-conformance-suite section 3.6 already rules that about 1,150 free-form refusal tests may refuse at 85 as regression
   tests of `--source-format free`, witnessing no row. Nothing in `check_editions` changed; the section 4 list now says "ACCEPTED
   or FLAGGED" so it matches section 3.6.
2. **22 cases at `accept: [85, 2023]`.** Each was run at 2002 and 2014 with `run_probe.py --expect` against its `.out`: all 44
   runs MATCH. Their `accept` is now `[85, 2002, 2014, 2023]`; `generate.py --check` accepts every rule at those editions.
3. **The store named 172 cases (285 pairs) whose `rules` did not cite the row.** The headers now cite the 2023 rows the store
   names them for (263 pairs). The other 22 pairs name a case accepted at an edition before the row's band (the `_85`
   user-word gates and the like), which the generator forbids a case to cite, so they stay as the store has them. The rule
   is now enforced: `_check_store_witnesses` (generate.py) refuses a case a row names as its own witness when the case's `rules`
   hold neither the row nor, for a chained older row, a rule it is KEPT into, unless no cite of the row could hold the case
   (accepted outside every candidate's band). The self-test plants one refusal and one exempt gate case. The check found nine
   chained 1985 rows whose own witness was another rule's test (for example `pb5889_run_time_check_85`, which uses MOD and ORD,
   listed under the FACTORIAL, RANDOM and CHAR rows); each is retired through `record_verdicts.py --edition 1985` and keeps
   at least one other own witness (GAP 0 to 0). The 1989 function rows now cite their 2023 targets in the cases that really
   use FACTORIAL, MOD, RANDOM and CHAR.

Rows closed: none (`closes_rows: []`, with its reason). Leads: the 2002 rows whose only witness is a case that cannot cite a
2023-only band (for example 2002-SR-13.16.33.2-2 against `pb1221_report_keyword_prefixes`) wait for the 2002 band widening.
