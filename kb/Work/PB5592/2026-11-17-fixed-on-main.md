**2026-11-17: fixed on main; closed by the PB4950 branch (w20262102c).** The Mythos lane's 1985 triage ran the note's two
programs on main (`E:/COBOL-coord/scratch/my-20261115/triage85/triage-rows.json`, row PB5592, FIXED-ON-MAIN), and this branch
ran them again on its own build (`python scripts/orchestrator/run_probe.py --std 85 --tree .`, sources
`E:/COBOL-coord/scratch/my-20261115/triage85/g4/b7a.cob` and `b7b.cob`; run directories
`E:/COBOL-coord/scratch/my-20261115/w20262102/c/probe/b7a-001` and `b7b-001`):

- `b7a.cob` (USE FOR DEBUGGING ON W-R, 66 W-R RENAMES W-Y): `EXIT: 0`, stdout `D1 W-R`, once.
- `b7b.cob` (ON W-R W-Y): `EXIT: 0`, stdout `D1 W-Y` then `D1 W-R`.

Both equal the note's Expected, derived from §1985:XV.3.2.4 GR6c and GR13; the note observed no output and `D1 W-Y` twice
on 689413b7de. No change of this branch touches the debug trigger, so the fix is main's.
