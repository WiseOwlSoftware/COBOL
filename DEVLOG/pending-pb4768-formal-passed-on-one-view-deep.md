## PB4768 - a formal passed on BY REFERENCE is one view deep, not one more per activation

**Mechanism.** `CobolArgAdapt.Text` (and `DynText`, the ANY LENGTH arm and the native-cell image arm) returned a new
`OverField` over the previous activation's carrier. A formal passed on BY REFERENCE through d activations was a chain of
d closures: d delegate calls per access, and at about 21,400 levels a .NET stack overflow after the
EC-PROGRAM-RESOURCES declarative had run, where §14.9.4.4 GR3 c) requires execution to continue.

**Re-probe.** The note's repro (a recursive `DP` passing `L-A` on, displaying and storing it at the level whose CALL
failed) with `PIC X(4)`, `PIC 9(4)`, `PIC 9(4) COMP` and `PIC S9(4) COMP-3` runs to the end on the fixed build:
`DECL EC-PROGRAM-RESOURCES`, `AT FAIL L-A=ABCD`, `BACK X=WXYZ`.

**Fix.** One builder, `Control/TextViews.cs` (`Window`, `AnyLength`, `Dynamic`). Each view records what it is
(`ManagedPointer<T>.ViewTag`), and the next adaptation builds over the UNDERLYING carrier: a window of width w over a window
(Trunc a, Width w1) is one window over the base with Trunc = min(a, w1) (read `Store(Trunc(B,.), w)`, store splices
`min(w, a, w1, |B|)` characters); an ANY LENGTH view over a window or an ANY LENGTH view is that carrier; dynamic limits
compose by their minimum. The image arm of a native cell is now a window over the cell's image carrier. Not collapsed:
the variable-length-group `OverStorage`/`OverField` arms (a shape conversion per hop; reachable only with differently
shaped groups) and the numeric-cell converting arms (they alias when scale and carrier agree, the recursive case).

**Goldens.** `14.9.4-CALL/call_by_reference_formal_passed_on` (2002+, three chains: X(4), 9(4), 9(4) COMP) and the
refusal `call-by-reference-formal-passed-on-at-85` (COBOL0900, the `>>TURN` directive). Unit:
`CallAbiViewDepthDriftTests` (100,000 levels stay one window; the composed window equals the chain it replaces for six
width/storage shapes). Rows closed: none (the note claimed none).
