## The FROM/INTO record-area rule ends at 2002, as the 2014 text says (PB5270)

Wave 20262004 group G (PB5183, PB5163, PB5209, PB5270). This branch continues w20261998c (PB5120 and its siblings, merged in
from worktree-wf_c902d12a-c75-3); the merge kept both sides of main's new DialectBehavior rows 8 to 13 and VCR rows 7.58 to 7.63,
so the INSPECT BEFORE/AFTER row PB5120 added is now DialectBehavior 14 and VCR row 7.64.

**PB5270.** Re-probed on the branch build: `WRITE FR FROM FR` and `READ F INTO FR` drew COBOL6827 at `--std 2014` as at 2002,
though `§2014:E.3` lists all five phrases (READ INTO, RELEASE FROM, RETURN INTO, REWRITE FROM, WRITE FROM; items 21, 24, 25, 26
and 38) as no longer prohibited. The rule had been kept at 2014 by a DETERMINATION made before the 2014 text was in the
repository. `RecordAreaRule.InForce` is now `editionYear < 2014`, and the COBOL6827 descriptor names the 2014 change. The five
`pb5077-*-own-record-area` refusals refuse at 85 and 2002; the new golden `pb5270_from_into_own_record_area_2014` runs all five
phrases from 2014 and prints only the defined results (I-O status, the record count the SORT returned). Docs: CONFORMANCE §3.5,
FILES_DESIGN D31 (b), VCR row 7.67. No inventory row closes: there is no 2014 inventory, and the 2023 rows never carried the rule.

PB5183, PB5163 and PB5209 were re-probed and still reproduce; the fleet stop arrived before their fixes (see the branch's
STATUS.md for the next step).
