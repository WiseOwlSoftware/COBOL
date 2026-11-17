**2026-11-17: the printed folio XIII-55 replaces the reading on the qualified TYPE operand (kb/Work PB5910).**
`2026-11-14-landed.md` decided "a QUALIFIED TYPE operand ... stays COBOL-85" as "a reading, folio XIII-55 missing", and
said that if the printed page said otherwise the change would be one predicate in `GateCobol85ControlName`. The printed
page is now in the transcription (kb/Work PB5789, pull request 1849), and it says otherwise.

Why the reading falls:
- The reading rested on a rule the reconstruction carried back from 2002, that each operand "shall be the same as one
  of the operands of the CONTROL clause" (`§2002:13.16.55.2 SR10`). The printed 1985 rule is different:
  `§1985:XIII.3.20.3 SR4` says only that data-name-1 and data-name-2 "must be specified in the CONTROL clause of the
  corresponding report description entry". It names membership, not the spelling written in the CONTROL clause.
- `§1985:III.2` forbids a qualified data-name in a general format "unless specifically permitted by the rules of the
  format". The CONTROL clause has that permission (`§1985:XIII.3.7.3 SR1`, "Data-name-1 may be qualified"); the
  printed TYPE clause, `§1985:XIII.3.20.2` and `§1985:XIII.3.20.3`, has none. The permission to qualify the TYPE
  operand first appears in 2002, in the same rule that adds reference modification (`§2002:13.16.55.2 SR10`, "Data-name-1
  and data-name-2 may be qualified and reference modified").
- The reading's practical argument, that a control name needing qualification would have no heading or footing, does
  not override the text. A control data item whose name is not unique has a control heading or footing at 1985 only
  if its name is unique (the CONTROL clause qualifies it, the TYPE clause cannot); that is the edition's limit, and
  2002 lifted it.

So at `--std 85` a qualified TYPE CONTROL HEADING / CONTROL FOOTING operand is refused (kb/Work PB5910), and the
CONTROL clause's qualified operand stays legal. The rulings: `kb/Work/PB5683/2026-11-17-rulings-code-reads-batch-23.md`.
