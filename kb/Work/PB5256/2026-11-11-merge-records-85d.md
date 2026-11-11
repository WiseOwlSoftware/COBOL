## Merge of main after records-85d: the 1985 digests held, and the rule for the next records branch

**Sites:** `tests/version-matrix/inventory-1985/` (128 records records-85d changed, merged), `scripts/spec/spec_pdf.py`
(`EDITIONS`: the 2014 row gains `pages`), `docs/VERSION_CHANGE_REFERENCE.md` (the held-texts paragraph),
`docs/rearchitecture/spec-rule-catalog-2002.json` (regenerated), `scripts/spec/lineage.py` (`pairing_text`, `_HARD_BREAK`,
`_ENTITY`, `self_test`), `docs/rearchitecture/spec-lineage-candidates-2002.json` (regenerated),
`docs/rearchitecture/DESIGN-spec-conformance-review.md` (the lineage seed).

**What main brought.** origin/main 4d78809a58, 19 commits past the merge base d9dde0e7ba: records-85d (kb/Work PB4996: 116
refuter-upheld verdicts of 1985 code-read slices and 12 re-read rows, 128 store files), the 2014 transcription (kb/Work
PB5268) and kb/Work PB5121. records-85c landed before d9dde0e7ba, so its records were among the 1,937 this branch stamped.

**The 1985 store needed no restamp.** records-85d changed no record's `next`: it added `verdict`, `code-location` and
`notes` lines, which git merged beside this branch's `next.digests` without a conflict. Checked over the merged tree by a
comparison that reads each file: 2,260 1985 records; 1,937 name `to` rules, and each carries `next.digests` equal to its
targets' current `rule-digest` in the 2023 view; each of the 128 files main changed equals main's record plus exactly this
branch's digests (0 differ, 0 with a changed `next`). The INVENTORY VIEW audit over the merged tree is the mechanical
witness (the merge's DEVLOG entry and commit carry its verdict line).

**Two conflicts, each two additions to one line.** `spec_pdf.EDITIONS`: this branch's sixth `Edition` field, `pages`, meets
main's new 2014 row; 2014 takes 952, measured with PyMuPDF from `ISO_COBOL_2014.pdf` at the specs-private commit main records
(0727a70), the measure that reproduces 2002's 882 and 2023's 1,261. `VERSION_CHANGE_REFERENCE.md`: main's paragraph, which
now names the 2014 text, with this branch's sentence on the 2002 catalog, inventory and lineage candidates kept after the
2002 sentence.

**The 2002 catalog went stale, and the pairing's normalization missed the transcription's markup.** PB5268 also edited
`specs/ISO_COBOL_2002.md`: it split run-on dash lists into one item per line, each ending in a Markdown hard break, indented
the wrapped lines with `&emsp;`, and dropped a page footer. RULE CATALOGS went RED on 2002 (5 rules and the findings'
line numbers differ from what the transcription yields); `extract_rule_catalog.py --edition 2002` regenerated it (10 lines;
no 2002 rule is adjudicated, so no stored decision rested on the old text). The regenerated pairing then LOST two exact
pairs whose words are unchanged, 2002-SR-13.16.50.2-1 and 2002-GR-14.1.3-10: `pairing_text` erased an escape (a backslash
before punctuation) but not a hard break (a backslash before white space), so `'S'\ —` never met 2023's `'S' —`. The
transcription's markup is typography, so `pairing_text` now reads a hard break as white space and a character entity as its
character (a self-test arm plants both; `A & B`, `a &amp b` and `a\b` stay unequal to their bare forms). Re-derived 2002 to
2023: 1,771 paired (1,730 exact, 41 normalized, 2 with two candidates), 1,519 unpaired, against 1,758 (1,732, 26, 2) and
1,532 before the merge. No earlier pair was lost or re-targeted: the two rules keep their targets as `normalized`, and the
13 pairs gained each carry a hard break or `&lt;` that alone separated them (2002-SR-8.7.5.2-5 and -8, 2002-GR-14.8.12.3-4,
2002-RV-15.18.3-1, among them). The 1985 pairing is unchanged (none paired).

**The gate found a sibling R1-4 missed: a self-test that plants a 1985 change record.** The first gate of the merged head
(run 000005-20261111T155134Z at cf97b0fb09) was RED in its SELF-TESTS, 1 of 87: `audit_witness_loss.py --self-test`, three
arms of "writer at 1985" (PB4995's witness-before-verdict, already on this branch before the merge, so not a merge
interaction). Its planted CHANGED record named `to: ["SR-1"]` with no `next.digests`, which R1-4's `next_errors` now refuses;
no earlier gate of this branch had reached the self-tests (each was withdrawn in the slot queue). The planted targets now
carry a `rule-digest` and the planted records into them carry digests stamped by the writer's own `lineage.target_digests`,
so the arm follows the writer; the KEPT arm is refused for its own reason alone. The other 86 self-tests were green in that
run, so the sweep for planted change records found no other.

**THE RULE FOR THE NEXT RECORDS BRANCH (records-85e and every one after it).** A 1985 change record that names `to` rules
carries `next.digests`, the `rule-digest` of each target as the 2023 view holds it when the record is written.

- **The command that writes them** is the one writer, `python scripts/spec/record_verdicts.py --edition 1985 <batch.json>`.
  It stamps `next.digests` on every batch record whose `next` names `to` rules and carries none (`lineage.target_digests`),
  and checks, never overwrites, digests a batch supplies. A batch of verdicts alone (no `next`) leaves the stored record's
  `next`, digests included, as it is.
- **A branch cut before this one lands** whose batches write `next` re-runs those batches through the writer after it merges
  main, so each record it wrote without digests gains them. A record whose `next` it changed while a 2023 target's text
  also changed is STALE and is re-decided against the text as it is, never restamped by hand.
- **The audit that refuses it** is INVENTORY VIEW, `python scripts/spec/inventory_view.py --check` (a gate audit,
  `gate.py` `AUDITS`, and a step of CI's audits job). It runs `lineage.next_errors` on every 1985 row and fails on
  `next.digests is missing`, a digest of a rule `next.to` does not name, a target with no digest, and a STALE target. The
  writer runs the same `next_errors` on each record before it writes, so a batch that supplies a stale digest writes
  nothing.
