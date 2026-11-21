# PB5991, 2026-11-21: not a defect; the filing session's reproduction was wrong

The note was filed by the Mythos session from the prose campaign's refuter (slice 85p-08), with a "reproduction" of its own:
`E:/COBOL-coord/scratch/prose85/work/refute-85p-08/p/bsec.cob`, `S1 SECTION.` on line 4, exit 0 at `--std 85`. Line 4 begins the
header at column 11. §1985:IV.7.3.2's area A is columns 8 through 11, so column 11 is in area A and the program is legal; the
exit 0 is the right answer, not a defect. The session counted the columns wrong and did not check the column against the area's
bounds before filing. Wave 20262117 group D (report E:/COBOL-coord/scratch/reports/w20262117d-PB5991-report.md) found it: at
column 12 (area B) the compiler refuses `WORKING-STORAGE SECTION.` and `S1 SECTION.` with COBOL3734 (AreaPlacementPass, kb/Work
PB3147, PB5313), with negatives already in the suite. Retired with no change.
