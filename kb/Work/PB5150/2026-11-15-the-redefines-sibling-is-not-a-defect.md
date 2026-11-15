**2026-11-15: the GR3 "REDEFINES entry with GLOBAL" sibling registered in `2026-11-14-adjudication-landing.md` is NOT a
defect.** The adjudication landing, the PB5150 report and its DEVLOG entry registered, as a sibling not fixed, that
`ScreenGlobalNameUniqueness` does not consult §2023:13.18.27.4 GR3 and treats a redefining 01 carrying GLOBAL as global
itself. The PB5150 review (`w20262039f-PB5150-report.md`, R1-1) refutes it. GR3 says: "it is only the subject of that
REDEFINES clause that possesses the global attribute" (`cite.py --check "§2023:13.18.27.4 GR3"` OK). The entry that
carries the GLOBAL clause and the REDEFINES clause is the REDEFINES subject (the redefining item); its target is not
made global. `IsGlobalRecord(item.Root)` already treats exactly that entry as global, which is what GR3 requires. No
note is needed for it; nothing opens from the landing's "Sibling note". The two real second-order effects of the widened
screen (same-record subordinates, the TYPE-copy report line) are kb/Work PB5629.
