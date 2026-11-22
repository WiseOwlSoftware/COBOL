## Discharged (2026-11-22, wave 20262135 group A)

Re-read on 2061bdb7bb: the second validity test no longer exists. `CobolEdit.DeEditFloat`
(`src/WiseOwl.Cobol.Runtime/Values/Numeric/CobolEdit.Float.cs:310`) reads the significand digits, the sign and the exponent,
returns zero for an all-spaces image under BLANK WHEN ZERO, and under EC-DATA-INCOMPATIBLE checking re-renders the value
through `FormatFloatCore(NormalizeFloat(…), m, blankWhenZero, out _)` and compares the image: the round trip DeEdit runs.
The per-position checker (the 'B' arm, the Table 8 sign arms and the exponent form) is gone, and the emitter
(`NumericRenderer.cs:569`) passes `blankWhenZero: true` for a BLANK WHEN ZERO sender. The landing that did the whole
target is 56312e1996 "PB2617+PB2643+PB2616: exact scaled powers; BWZ float de-edit; wide subs", the defect (#4) this
note said the unify should land with. Nothing is left to do, so the note is retired with no change of its own.
