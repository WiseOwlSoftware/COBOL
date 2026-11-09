## PB4984: a reader finds the 1985 standard's real-word misprints, and the Errata corrects eleven

PB4972's finders saw only a misprint that makes a non-word (`BEFCRE`) or a letter outside ASCII; a misprint that turns
one real word into another needed a reader. The earlier decision (wave 20261937 B) put that reader into the verify
pass. Building it showed the wrong shape: the verify pass reads only SAME answers, its stored answers feed every rule's
disposition and change record, and a new answer field would have withdrawn and re-asked all of them. So
`scripts/spec/audit_batch.py` gains a ninth pass, `misprints`, planned from the 1985 catalog as it now reads, each rule
beside the 2023 rules its map answer paired it with, or none (a NONE rule, read for its sense alone). Its answer is
MISPRINT (printed words, intended words, context) or CLEAN, and `misprints_error` holds each misprint to the rule's own
words, so every lead points at text the page prints. `misprint-leads` lists them. Design section 10.3 records the pass;
the self-test plants its plan, prompt, answer shape (seven refusals) and leads.

The run on `state-route` read all 2,255 rules (1,863 with a counterpart) for $5.57 with no refused reply: 26 leads in 25
rules. Each plausible lead was read on its rendered page. Eleven are printed misprints with an unambiguous sense,
corrected in place as Errata C20 to C30 (`explain`, `a entry`, `referenced modified` twice, `participate`, `assumed`, `to
be right`, `modules`, `If not exception`, `medium reference by`, `printer line`). Two more (`had been execution`,
`current page group`) are corrected by Amendment 2's own list of changes, so they stay as printed. The rest are the
standard's requirement or deliberate wording (the SET rule's `index-name-1`, the MOVE category list, `multiplier`, the
SAME RECORD AREA sentence printed alike in three modules) and stay. The Errata now says what the reader covered and what
it did not (the prose outside the catalog, the index; a rule with no counterpart read for sense alone).

The 1985 catalog is regenerated (ten rule texts), and the ten 1985 change records stored against the old text are
restated against the corrected text through `record_verdicts.py --edition 1985`, decisions unchanged; the inventory
view is GREEN. A lead for the 2002 campaign (PB4813): `specs/ISO_COBOL_2002.md` carries the same `participate only` slip
in INSPECT's AFTER rule, which 2023 corrected.
