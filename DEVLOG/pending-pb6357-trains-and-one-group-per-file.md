## PB6357: fixes land in trains, one group per source file

Owner 2026-11-28: bundle about a dozen fixes into one build/test/verify pass, and make every known fix in one source file at once. kb/Work PB6357 records the decision and the five tooling sites that will enforce it (dispatch template, wave planner, enqueue, review lane, landing check). The evidence is the night of 2026-11-27: about 16 single-fix branches, each with its own whole-population gate, two of them duplicates of the loop's own wave.
