# PB5362: the 'A' / "A" witness is not a COBOL-85 program (merge with PB5160)

Found by the CONFLICT finisher of `worktree-wf_6fe280d5-b8c-15` (PB5213, PB5160), at the gate of the merge of main
(eda73715). The merged tree was RED on one case: `pb5362-external-value-of-same-value` (accept `[85]`) wrote
`VALUE OF FILE-ID 'A'` in one entry and `"A"` in the other, and PB5160 makes the apostrophe-delimited literal COBOL0900 at
`--std 85` (§1985:IV.4.2.2.2.1, "at the beginning and at the end by the separator quotation mark"; §2002:F.2 item 3).
VALUE OF exists only at COBOL-85 (VCR row 7.5, refused from 2002), and `X"41"` is a 2002 format too (PB1545), so a COBOL-85
alphanumeric literal-1 has one spelling and the delimiter difference cannot occur in a conforming program.

**Change.** The case writes `"A"` in both entries; RETENTION (`7`, `007.0`) and OWNER (`SPACE`, `SPACES`) still differ in
spelling and agree in value, which is what exercises `ValueOfLiteralValue`. The key stays delimiter-blind (it states the
rule's value comparison); its summary now says where two COBOL-85 spellings agree. The row 1985-GR-VII.3.9.4-2's notes were
restated through `record_verdicts.py` to match.
