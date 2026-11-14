- **D-TERMINATION-STATUS — STOP RUN / GOBACK termination status (§2023:14.9.42.4 GR5 / §2023:14.9.18.4 GR10 — Annex A required items 192/193)**:
  the status "passed to the operating system" and the ERROR/NORMAL termination indication both map to the single
  observable available on .NET — the process exit code (`Environment.ExitCode`). The constraint on the STATUS
  operand (item 192): the integer value of `literal-1` / `identifier-1` (truncated toward zero) becomes the exit
  code; a **non-numeric** operand — a display/national data item per §2023:14.9.42.3 SR2, **or a non-numeric
  literal-1**, which SR3's conditional ("if literal-1 *is* numeric …") and SR4 (which bars only a zero-length
  literal) together admit — is interpreted numerically: its digit characters decode as an unsigned integer, a
  non-digit position contributing no digit (`CobolNum.FromAlphanumeric`, the same §2023:14.6.13.2 deterministic decode
  an alphanumeric operand takes in any numeric context). ⚠ **A consequence worth stating: an all-non-digit
  literal maps to 0**, so `STOP RUN WITH ERROR STATUS "ABEND"` exits 0 — indistinguishable at the process level
  from NORMAL termination. That follows from this determination rather than from the ERROR/NORMAL indication,
  which GR5's value phrase overrides. ✅ Surveyed 2026-08-31 before being kept (kb/Work PB169): GnuCOBOL 3.2
  binds the phrase to ONE integer and hands the operand's value to `cob_stop_run` → `exit(status)`, with 1/0
  substituted only when the operand is ABSENT — no arm anywhere gives a non-numeric operand a distinguished
  exit — and no vendor documentation reachable in the survey does either. This determination is the consensus
  shape, not an outlier.
  **"NON-NUMERIC LITERAL-1" MEANS EVERY NON-NUMERIC LITERAL FORM**, which the code now implements rather than
  only the quoted one: a FIGURATIVE constant (§2023:8.3.3.6.3 SR1 admits one "whenever 'literal' appears in a format",
  and §2023:8.3.3.6.4 GR3 b + its NOTE 2 — which names the STOP statement — give it a length of ONE character, so
  `STATUS SPACE` exits 0 and `STATUS ZERO` exits 0), `ALL literal-1` (GR3 c gives it literal-1's length, so
  `STATUS ALL "07"` exits 7), `ALL symbolic-character-1`, a BOOLEAN literal and a boolean CONCATENATION (their
  '0'/'1' characters decode, so `STATUS B"01"` exits 1 and `STATUS B"1" & B"0"` exits 10), a NATIONAL literal and
  a HEXADECIMAL alphanumeric literal. ⚠ **NULL is NOT one of them**: §2023:8.3.3.6.2 lists no NULL format, §2023:8.4.3.10.1
  makes it a predefined address / object reference and §2023:8.4.3.10.3 SR1 admits it only in INITIALIZE/SET, a
  prototype argument, or a pointer-or-object-reference relation condition — so `STATUS NULL` is rejected
  (COBOL1704), not mapped. The error-termination mechanism
  (A.1 item 192; item 193, the implementor-defined error-termination mechanism, is recorded in §7 as not provided): when a status phrase specifies `ERROR` with **no** STATUS value, the exit code is 1; `NORMAL` (or
  no phrase) is 0. When a STATUS value is present it wins regardless of ERROR/NORMAL. A main-program GOBACK
  (§2023:14.9.18.4 GR3) uses the same mapping; a status phrase on a GOBACK executed in a **called** program is inert
  (GR2). Programs with no status phrase leave the exit code at 0 (the abnormal-fatal case still forces item 44's
  nonzero exit). The status is the run unit's own `RunUnit.ExitStatus`, written by the STOP statement wherever in the
  run unit it executes (so it crosses assembly boundaries — a separately-compiled module's STOP RUN … WITH STATUS
  reaches the process exit code) and delivered to `Environment.ExitCode` ONCE, when the run unit has ended, by the
  boundary that IS the process (`ProgramTable.RunModule`, the generated `Main`'s statement). A run unit embedded in a host
  (the generated host entry `Cobol.<assembly>.<NAME>.Call`, inside or outside `RunUnit.Run`) never writes the host's exit
  code (kb/Work PB3163; A.1 item 167): a run unit that STOP RUN or a fatal condition ended reaches the host as the
  `RunUnitEndedException` carrying the status, and the host reads a run unit's status still in progress from
  `RunUnit.ExitStatus`. Two host limits apply on top of the value mapping: the exit code is an `Int32`, so a
  status outside [−2,147,483,648, 2,147,483,647] is **clamped** to the nearer end (`CobolNum.Position32`, the one
  saturating narrowing — see "Integer operands and host carriers" below), never wrapped: before kb/Work PB1178
  `STOP RUN WITH ERROR STATUS 4294967296` wrapped to exit 0, a NORMAL termination reported for an ERROR phrase.
  A POSIX host then reports only the low 8 bits of the exit code — outside COBOL's control — so a STATUS ≥ 256
  is reduced modulo 256 there (2,147,483,647 exits 255; a value that clamps to −2,147,483,648, or any multiple of
  256, exits 0). The value-wins-over-ERROR/NORMAL precedence above is the rule-1 precedence applied: §2023:14.9.42.4
  GR5 leaves "any constraints on the value" to the implementor and GnuCOBOL passes the value as given.
