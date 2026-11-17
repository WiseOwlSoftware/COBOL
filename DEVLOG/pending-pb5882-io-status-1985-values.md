## PB5882: at COBOL-85 the FILE STATUS item receives only the 1985 values and their 9x

**The defect.** The runtime reported the 2023 I-O status vocabulary at every edition, so a `--std 85` WRITE of a record
holding the line delimiter, on a file framed by the implementor-name LINE-SEQUENTIAL (kb/Work PB5844), stored '71', a value
the 1985 text does not list. The re-probe on this branch (`rdely.cob`, `run_probe.py`) printed `LFW 71`.

**The rule.** §1985:VII.1.3.5, §1985:VIII.1.3.4 and §1985:IX.1.3.4 each list "the values placed in the I-O status for the
previously named conditions" for their module; a condition the implementor adds is 9x, which "must not duplicate any
condition specified by the I-O status values 00 through 49" (§1985:C.2 item 48 gives x to the implementor).

**The fix** (docs/FILES_DESIGN.md D33). One projection at the one place the program reads its status: the connector keeps
its condition (`FileConnector.Status`), and `FileConnector.ProgramStatus` reports, at 85, `IoStatus1985.Report` (a
`DialectBehavior`, VCR row 7.86). A condition 1985 names under another value takes it ('06' → '04' by §1985:VII.4.4.4 GR13,
'09' → '00', '31' → '30'); a condition no module names has its own 9x ('45' 92, '51' to '54' 93 to 96, '61' 97, '62' 98,
'71' 99), documented in the conformance statement's new section 3.13. Every projection keeps its value's class and
criticality, so the phrases steered by the runtime's returned statuses and the hooks steered by the program's status
agree. After: `LFW 99`.

**Tests.** `IoStatus1985Tests` (the module lists re-read from the 1985 text; every `FileStatusCode` constant on every
organization), and the 1985 case `pb5882_io_status_1985_values`, whose expected lines are derived from the rules above
(at 2023 the same program prints 09, 06 and 71, so the case discriminates). The 1985 store records the case as witness of
`1985-GR-VII.1.3.5-L2.5`, `-5` and `1985-GR-VII.4.4.4-13`, and the code site of `1985-GR-IX.1.3.4-L2.6`.

**Not done here, registered in the report.** At 85 the line-delimited READ still continues an over-length line's remainder
and gives '00' for a short line, where §1985:VII.4.4.4 GR13 truncates and reports the length conflict; the OCCURS DEPENDING
ON READ's '34' is a 1985 value of another condition; 2002 (frozen) still reports '06', '09' and '71'.
