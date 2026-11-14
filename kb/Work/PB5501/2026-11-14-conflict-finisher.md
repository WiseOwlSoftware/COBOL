## Addendum (conflict finisher, 2026-11-14): the fact this fix reads is `DialectBehavior.UnphrasedSizeErrorIsFatal`

Bringing origin/main in conflicted in `EditionInfo.cs`, `PublicAPI.Unshipped.txt` and `docs/CONFORMANCE.md` DOC-A.1-70: PB5484's
review (R1-1) had moved the 85-vs-2002 fact out of `EditionInfo.UnphrasedSizeErrorEndsOnlyTheStatement` into the
`DialectBehavior` registry (VCR row 7.71). The merge takes main's deletion and re-points every consumer of this branch at it:
`ConditionRenderer.SizeErrorGuarded` wraps the leaf when `DialectBehaviors.IsActive(DialectBehavior.UnphrasedSizeErrorIsFatal, …)`
is false, and the `CobolSizeError.FalseWhenRaised` doc and DOC-A.1-70 name that member. The landed addendum's `EditionInfo` member
names are the pre-merge state. The PublicAPI baseline keeps both sides' symbols (`FalseWhenRaised`, `RecordLengthConflicts`).
