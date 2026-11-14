---
order: 20
---
> ⚖ **DETERMINATION — a RETURNING result whose description does not conform, with EC-PROGRAM-ARG-MISMATCH checking
> off in either element** (2026-10-02; kb/Work PB1040). §2023:14.8.3.3 (cite-checked) gives a conforming RETURNING receiver
> "the same ALIGN, BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses" as the sender, and
> §2023:14.9.4.4 GR3 d) makes a violation EC-PROGRAM-ARG-MISMATCH "if checking for it is enabled in both the activated
> program and activating runtime element", the call "not successful" (the callee never runs; GR3 h) 1. sends it to
> ON EXCEPTION). Checked in both, WiseOwl COBOL raises it at call initiation: at a CALL whose activated program the
> activating element holds no signature for, over both returning items' whole descriptions (§2023:14.8.3 entire,
> `BoundaryItem.ReturningViolation`; kb/Work PB165), elsewhere over the numeric item's PICTURE, SIGN and USAGE and a
> text-carried item's character length. The standard names no outcome when checking is off in either element, and WiseOwl COBOL
> follows the posture it already documents for the unchecked argument COUNT (`ProgramTable.CallProgram`: a missing
> argument behaves as omitted, an excess one is ignored): the call proceeds and the result is stored in the
> RECEIVER'S OWN width — a text receiver by the alphanumeric alignment of §2023:14.6.8.5 ("aligned at the leftmost
> character position in the data item with space fill or truncation to the right"), a numeric receiver as the value
> the sender held under the receiver's own PICTURE and USAGE — so an item's image never changes width (before this
> determination a 3-character result left a PIC X(5) item displaying `[ABC]` with FUNCTION LENGTH 5). This is
> implementor latitude settled by the project's own existing posture for the same rule, not by a survey of other
> compilers. Pinned by `2002/pb1040_returning_length_conformance`. **The same gate and the same unchecked posture govern an
> ARGUMENT's description on the dynamic Format-1 lane** (kb/Work PB165): checked in both elements, a formal of a different
> length than its argument (§2023:14.8.2.3.2 / §2023:14.8.2.3.3 rule 1), or an alphanumeric group paired by reference with a
> non-alphanumeric item or a longer formal (§2023:14.8.2.2 1)), is EC-PROGRAM-ARG-MISMATCH at call initiation; unchecked, the call
> proceeds and the formal's window fits the argument as it always did. Pinned by `2002/pb165_dynamic_call_description_check`.
