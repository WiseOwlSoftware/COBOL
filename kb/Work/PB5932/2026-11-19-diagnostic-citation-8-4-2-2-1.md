## Lead (data-name lane, 2026-11-19): a diagnostic cites `§2023:8.4.2.2.1 rule 1`, a designator cite.py refuses

`src/WiseOwl.Cobol.Compiler/Binding/DataBinder.Configuration.cs` line 131 ends a COBOL1639 message (a reference that names both a
data item and a switch-status condition-name, kb/Work PB3347) with `(ISO §2023:8.4.2.2.3 SR1; §2023:8.4.2.2.1 rule 1)`.
`python scripts/spec/cite.py --check "§2023:8.4.2.2.1 rule 1" "No other name has the identical spelling"` prints
`FAIL: 'rule 1' is not a rule designator`; the checked form is `§2023:8.4.2.2.1 SR1` (OK, "the text is in SR1").

The data-name lane swept the 43 comment and document sites of the form to `SRN`; this one is the compiler's OUTPUT, so the change is
not behaviour-neutral (the oracle and any golden that pins the message move with it), and a restructuring branch does not carry it.
Owed: the message cites `§2023:8.4.2.2.1 SR1`, and every golden of the message is re-recorded from the standard's text in the same
change.
