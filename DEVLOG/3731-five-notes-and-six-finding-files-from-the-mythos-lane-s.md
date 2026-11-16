## Entry 3731 — 2026-11-15 23:38 PST — Five notes and six finding files from the Mythos lane's batch-6 rulings of 2026-11-15

Filed in kb/Work (documents only, no compiler source, no case, no inventory record). Each defect note carries the rule text with its `cite.py --check` line, the repro program in full, expected and observed at `--std 85` (and `--std 2023`), and the leads.

- PB5813 (defect, MAJOR): INITIALIZE REPLACING treats a reference-modified alphabetic item as alphanumeric (every edition); leads in `MoveOverlap.cs` and `ExpressionBinder.cs` not probed.
- PB5814 (defect, MAJOR): at `--std 85` ADD and SUBTRACT CORRESPONDING evaluate the group operand's subscripts afresh for each pair, where the 1985 text as amended requires once at the start; the documented determination D-CORR85 is wrong for these two verbs. The note names `docs/conformance/3/D-CORR85.md` and the two `pb5185_*_per_pair` cases as what the fix must correct; none is edited here. SUBTRACT CORRESPONDING was not probed.
- PB5815 (defect, MAJOR): a file written with ADVANCING or LINAGE is opened INPUT or I-O with status 00 and read as fixed blocks: wrong records, no AT END, REWRITE over a line end (every edition).
- PB5816 (defect, MAJOR): at `--std 85` UNSTRING from a zero-size sender runs neither overflow phrase, where the 1985 rule makes it an overflow.
- PB5817 (adjudication, open): does a reference that uses another table's index-name for a dimension violate `1985-SR-IV.4.3.8.2.3-3`? The text admits two readings; the same question at 2023 row `SR-8.4.2.3.3-4`.

Six dated finding files beside existing notes: `kb/Work/PB5729/2026-11-15-at-85-and-a-second-symptom.md` (the row `1985-GR-VII.4.4.4-6` joins, with a second symptom: a numeric redefined item gives a C# backend error, CS0103), `kb/Work/PB5785/2026-11-15-the-varying-form.md` (the row `1985-GR-VII.4.4.4-13`, the RECORD VARYING form), the source copied unchanged as `kb/Work/PB5683/2026-11-15-rulings-code-reads-batch-6.md`, and `2026-11-15-more-rows.md` beside each of PB5756, PB5757 and PB5758 (twelve rows the 1985 golden integrators dropped as not closable while those three notes were not yet on main).

Mechanisms 2, 8 and 10 of the batch are REJECTED by the ruling and filed as no note; they live in the ruling copied under PB5683.

**The rate so far at 1985:** 64 of 804 rows read are contradicted (the sample 60, the first population 504, batches 5 and 6 240), about 8 %; 6 undecided on open readings (PB5789, PB5799, PB5800, PB5805, PB5812, PB5817).

