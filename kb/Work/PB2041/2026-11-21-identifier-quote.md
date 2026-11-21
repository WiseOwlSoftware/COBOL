## 2026-11-21 - the same screen's 1985 sentence quotes the Identifier definition Amendment 1 replaced (wave 20262131 group o)

Where: DataBinder.cs lines 1187-1189 (ScreenDataNameShape's subscripted arm, the note's own screen), the
COBOL2024 message's 1985 text: `"a syntactically correct combination of a data-name, with its qualifiers, subscripts,
and reference modifiers" (ISO §1985:III.2, Identifier)`).

**Repro (branch train85b-reads, 2026-11-21):** `run_probe.py --std 85 E:/COBOL-coord/scratch/w20262131o/probes/g-02/dn-sub.cob`
is refused with COBOL2024 at line 10 (correct), quoting that text. `python scripts/spec/cite.py --check "§1985:III.2 p124"
"a syntactically correct combination of a data-name"` gives `FAIL [text-not-in-rule]`: the quoted words are not p124's
(the Identifier definition as Amendment 1, Errata C75, prints it), and "§1985:III.2, Identifier" names no paragraph in
the edition's citation form.

**Owed:** the 1985 arm quotes the amended p124 text and cites `§1985:III.2 p124` (cite.py --check on the quotation), with
the goldens that match the message updated from the text. One screen, one message: fix it with this note's own change.
(Reader lead of the 85w g-02 slice, row 1985-GR-III.2-p124.)
