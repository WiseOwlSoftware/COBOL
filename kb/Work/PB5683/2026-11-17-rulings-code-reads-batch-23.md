# Rulings on the reads of slices 1985-250 and 1985-251 (batch 23), and a correction to batch 22

Written 2026-11-17 by the Mythos lane session (Claude Opus 5.5, main thread). Read pass 21: run wf_728e4fd8-03f of
`coderead-1985/coderead-sample.js` on a compiler built at main a05cc533dc. These are the nine KEPT rows whose rule
text changed when the printed folios XIII-30, XIII-31, XIII-54 and XIII-55 replaced the reconstruction (pull request
1849). Reader: 9 CONFORMS. Refuter: 8 UPHELD, 1 DOUBTFUL (`1985-SR-XIII.3.20.3-4`). Every citation below answered
`python scripts/spec/cite.py --check` OK in this session or in the refuter's run.

## The eight agreeing rows
`1985-GR-XIII.3.10.7-2`, `-3` and `-4`; `1985-GR-XIII.3.19.4-10`, `-11` and `-12`; `1985-SR-XIII.3.20.3-2` and
`-3`. They import as the tally gives them.

## 1985-SR-XIII.3.20.3-4: the row CONFORMS for what it says; the refuter's doubt is UPHELD as a defect of the FORMAT row
The refuter confirmed SR4's membership and count halves. A TYPE operand that is not a CONTROL operand is refused
(xrptctl.cob), and so is a different item of the same name (wrongitem.cob). SR4 CONFORMS.

The doubt: at `--std 85` a QUALIFIED TYPE operand compiles (qualneedless.cob: `TYPE CH WS-KEY OF WS-GRP` with
WS-KEY unique; the reader's chqual.cob: `CF WS-KEY IN WS-GRP`). The printed text settles it against the compiler:
- `§1985:III.2` defines data-name: in the general formats, "must not be reference-modified, subscripted, or qualified
  unless specifically permitted by the rules of the format".
- The CONTROL clause permits it (`§1985:XIII.3.7.3 SR1` "Data-name-1 may be qualified").
- The printed TYPE clause (folio XIII-55, `§1985:XIII.3.20.2`, `§1985:XIII.3.20.3`) has no such rule. SR4 says only
  that data-name-1 and data-name-2 "must be specified in the CONTROL clause of the corresponding report description
  entry".
- The later editions ADD the qualification of the TYPE operand. The implementer of pull request 1849 listed it among
  what a later edition added.

So at 1985 a qualified TYPE operand is not permitted, and the compiler under-rejects. kb/Work PB5573 admitted it by a
reading recorded while folio XIII-55 was missing (its `2026-11-14-landed.md`: "a reading, folio XIII-55 missing").
The printed page replaces that reading.

**Correction to batch 22** (`kb/Work/PB5683/2026-11-17-rulings-pb5789-folios.md`): I accepted the refuter's
re-citation of the FMT row's qualified-operand basis to `§1985:IV.4.3.8.1`. That basis is withdrawn. `§1985:IV.4.3.8.1`
requires a name to be unique or qualified where qualification is permitted; it does not permit qualification where
the format's definition of data-name forbids it.

**New defect kb/Work PB5910** (under_rejects, MINOR, area report-writer, 1985 only): at `--std 85` a qualified TYPE
CONTROL HEADING or CONTROL FOOTING operand is accepted where the printed TYPE format permits no qualification. The row
`1985-FMT-XIII.3.20.2` is PARTIAL until the fix lands. The fix owes:
- refusal of a qualified (and a subscripted or reference-modified) TYPE operand at 85, through the edition gate that
  holds the CONTROL operand's shape (`VersionConformancePass#GateCobol85ControlName`);
- the CONTROL clause's own qualified operand stays legal;
- each later edition judged by its own text, not changed unless its text says the same.
- Programs: E:/COBOL-coord/scratch/coderead-1985/out/refute-1985-251/ (qualneedless.cob) and the reader's chqual.cob
  under out/work-1985-251/.
- A dated file beside kb/Work PB5573 says its reading is replaced, and why.

## Leads that are not yet anything (to notes batch 22's sweep or the next)
- Slice 250: at `--std 85` the page-heading tail COBOL2199 message names PRESENT WHEN, a later-edition phrase.
- Slice 251: COBOL0899 is placed at the RD line, and one message shows the internal name 'PageHeading'.
- Both slices: messages cite §2023 at 85; this is the misleading-diagnostic lead of batch 22.
