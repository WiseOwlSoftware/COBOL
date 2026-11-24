## Entry 4019 — 2026-11-23 17:11 PST — PB5709: a 2002 constant entry LENGTH OF / BYTE-LENGTH OF operand may carry a data-name subscript

`ConstantEntryBinder.BindConstantLength` applied §2023:13.10.3 SR3 ("All subscripts of data-name-1 and data-name-2 shall be literals") at every edition, so `01 K CONSTANT AS LENGTH OF E (I)` was refused COBOL1547 at `--std 2002` and the first use of K drew a COBOL2364 cascade. The 2002 constant entry has no such rule: §2002:13.9.2 SR3 says only "Data-name-1 and data-name-2 may be qualified", and the literal-only rule first stands in §2014:13.10.2 SR3.

Re-probe on this build (`varsub.cob`, `--std 2002 --run`): before, COBOL1547; after, `K=3 KB=3`. At 2014 and 2023 the refusal is unchanged (COBOL1547 for LENGTH OF and BYTE-LENGTH OF alike: one binder arm serves both phrases, so the sibling is fixed in the same change).

Fix shape: the edge is a named behaviour, `DialectBehavior.ConstantLengthSubscriptsAreLiterals` (ChangedIn 2014, VCR row 7.124), asked through `DialectBehaviors.IsActive` by `ConstantEntryBinder.LiteralSubscriptsRequired`. From 2014 the subscripts are screened for form and counted as before. At 2002 they are read by the one subscript reading, `SubscriptBinder.ReadSubscripts` (names, index-name association, arity). That reading answers Deferred, silently, for a position this data-division resolver has no statement to evaluate (a function-identifier, a decimal literal, a non-integer constant-name); GR1b makes a non-integer value a run-time condition and the length never depends on it (§2002:13.9.3 GR5/GR6), so the binder accepts it. It still checks every written name, inside a function argument too (nothing binds a constant entry's subscript again): an undefined name is refused COBOL1547 (§2002:8.4.1.2.2) and an alphanumeric constant-name is refused as a non-numeric operand (§2002:8.8.1). `E (NOSUCH)`, `E (FUNCTION MIN (NOSUCH 1))` and `E (KC)` with `KC AS "A"` were the silent cases.

Goldens: `13.9@2002-CONSTANT-ENTRY/pb5709-constant-length-data-name-subscript` (accept 2002; K1=3, K2=3 with an arithmetic subscript, K3=4 qualified two-level) and `13.10-Constant/pb5709-constant-length-data-name-subscript-from-2014` with its diagnostics file (refused 2014 and 2023, `§2023:13.10.3 SR3`). Row `2002-SR-13.9.2-3` closed (CONFORMS; GAP 1143 to 1142).

Codes: none allocated were used.

Review R1-1/R1-2/R2-1/R2-2 (kb/Work PB5709): the 2002 non-integer golden became the accept case pb5709-constant-length-nonliteral-subscript; refusals pb5709-constant-length-undefined-subscript-name, -function-undefined-name and -alphanumeric-constant-subscript cover the name checks.

