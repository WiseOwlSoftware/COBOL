## 2026-11-15 — the 1985 rows the conversion reopened, for the 1985 witness lane

The free-form cases 1985 rows named (measured on main 30b21e7d87: 134 refusal tests of `cases/` over 182 references, 4
accepted tests of `wiseowl/` over 15) went through `conformance-suite/harness/reformat.py`. It converted 137 and refused
one, so one reference left its row (batch `pb5754-retire-unconvertible-1985`):

| row | its reference that left | why the converter refused it | after `witness-batch` and `widen` |
|---|---|---|---|
| `1985-SR-VI.5.7.3-1` (KEPT into `SR-13.18.33.3-1`) | `conformance:cases/13.18.33-Level-number/pb485-level-number-missing` | line 24 opens a data entry with no level-number (`W PIC X(3).`, the case's subject), so whether it begins in area A or area B cannot be decided | still GAP: the target's only suite case is that free-form one (`widen` reason `reference-format`), so the row needs a 1985 witness of its own |

The row's witness is a 1985 program, written in fixed form, whose data description entry lacks its level-number
(§1985:IV.7.4 places a level-number 01 or 77 in area A, any other right of margin A), refused at 85 as
`pb485-level-number-missing` is refused at every edition (COBOL0001). No `wiseowl/` case was left
free form: none of the four is about free form or the `--source-format` option.
