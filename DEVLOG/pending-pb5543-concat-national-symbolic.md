## A national symbolic-character in a concatenation expression keeps its category (PB5543)

Re-probe: with `SYMBOLIC CHARACTERS FOR NATIONAL SYM-N IS 67`, `MOVE "X" & SYM-N TO W-X` compiled at 2023, folding a national figurative as an alphanumeric character. `ConcatFolder.Classify` read `LiteralEnvironment.SymbolicValue`, which returns only the value of `SymbolicOf`'s `(Value, National)`.

Fix: `Classify` reads `SymbolicOf` and `ConcatFolder.Term` carries `NationalOnly`. A symbolic-character stays a figurative constant that takes its partner's class (§2023:8.8.3.3 GR1a), but a national one (§2023:12.3.7.4 GR11c) has no alphanumeric value, so `ConcatFolder.Append` reports COBOL1540 (§2023:8.8.3.2 SR1) when its pair is not national; `SYM-N & SYM-N` is alphanumeric by GR1b and is refused the same way. `Append` also appends the symbolic value whole rather than `Value[0]`. `LiteralEnvironment.SymbolicValue` had no other caller and is deleted, so no caller can drop the flag (the siblings PB5523 and PB5420 had fixed the other two readers).

Goldens: `pb5543_concat_national_symbolic_character` (accept 2002; refused at 85, COBOL0900) and `pb5543-concat-national-symbolic-alphanumeric-pair` (refused 2002, 2014, 2023, COBOL1540). Rows closed: none.
