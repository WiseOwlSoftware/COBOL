**2026-11-24, second sighting by the CONFLICT finisher of worktree-wf_c37a519f-ba4-15 (PB5707).** Gate run
`000007-20261124T144607Z` (merged head 3deff265d8: PB5707 merged with the queued PB5725 head, a binder change and a
preprocessor TURN change; no runtime or file I/O code) went RED on six HARNESS NON-OBSERVATIONs, each TimedOut twice at
120 s with 1.5-2.3 s of CPU on a 59-63 % busy host: NIST IC114A, SQ204A, SQ214A, SQ114A, OBSQ4A, OBSQ5A (and OBSQ3A in
one of two compilations). SQ205A, SQ224A and CM102M timed out once and passed on retry. All are sequential or indexed file
programs, the same family as the first sighting (gate 000005 of this branch, before the merge).

New datum: the hang does not reproduce under file-I/O concurrency alone. Four concurrent `dotnet test --filter` runs of the
same nine NIST cases on the same build (36 compiled programs, at most 4 alive at once) all passed, 8/8 each in 36-37 s, while
two other gates ran. A hang-watcher (`dotnet-stack report` on any program alive past 40 s) caught one slow program in
another gate (SQ217A of worktree-wf_38508f28-551-1 run 000002), which finished under its 120 s budget, so nothing was
captured. The blocking seems to need the full gate population (about 29,000 programs across 3 test hosts, alongside other
gates), not just file-I/O programs side by side. The re-gate of this branch ran with a 90 s watcher armed; see its report for
any stack it captured.
