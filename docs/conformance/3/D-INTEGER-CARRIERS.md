- **D-INTEGER-CARRIERS — Integer operands and host carriers (§2023:4.5 / §2023:4.6 — the limits of an implementation; kb/Work PB1033, PB1058)**:
  an integer data item or literal holds up to 31 digits (§2023:13.18.40.3 SR14, §2023:8.3.3.3.2), and every rule that
  consumes one asks about its VALUE. Two carrier rules follow, each written in ONE place:
  **(1) an integer-n the compiler binds into its own model** — a table size, a record or block size, a LINAGE or
  PAGE line count, a report LINE or COLUMN, an ordinal — is limited to **2,147,483,647**; a larger literal is
  `COBOL2427` (§2023:4.5: "Translation may be unsuccessful due to factors other than lack of conformance", whose
  NOTE names "the limits of an implementation"), screened once for every `integerLiteral` by
  `Validation/IntegerOperandPass` and read by the binder only through `IntegerOperandRules.HostValue`. The same
  limit meets an integer the binder reaches by another road — an integer constant-name substituted for integer-n
  (§2023:13.10.4 GR1: OCCURS K where K is 77777777777) and a reference-modification literal of a CONTROL, TYPE CF or
  SUM … RESET ON operand (§2023:13.18.16.3 SR4; the prior control is laid out as the slice, §2023:13.18.16.4 GR3) — through
  the one text reader `IntegerOperandRules.TryHostValue`, which reports "integer literal, saturated" rather than
  "not an integer literal", and the one sentence `BeyondLimitMessage` (kb/Work PB1579). Exempt,
  because their value is carried at full width: PERFORM integer-1 TIMES, WRITE ADVANCING integer-1 LINES, and
  the DYNAMIC LENGTH LIMIT (clamped by A.1 item 62's own determination). **(2) a run-time integer value handed to a host
  `int` or `long`** — a GO TO … DEPENDING selector, a reference-modifier position or length, a PERFORM TIMES
  count, a RETRY amount, a STRING / UNSTRING pointer, a RELATIVE KEY, a boolean shift
  count, an OCCURS DEPENDING current count, an ADVANCING or LINAGE line count, a termination status — is
  **saturated**, never wrapped (`CobolNum.Position` / `Position32`, emitted only through `RuntimeApi.HostInt64` /
  `HostInt32`; a CONTINUE AFTER period is saturated inside `CobolTiming.ContinueAfter`, which reads the interval on its own carrier through `CobolNum.PositionOf`, kb/Work PB1529). A wrap maps an out-of-range value back into range; saturation keeps every rule's out-of-range
  answer, because every range those rules test lies strictly inside the carrier: `GO TO … DEPENDING ON` a
  selector of 4,294,967,297 falls through (§2023:14.9.17.4 GR2), a reference-modifier start of 4,294,967,297 raises
  EC-BOUND-REF-MOD (§2023:8.4.3.3.4 GR5 c), a STRING pointer of 2^64 + 3 overflows and stays unchanged (§2023:14.9.43.4 GR6,
  GR8). A subscript's or OCCURS DEPENDING count's saturated `long` still positions every table, and the diagnostic
  names the value the program holds, not the clamp (`CobolTable.Occ` returns an `Occurrence`, kb/Work PB2695); a reference-modifier
  leftmost position or length is the same carrier (`CobolString.RefModPosition`, kb/Work PB2938), and an integer literal past `long`
  keeps its value in the diagnostic too. A repeat count saturates at 2^63 − 1, which no terminating run can tell from a larger one; the
  observable carrier limits are an **ADVANCING line count** above 2,147,483,647 (≈ 4 GiB of line terminators),
  which advances 2,147,483,647 lines, and the termination status (item 192 above). An UNSTRING TALLYING count is
  summed exactly, never narrowed. `semgrep` rules `wiseowl-cobol-integer-value-cast-narrowed` and
  `wiseowl-cobol-integer-n-parsed-outside-host-value` hold both rules to their one place.
