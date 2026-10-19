## PB3424 - a variable-length group coerced under --permissive to a function string argument or a numeric operand is the image DISPLAY shows, not a run-time abort

**What was wrong.** `MOVE FUNCTION UPPER-CASE (PG) TO X` over a group holding a DYNAMIC LENGTH item compiled under `--permissive` with the
COBOLNET1627 warning ("accepted ... with the existing coercion") and then aborted with `NotImplementedCobolFeatureException`, the Tier-C
byte island. The same group in a numeric position (`FUNCTION ABS (PG)`, `COMPUTE X = PG + 1`, `ADD PG TO X`, each a COBOLNET0844 or
COBOLNET1627 warning) died the same way. ISO 15.3 admits a variable-length group as a function argument only where the function says so
(LENGTH, BYTE-LENGTH) and 8.8.1.1 bars a group arithmetic operand, so strict refuses all of it; the defect is that a coercion the compiler
chooses to accept ended in a run-time abort.

**Re-probe.** The note's repro (`P6.cob`) reproduced on main fae388908 for the plain group and for the strong typedef holding an
object-reference leaf; the numeric position was found by the sibling sweep.

**Root cause.** One question, what are this group's characters, answered three ways. DISPLAY carried the variable-length arm inline
(`VarGroupCurrentImage`, gated on `CurrentExtentTransferImageCapable`); `OperandText.AsTransferString`, which the function-argument
visitor asks, only had the fixed pointer-leaf arm; `NumericRenderer.FieldNumCore` went through `SendingGroupValue`.

**Fix.** `PlaceRenderer.VarGroupOperandImage` is the one reader. `AsTransferString` asks it and DISPLAY now calls `AsTransferString`
alone, so DISPLAY and a function argument cannot differ. `FieldNumCore` asks it with the strict capability, so a variable-length group
holding a pointer or object leaf stays loud in a numeric position, as its fixed twin does. The image is the A.1 item 57 current-extent
image (fixed members, each dynamic member at its current extent, an OCCURS DEPENDING table at its current count, a dynamic-capacity table
at its current capacity); the DOC-A.1-57 row names the two coercions and their witnesses.

**Goldens.** `VariableLengthGroupArgumentPermissiveTests` (7 cases): strict refusal, the string lane over a fixed control group, a scalar
variable-length group, an ODO-plus-dynamic group, a dynamic-capacity group and a strong typedef with an object-reference leaf, and the
numeric lane. The conformance-suite manifest runs strict only, so the permissive run-time witness is a Conformance test class; the strict
negative is the standing `15.3-Arguments/pb124-vlg-function-argument`.

**Rows.** None: PB3424 claimed no inventory row, closes none.
