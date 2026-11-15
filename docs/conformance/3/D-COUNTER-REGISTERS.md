- **D-COUNTER-REGISTERS — The counter registers' declared description — LINAGE-COUNTER, PAGE-COUNTER, LINE-COUNTER** (§2023:8.4.3.14.4 GR1 /
  §2023:8.4.3.15.4 GR1; NOT an Annex A.1 item — the registers' size and usage are simply unspecified, so this is a pinned
  implementor choice, kb/Work R26, PB1153, PB1746): all three registers are **unsigned integers** per their GRs, so
  `LOWEST-ALGEBRAIC` of any of them is **0** and `SMALLEST-ALGEBRAIC` is **1**. **LINAGE-COUNTER**: GR1 ties its
  size to "the page size specified in the LINAGE clause" — `HIGHEST-ALGEBRAIC(LINAGE-COUNTER OF f)` folds to the
  LINAGE literal's value directly (`LINAGE IS 66 LINES` → 66); with a data-name operand the page size is set at
  run time, and the compile-time capacity is the MAXIMUM that operand can specify, its PICTURE's all-nines.
  **PAGE-COUNTER / LINE-COUNTER**: the standard gives them no size and no usage, so CLAUDE.md rule 1's latitude order
  (ISO → GnuCOBOL → IBM / Micro Focus) takes **GnuCOBOL's, surveyed with cobc 3.2.0 (kb/Work PB1746)**: `build_report`
  (cobc/tree.c) declares both `CB_USAGE_UNSIGNED_INT`, an **unsigned 32-bit binary integer**. WiseOwl COBOL's
  description is therefore the shape of `BINARY-LONG UNSIGNED` (`PicInfo.ReportCounterItem`, the one statement of it;
  the `OCCURS DYNAMIC` CAPACITY register is the same shape): capacity **4,294,967,295**, `HIGHEST-ALGEBRAIC` folds to
  it, the character image is **10 digits**, and `FUNCTION LENGTH` / `BYTE-LENGTH` are **4**. The survey, against
  cobc 3.2.0: `DISPLAY PAGE-COUNTER` prints `0000000001` (10 digits); `MOVE PAGE-COUNTER TO` a `PIC X(4)` item stores
  `0000`; `MOVE 5000000000 TO PAGE-COUNTER` leaves `0705032704` (the 32-bit value) and `ADD 1` to 4,294,967,295 leaves
  `0000000000`; `STRING` refuses the register ("must be USAGE DISPLAY or NATIONAL"); `FUNCTION LENGTH(PAGE-COUNTER)`
  and `PERFORM PAGE-COUNTER TIMES` are syntax errors there (WiseOwl COBOL admits both, §2023:15.50.3 r1 and
  §2023:8.4.3.15.3 SR1 — the standard decides what GnuCOBOL does not take); `HIGHEST-ALGEBRAIC(PAGE-COUNTER)` prints
  `294967295` and a `MOVE` of the register to `PIC X(14)` shows 9 digits (cobc's attribute digit count for
  `UNSIGNED-INT` is 9 where its DISPLAY prints 10 — a GnuCOBOL inconsistency WiseOwl COBOL does not copy: the
  description is one 10-digit item, as `BINARY-LONG UNSIGNED` is here). **Runtime:** the report engine stores the
  counters in a `long` and reduces every assignment modulo `CobolReport.CounterModulus` (2³²), so a page advance
  past the capacity, or `MOVE 5000000000 TO PAGE-COUNTER`, wraps exactly as cobc's does;
  `ReportCounterDescriptionDriftTests` pins the runtime modulus to the compiler's description.
  (§2023:8.5.2.12 items 3/4/5 make the registers category-numeric DATA ITEMS, which is what admits them to the
  §2023:15.43.3/§2023:15.58.3/§2023:15.83.3 r1 argument position at all.) **The same capacity answers the §2023:8.8.4.4.4 GR3 g)/m)
  class conditions** (kb/Work PB1401): `LINAGE-COUNTER OF f IS FARTHEST-FROM-ZERO` is true exactly when the
  register holds the capacity `HIGHEST-ALGEBRAIC` folds to, and `IS NEAREST-TO-ZERO` when it holds 1 — one
  statement of the description (`AlgebraicRanges.DescribeCounter`) serves every surface, so they cannot disagree. A
  register `IS NUMERIC` is always true (GR3 n) 1. c.: its content is a value the register holds), and
  `IS IN-ARITHMETIC-RANGE` is always true (every capacity lies inside every mode's intermediate).
  **The registers' implicit DESCRIPTION** (kb/Work PB1153, PB1199, PB1318, PB1746): **LINAGE-COUNTER** is the unsigned
  integer item **`PIC 9(d) USAGE DISPLAY`**, d the digits of the page size (`LINAGE IS 20 LINES` → `9(2)`; a
  data-name page size → its PICTURE's digits) — GR1 gives it a size in digits, which is a DISPLAY item's; GnuCOBOL's
  `UNSIGNED-INT` LINAGE-COUNTER (a 10-digit image whatever the page size) contradicts that GR, so the standard decides.
  **PAGE-COUNTER / LINE-COUNTER** are the unsigned 32-bit binary integer above. Consequences: `DISPLAY LINAGE-COUNTER`
  after `LINAGE IS 20 LINES`, OPEN OUTPUT and one WRITE shows `02`; `DISPLAY PAGE-COUNTER` after INITIATE shows
  `0000000001`; `MOVE LINE-COUNTER TO` a `PIC X(4)` item stores the leftmost four of the 10 digits (§2023:14.9.25.4 GR6 a));
  every register is an integer for `PERFORM … TIMES`. **Usage screens follow the description:** STRING's identifiers
  "shall be described … as usage display or national" (§2023:14.9.43.3 SR1) and §2023:8.8.4.2.5's numeric integer operand "shall
  be … of usage display or national", so **LINAGE-COUNTER takes a STRING sender and an alphanumeric relation, and
  PAGE-COUNTER / LINE-COUNTER are refused in both** (COBOL1626, COBOL2532), because those rules read the item's usage
  "described implicitly or explicitly" and the registers' implicit usage is binary. **§2023:14.9.25.3 SR8 does not reach the
  registers**: it keys on "a data item described with usage binary-char, binary-short, binary-long, or binary-double" —
  a USAGE clause a program wrote — and §2023:8.4.3.15.4 GR1 gives the counters no usage clause, only the category numeric and
  the unsigned-integer shape the implementor pins above; so `MOVE PAGE-COUNTER TO` an alphanumeric item is judged by
  Table 16's numeric-integer row alone and is legal (and cobc accepts it), whereas `MOVE` of a declared
  `BINARY-LONG UNSIGNED` item to one is COBOL0819 (`MoveTable16.ShapeRefusal`, kb/Work PB1746 review R1-1);
  UNSTRING's identifiers "shall reference data items of category alphanumeric or national" (§2023:14.9.48.3 SR2), which a
  numeric register is not, so **all three registers are refused as an UNSTRING sender or delimiter** (COBOL1651;
  before PB1746 the statement compiled and split the register's image). `AlgebraicRanges.CounterRegisterDigits` /
  `CounterRegisterByteLength` / `CounterRegisterIsUsageDisplay` / `IsCounterRegister` are the one reader every consumer
  asks (`OperandText.AsString`, `IntrinsicResultType.IsIntegerOperand`, `RelationComparability`, the STRING / UNSTRING
  binder, the LENGTH / BYTE-LENGTH folds). Witnesses: `conformance:cases/8.4.3.14-LINAGE-COUNTER/pb1153_counter_register_character_image`,
  `conformance:cases/_unassigned/algebraic_counter_registers`, `wiseowl:cases/8.4.3.15-Report-counters/pb1746_report_counter_description`,
  `wiseowl:cases/8.4.3.15-Report-counters/pb1746_page_counter_advance_wraps` and the `pb1746-*` refusals.
