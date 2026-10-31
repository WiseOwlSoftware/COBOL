## SYMBOLIC CHARACTERS pairs names with ordinals across the whole clause, so a literal ordinal before a constant-name ordinal compiles (PB4108)

`SYMBOLIC CHARACTERS SC1 SC2 66 K2` (K2 an integer constant-name) was refused with COBOL1670 "2 name(s) against 1 integer(s)"
while `SC1 SC2 K1 67` compiled. Re-probe on the built compiler at main f2b93f2ab reproduced RT04, RT06, RT07 and
`SC1 SC2 SC3 66 K2` (3 names, 66, K2), all COBOL1670, each also followed by a false COBOL1639 "'SC1' is not defined" at every use.

Root cause: `symbolicCharacterEntry`'s first alternative (`cobolWord+ (IS|ARE)? integerLiteral+`) ends the entry at the
last literal, so the constant-name ordinal opened a second entry, and `SwitchBindSymbolic` paired `SymbolicEntryGroups`'s
groups one entry at a time. The entry is not a unit of the rule: ISO/IEC 1989:2023 §12.3.7.3 SR16 b) pairs "by position in
the SYMBOLIC CHARACTERS clause" and c) requires a one-to-one correspondence between occurrences of symbolic-character-1 and
of integer-1 (both `cite.py --check` OK; §5.5 1) and §13.10.3 2) make `66` and `K2` two integer-1 occurrences).

Fix: `DataBinder.SymbolicClauseOperands` (replacing `SymbolicEntryGroups`) reads every entry's tokens as one source-order
stream and decides each word a name or an ordinal by the same constant-table fact as before; `SwitchBindSymbolic` pairs the
two lists once. The grammar is unchanged. A refused count now declares the clause's names with a placeholder so the one
COBOL1670 no longer cascades into COBOL1639 at each use.

The lead RT06 (`SC1 SC2 66 SC3 67 68`, 3 names against 3 integers) is now accepted and pairs SC1=66, SC2=67, SC3=68:
SR16 b/c speak of positions and occurrences in the clause, and the braced group of the format repeats. GnuCOBOL 3.2 refuses
it; the ISO text controls (CLAUDE.md rule 1).

Goldens: `12.3.7-SPECIAL-NAMES/pb4108-symbolic-literal-then-constant-ordinals` (accept 2002, where the CONSTANT entry begins;
expected `AB / CDF / GHI / JK` computed from the UTF-16 native order, ordinal n is code n-1) and the refused
`pb4108-symbolic-clause-count-mismatch` (COBOL1670 at 2002, 2014, 2023). No inventory row claimed or closed.

Review round 1 found that the one-stream pairing checked only the totals: a clause whose operand kinds do not form the
format's repeated group `{symbolic-character-1}... [IS|ARE] {integer-1}...` (ISO §12.3.7.2) but whose counts balance, such as
`SC1 66 67 SC2` or `K1 SC1 SC2 66`, compiled and paired SC2 with 67. `SymbolicClauseOperands` now also reports whether the
stream opens with a name and closes with an ordinal (the only condition for (name+ ordinal+)+), and `SwitchBindSymbolic`
refuses otherwise with one COBOL1670 and the names still declared. Goldens `pb4108-symbolic-trailing-name-without-ordinal`
(COBOL1670 at 85, 2002, 2014, 2023: it uses no CONSTANT entry, and the clause's format is COBOL-85's too; review round 2's lead
R2-1, fixed by the landing's finisher) and `pb4108-symbolic-leading-constant-before-names` (COBOL1670 at 2002, 2014, 2023).
The PB4110 note's reference to the renamed member was updated. The landing's merge of main moved the three refusals into
main's one-file-per-test layout (`conformance-suite/wiseowl/diagnostics/<id>.json`, kb/Work PB4147).
