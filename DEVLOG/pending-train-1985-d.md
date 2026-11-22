## 1985 train D: five fix branches in one landing; the 1985 inventory reaches zero GAP

Wave 20262132's 1985 train lands in one pull request. It carries the fix branches of groups c (`train85d-register`), e
(`train85d-itemcite`, with group s's `train85b-g3-cite` and group c), a (`train85d-examples`), b (`train85d-witness`) and d
(`train85d-iostmt`). They were merged into `train-1985-d` on top of train C (`train-1985-c`, followed to c2a1ebbdba) and gated
once. The train's rulings come from the controller (account 2, 2026-11-21). D-A says the lineage switch and PB4996 do not
block 1985. D-B gives 1989-RV-A.2.46.3-3 its own listing witness. D-C makes the 2023 catalog regeneration 2023's work alone.
`edition_status.py --edition 1985` read `1 inventory GAP · 4 code reads · 33 open work notes · 16 catalog prose holes` on
train C's head, and reads `5 code reads · 13 open work notes` on this train's head: 0 GAP and 0 prose holes.

**The register catches up (c, PB5924).** PB4995, PB4886 and PB5596 closed on re-measured evidence. Rulings D-A and D-C were
written onto seven notes, and eleven notes were narrowed off 1985 by `work.py set-editions`. `set-editions` named a second
judgment of a day so that it sorted first and was ignored. That is fixed: the second judgment is now `-editions02.md`, with a
self-test. `cite.py` now reads a row's extent from the units the catalog mints, so a quotation of a unit with no row is no
longer attributed to the row before it (PB6014). `Table <n>` is held to the printed caption, read by the one caption reader
the segmentation uses (PB6015).

**A numbered list item is citable (e, PB5449).** `item <n>` (`item L<m>.<n>`, with its sub-item path glued on) joins the one
designator grammar. It names an item that no numbered rule holds, checked against one reading of the items a clause prints.
An item the catalog holds as a rule must be cited by that rule, and `item <n>` there is refused with the rule's name. Every
1985 item citation now checks: 53 sites were re-pointed by hand, and the glossary's Text Word became `§1985:III.2 GRL4.3`.
PB5829's sequential lead was measured. g85_bf25_01 cannot fail on 1985-GR-VII.4.3.4-25's "must not subsequently be changed",
so the row gained a witness that can fail.

**The INSPECT examples (a, PB6001).** The 1985 catalog's last 16 prose holes all lay in §1985:VI.6.18.5, "Examples". Fourteen
were lines of fenced INSPECT code. The measure read them as prose, although the segmentation makes no unit of code. The
measure and the 1985 unit spans now read displays through one function, `text_units.display_mask`. A title pattern with no
record had kept the clause out of the units. It is now 19 units like any other text, disposed on reader and refuter records.
The label paragraphs are lead-ins, the result rows and the equivalence restate the INSPECT general rules, and the preamble is an
informative row. The two new example rows have self-checking witnesses.

**Own-edition witnesses (b, PB5740, PB5926).** A general format grants forms, so a refusal of a malformed form no longer
witnesses an FMT row. `lineage-evidence-kinds.json` now names SR as the only diagnosed kind. That turned the LINEAGE READS
audit red on the 12 1985 KEPT format rows that rested on refusals. Each now names a case accepted at 85. Four of the cases are
new and write every optional word and alternative of END PROGRAM, SIGN, SAME and ORGANIZATION IS RELATIVE. Under ruling D-B
the suite gained the WiseOwl option `listing=on`. 1989-RV-A.2.46.3-3 is now witnessed by a program that reads its own
listing's heading and finds WHEN-COMPILED's instant there. The derivation that rested on "no listing" is deleted. The row now
owes a code read, which is PB5683's.

**Input-output statements are an edition fact (d, PB6032).** The compiler's one classifier held 2023's eight statements and
was asked at every edition. `InputOutputStatementSet` (Editions) now holds one value per edition. 1985's value is the fifteen
statements of its definition (§1985:III.2 p139). For 2002, 2014 and 2023 it is the eight statements that set the I-O status
(§2002:9.1.12, §2014:9.1.12, §2023:9.1.13.1). `InputOutputStatementForms` is the one classification of a parsed statement
against that value. No outcome changes at any edition. On this branch, 1985-GR-III.2-p139 is recorded on its definitional
derivation, located at that set, which takes the 1985 GAP from 1 to 0. That closes PB6032 and PB5453, whose 657 minted rows
all stand on own verdicts.

**Integration.** The citation baseline was regenerated, never hand-merged. Group e's grammar reclassified 14 of train C's
sites as `item-is-a-rule`. Five were rewritten to the rule the tool names, and nine are history addenda or quote the 2023
run-time message, which is PB5449's 2023 half. Group e's item rewrite had edited five sealed 1985 code-read records, and
CODE READS turned red. They were restored to main's bytes. The gate's WITNESS LOSS audit refused group d's hand edit of
SR-13.4.6.3-4's code location, so that row now retires the deleted member through `record_verdicts.py`.

**The kill-by-name guard (PB6034).** This train carries main's hook unchanged; the hook rounds continue on branch
`hook-pb6034-r4`.

**The branch review, round 1.** A refuter record also stood for any disposition of its unit, so a `restates` list
edited after its refutation kept its old record and could hide a prose hole. Now a record upholds the disposition it judged: its kind, its class and its rule list. The 672
committed records that spelled only the kind gained that disposition from the reader record each one names, and the
import writes it from now on. Every entry already matched its record, so 1985 still has 0 prose holes.

**Code reads and two reader leads.** The five 1985 code re-reads (1985-c02 and 1985-c03, all CONFORMS and UPHELD) are
imported at the trees they read, so 1985 owes no code read. At `--std 85`, the refusal of a COMP-1 or COMP-2 item as an
integer argument cited ISO §2023:14.6.8.3. It now says the 1985 USAGE clause (§1985:VI.5.14.2) has no floating-point
usage, and it states what §1989:A.2.2 AR4 admits, an arithmetic expression that always results in an integer value.
`DISPLAY FUNCTION LENGTH (FUNCTION WHEN-COMPILED)` at 85 is refused correctly. The nested function-identifier is a legal
argument (§1985:IV.4.3.8.3.4 GR2) and compiles in `COMPUTE`. The refusal is of LENGTH itself, an integer function, as a
DISPLAY operand, which §1985:IV.4.3.8.3.3 SR4 allows only in an arithmetic expression.
