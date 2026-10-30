## PB3953 review fix: a wide function temporary is ordered against a non-wide one by the exact values, never by its 34-digit image

**Why a second PB3953 entry.** Entry 2754 records PB3953's round-1 head `5d4e0ac1d`, which reached main inside PR #787: PB1369's
same-file successor branch had merged that head, and the stamp folded PB3953's pending entry into PB1369's. The branch review had
held round 1 at FINDINGS (R1-1, BLOCKING), so main carried the defect below from #787 until this landing. 2754 keeps its number
and its text as the record of what #787 landed (kb/Work PB3123); this entry records the review fix, which lands on PB3953's own
branch after it. The coordinator's note PB4077 records how a later CLEAN released the successor.

**R1-1, the defect.** `FunctionValueField.Compare` ordered a wide temporary against a non-wide one by the wide value's 34-digit
image whenever the images differed, on the claim that the round-to-odd lowering is monotone. It is, but only against another
image: a non-wide temporary holds up to 39 exact digits (an Int128 value), off that grid. With
`A = X * Y + C = 170141183460469231731687303715884105000`, just under `Int128.MaxValue`, and `A + 1000` just past it, the wide
value's image (`…8841` then zeros) lies below A, so main showed `MAX (A, A + 1000)` = A, `MIN` = A + 1000 and
`MEDIAN (A + 1000, A, A + 2000)` = A + 2000, while ORD-MAX (on the exact `RenderWide` arm) answered 2 (§15.59.4 1), §15.63.4 1),
§15.61.4 1); §8.8.4.2.4).

**The fix.** `Compare` orders any pair holding a wide value by the exact values (`CobolWide.Compare`); the one temporary with no
exact wide form, an SDIDI value past the 256-bit range, is larger than every wide value, so its sign decides. Witnesses: golden
`pb3953_function_argument_int128_window` legs 8-11 and
`CobolWideIntrinsicsTests.AWideTemporary_OrdersExactlyAgainstANarrowOneOffTheImageGrid` (integer, fractional and past-256-bit
pairs).

**R1-2 (lead, fixed in the files changed).** Entry 2754 called MEAN and the binary64 family "left on the image, by
determination". They are open wrong answers: MEAN rounds twice (the image, then the SDIDI quotient) and SIN of a 39-digit
product reduces the wrong angle. They are filed as PB4075. The `RawArg` doc comment and kb/Work PB3953 now say so.

**Register.** PB3953 stays `landed` and its body now records the two landings; PB4075's site paragraph no longer says the fix
waits to land. No inventory row moves (`closes_rows: []`, as in 2754).
