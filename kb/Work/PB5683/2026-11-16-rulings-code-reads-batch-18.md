# Rulings on the code reads of slice 1985-244 (batch 18)

Written 2026-11-16 by the Mythos lane session (Claude Opus 5.5, main thread). Read pass: run wf_5dc304f1-a2e of
`coderead-1985/coderead-sample.js` at pin `1554cc5debb003cdbdf62be59cb1efb910488540`. Records:
`E:/COBOL-coord/scratch/coderead-1985/out/1985-244.json` (reader) and `1985-244-refute.json` (refuter).
`python -I tally.py 244 244`: 5 rows; 4 CONFORMS with the refuter UPHELD; 1 CONFORMS with the refuter DOUBTFUL
(`1985-SR-XIII.3.7.3-2`). The four agreeing rows import as the tally gives them; this file rules the fifth.

## 1985-SR-XIII.3.7.3-2: the doubt is REJECTED; the row CONFORMS on the reading kb/Work PB5866 recorded

The refuter's doubt: `§1985:XIII.3.7.3 SR2` ("Each recurrence of data-name-1 must identify a different data item")
is applied so that a level-66 entry without THROUGH identifies the item it renames, and the refuter argues the 1985
text does not say that (`§1985:IV.4.3.2.1` counts a level-66 entry among the entries that specify items;
`§1985:VI.5.11.1` calls RENAMES "alternative, possibly overlapping, groupings"), so `CONTROLS ARE A R1`, where
`66 R1 RENAMES A`, might be legal source the compiler refuses.

Ruled: that question was ruled from the text in kb/Work PB5866 (landed; my rulings batch 14, mechanism 2), and the
refuter brings no sentence that ruling did not weigh. `§1985:VI.5.11.4 GR2`: "When data-name-3 is not specified, all
of the data attributes of data-name-2 become the data attributes for data-name-1". Such an entry has no description
of its own: its storage, its class, its category, its size and so its value are those of data-name-2 at every
moment. A control break on it is a break on the same value, which is what SR2 forbids. That is not true of the
refuter's two controls: an item that REDEFINES another has a description of its own (its value under that
description can differ), and a level-66 entry WITH THROUGH is by `§1985:VI.5.11.4 GR1` "a group item which includes
all elementary items" from data-name-2 to data-name-3, a group of its own beside any other group over the same
elementary items. `§1985:IV.4.3.2.1` and `§1985:VI.5.11.1` describe what a level-66 entry may introduce (a
regrouping, which needs THROUGH); neither says an entry without THROUGH is a second data item. The line the compiler
draws (`ReportSectionBinder.cs#ScreenControlItemsDistinct`, `DataItem.AttributeItem`) is the line GR1 and GR2 draw.

The precedence was tried and settles nothing: GnuCOBOL 3.2.0 (`-std=cobol85`, this session,
`E:/COBOL-coord/scratch/my-20261115/b244-check/`) fails in its own generated C for both `CONTROLS ARE A R1` and
`CONTROLS ARE A A` (`ren.cob`: "'f_6' undeclared"; `same.cob`: "redefinition of 'rc_1_4'"): it has no check of the
rule, as PB5866 found, and its run is a defect of its own, not an answer.

Verdict: CONFORMS at 1985. The row's notes name kb/Work PB5866 for the reading. No determination entry in
`docs/conformance/` is owed: this is a reading of the text, not an implementor's choice.

Citations to run through `python scripts/spec/cite.py --check` by whoever tracks this file:
`§1985:XIII.3.7.3 SR2` "Each recurrence of data-name-1 must identify a different data item";
`§1985:VI.5.11.4 GR2` "all of the data attributes of data-name-2 become the data attributes for data-name-1";
`§1985:VI.5.11.4 GR1` "data-name-1 is a group item which includes all elementary items" (the refuter's run checked
SR2, GR2, VI.5.11.1 and IV.4.3.2.1 OK).

## Leads that are not yet anything
- The reader: some refusals at `--std 85` in this slice cite `§2023` in their message (a diagnostic naming the wrong
  edition's rule; it does not change what is refused). Nobody has listed which.
