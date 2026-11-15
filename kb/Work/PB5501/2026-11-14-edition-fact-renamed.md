## Addendum (wave 20262027 group B, 2026-11-14): the edition fact the lead names is renamed

The lead's "reuse EditionInfo.UnphrasedSizeErrorEndsOnlyTheStatement" is stale: the PB5484 branch review (R1-1) moved the fact into the
DialectBehavior registry as `DialectBehavior.UnphrasedSizeErrorIsFatal` (changed in 2002, VCR row 7.71), asked through
`DialectBehaviors.IsActive`. A fix for the condition path reuses that member, not an `EditionInfo` property.
