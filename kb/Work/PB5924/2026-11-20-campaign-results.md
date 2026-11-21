# PB5924 addendum, 2026-11-20: every uncovered 1985 requirement probed at `--std 85`

Owner 2026-11-20: "We should create a test case or golden or whatever appropriate for each new requirement. Compile it using
1985 and determine the current status." Workflow wf_10563339-8c6: 14 slices of the measured holes (Appendix C excluded), an
Opus reader and a Sonnet refuter each, compiler at main 8244f1aa4b; records E:/COBOL-coord/scratch/prose85/out/, drafted goldens
E:/COBOL-coord/scratch/prose85/work/<slice>/goldens/ (271 files, `rules: [TBD-1985-PROSE <locator> s<n>]` until the PR rows exist).

198 sentences: RESTATES an existing row 45 (4 overturned by the refuter to TESTED/CONFORMS, each owed its own golden),
NOT-NORMATIVE 30, TESTED 103 (95 CONFORMS, 8 DEFECT), NOT-OBSERVABLE 20 (19 CONFORMS, 1 DEFECT). Refutations: 176 UPHELD,
6 OVERTURNED, 15 DOUBTFUL. The defects, by mechanism: PB5986 (obsolete elements not flagged or documented), PB5987
(misplaced DECLARATIVES message), PB5988 (PICTURE precedence citation), PB5989 (operator spacing rules), PB5990 (debugging lines
unchecked without the mode), PB5991 (section header in area B, the refuter's overturn of IV.7.1 s3, reproduced). Leads: PB5992.
Seven sentences are UNDECIDED until the 1985 conformance statement exists (I.1.5.2 s1-s3, I.1.5.2.2 s1, I.1.5.2.5.1 s1, IV.4.1 s2,
XV.1.3.4 s2): PB4997/PB4885's work.
