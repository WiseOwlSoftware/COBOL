---
order: 19
---
> ⚖ **DETERMINATION — an unchecked out-of-range reference modification ends the run unit** (2026-09-30; owner
> decision kb/Work R60; kb/Work PB1707). §2023:8.4.3.3.4 5) b)/c) (cite-checked) make the position ranges a `shall`
> — leftmost-position a position of the item, and "The sum of leftmost-position and length minus the value one shall
> be less than or equal to the number of positions in the data item referenced by identifier-1" — and a violation is
> the fatal EC-BOUND-REF-MOD (Table 13). With checking enabled the condition is raised (USE declarative, else the
> run unit ends, §2023:14.6.13.1.3 5)/7)). With checking NOT enabled the standard names no outcome for the violated
> `shall`, so the owner's standing rule of 2026-07-28 decides — lenient wherever the standard names the outcome,
> LOUD ABORT wherever it names none — and WiseOwl COBOL **terminates the run unit with a runtime error**
> ("abnormal run-unit termination: EC-BOUND-REF-MOD (fatal): reference modification (1:10) out of range for a
> 5-position item", exit 1): it never clamps the slice or pads it with spaces, because a reference modification that
> addresses positions no item owns has no result to return (GnuCOBOL, IBM and Micro Focus addressed the storage that
> follows the item, for which there is no memory-safe behaviour to follow; before this determination the read
> padded with spaces and the write clamped, so `DISPLAY CX(1:77777777777)` ended with "Out of memory."). The test is
> ONE predicate for the read and the write (`CobolString.RefModOutOfRange`, through `CobolString.RefMod` and
> `CobolString.SpliceInto`), it also covers a non-integer position (§2023:8.4.3.3.4 5) c), which is the same
> violation), and a LITERAL violation against a fixed-size item is refused at compile time before it can run (A.1 item 69).
> Slices the COMPILER chooses — a REDEFINES view, an occurs-depending group's current extent, an INVOKE/CALL
> boundary prefix, the current record of a READ … INTO — are not reference modifications the program wrote, go through
> `CobolString.Window` / `WindowInto` (`RuntimeApi.StrWindow` / `StrWindowInto`), and keep their lenient
> space-extending semantics; `RuntimeApiWindowDriftTests` keeps the two lists apart. Pinned by
> `RefModUncheckedTerminationTests`, `negative:pb1707-refmod-literal-out-of-range`, and the checked arm by
> `2023/l1_fatal_ec_runtime_detection`.
