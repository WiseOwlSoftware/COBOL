## Lead: a full compile's allocation varies by about 2 MB between runs of one tree (listing step 6)

`CompileBenchmarks.FullCompile`'s allocated bytes for the same no-listing code differed by 2.07 MB between the `listing-step-6`
branch and its parent `0d4042678a` on SM201A (35.76 against 37.83 MB; the front end and bind rows agree within 0.5 MB), and
the full-compile rows of one tree have moved by several MB between runs at every step since step 1 (the README's tables).
On SM201A's 36 MB that variation is as large as the standard listing's 10 % budget margin, so its `Text over None` row reads
+5.2 % against the parent's `None` and +11.2 % against the branch's own. The budget of DESIGN-listing.md section 9 is therefore
undecidable on a single run for the smallest program. Lead: measure the listing's cost where it is spent (the front end and
bind, and the listing writer), or take the median of several `[IterationCount]`-controlled launches for the full-compile
allocation, and find what in the backend allocates a varying amount for identical input (Roslyn's emit is the suspect).
Measurements: E:/COBOL-coord/scratch/w20262104g/bench-run1-report.md, bench-run2-report.md, bench-parent-report.md.