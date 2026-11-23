## Entry 3965 — 2026-11-23 03:08 PST — PB6098, PB6102: the inventory schema has one reader and a drift assertion; the docs say what the landing check stops

**PB6098, the inventory schema's one reader.** DESIGN-architecture-review §8.2 rule 7 says a script reads the inventory
schema's vocabulary through `inventory_schema.py` and never parses the JSON a second time or splits a list field by hand.
Nothing enforced it, and the rule's own sentence said every reader's self-test planted a schema, which held only for
`normalize_batch`. The re-probe on a23ed881c2 found both of the review's sites still in place: `rename_diagnostic_prefix.py`
parsed the schema for the retired-witness mark, and `normalize_batch.py` split the test-ref and code-location fields inline.
The new drift assertion, `scripts/spec/schema_readers.py --check`, reads every live Python file's parse tree. Run over
origin/main's copies, it reports 11 findings in 6 scripts. Four of them are the conformance generator's editions splits, and
the others are `session_probe.py`'s path probe and the parses in `inventory_view.py` and `audit_witness_loss.py`. Another
11 parses went through the `SCHEMA_PATH` constant or an alias of it, which a one-call scan cannot see.

The fix: `inventory_schema.raw_schema(tree)` is now the one parse of the file. It returns a fresh dict, or None for a tree
without the file. `SCHEMA_PATH` is deleted, and every parse, production and self-test alike, calls `raw_schema`. The retired
mark is `RETIRED_WITNESS_MARK`. Every hand split now calls `Schema.split`. `session_probe.py`'s check for a missing schema
dated from before P14 Step 0 and is deleted. `schema_readers.py --check` has three rules (PATH, PARSE and SPLIT), and it
reads the separators from a real `Schema`, so a separator the schema adds is covered with no edit. Its self-test
(`scripts/selftests/spec/schema_readers/`) plants each kind of finding and each harmless prose shape, then runs the check
over the tree. Rule 7 now names the assertion, the measured count and the residue: an alias of `SCHEMA_REL`, or a read and
a parse on separate lines, escapes the scan. Found on the way and fixed: since 2b0dc0fbe1, `normalize_batch` had dropped the
commentary of every ref it kept. `normalize_ref` handed the commentary back as a note, but the loop read the note only for a
dropped ref. A bare `#Method` lost its remark the same way, and a bare `#` after a ref raised IndexError. All three now reach
`notes` (`case_02_a_kept_refs_remark_goes_to_notes`).

**PB6102, what the landing check stops.** The landing check (`landing_check.py`, which `enqueue.py checks` runs as exit 3)
stops a landing for only two reasons: a path no landing carries, or a public-API baseline that lists a symbol twice. Since
PB4452 (first ready lands), a branch whose edits meet another branch's is only named in a warning. Several documents still
described the older rule. The architecture-review skill, MANDATORY-PRACTICES L12, `references/landing.md` (exit 3 "retried
once landing_check.py passes") and the design's section 8.7 (its heading and two sentences) all needed correcting. So did the
paragraph `file_census_notes.py` generates ("stops a landing that changed a file outside it"). The sweep found three more
stale statements: `plan_wave.py`'s docstring, `dispatch_ledger.py`'s two descriptions of `at` (it is `best_group`'s last
tie-break, not an order that makes a later branch re-plan) and a plan §0 bullet. All now state the script's rule. The
generated paragraph appeared in 164 notes. In the 88 open notes, which an implementer reads as its contract, it was
rewritten mechanically: one sentence, one line per note. The 74 landed and retired notes keep the old paragraph as their
record. PB6102 records that decision.

Self-review: the four lenses found one mechanism per job (one parse, one splitter, one assertion), no second copy of a
rule, and a scan that parses only the files that contain one of the words a finding needs (0.8 s instead of 5 s). Gate: see
the report.

