## Review finisher (wave 20262027 group B, 2026-11-14)

Branch review R1-1 (blocking): the 85 disposition is no `EditionInfo` property. It is `DialectBehavior.UnphrasedSizeErrorIsFatal` (changed in 2002,
VCR row 7.71, `tests/doc-facts/compiler-facts.json` row, `PublicAPI.Unshipped.txt`), asked by `ArithmeticEmitter.EmitArith` through
`DialectBehaviors.IsActive`; `EditionInfo.UnphrasedSizeErrorEndsOnlyTheStatement` is deleted and every caller changed.

**Sites:** `src/WiseOwl.Cobol.Runtime/Control/DialectBehavior.cs` (`UnphrasedSizeErrorIsFatal`), `docs/VERSION_CHANGE_REFERENCE.md` (row 7.71).
