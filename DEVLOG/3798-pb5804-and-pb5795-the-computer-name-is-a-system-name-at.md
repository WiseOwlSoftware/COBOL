## Entry 3798 — 2026-11-16 15:46 PST — PB5804 and PB5795: the computer-name is a system-name at COBOL-85, in both directions

Two 1985 code-read findings on the one slot the SOURCE-COMPUTER and OBJECT-COMPUTER paragraphs share, `computerName`.
"Computer-name is a system-name." (§1985:VI.4.3.3 SR1, §1985:VI.4.4.3 SR1, §1985:XVI.2.2.3 SR1)

**PB5804 (rejected legal source).** At `--std 85` CLASS-ID, FUNCTION-ID, INTERFACE-ID and REPOSITORY were refused as a
computer-name, though 1985 reserves none of them. Re-probed on the base build: `OBJECT-COMPUTER. CLASS-ID.` drew COBOL5506 at
PROCEDURE, with SEGMENT-LIMIT after it COBOL0001, REPOSITORY COBOL0900 and COBOL3734, and SOURCE-COMPUTER the same; INTERFACE-ID,
which no earlier probe ran, failed too. The optional name let the parse leave the paragraph at a word that heads a 2002
construct, so the token-level reservation gate's second witness (a syntax error AT the free word) never fired. The slot now
offers the generated `reservedGatedWord` rule behind a left-edge, migration-blind predicate, `gatedWordFreeHere`: a gated word
the edition leaves free is the name, its match frees it, and the gate retypes it and parses again; where the edition reserves
the word, the slot is skipped, so `OBJECT-COMPUTER.` followed by `REPOSITORY.` is still the empty paragraph and the REPOSITORY
paragraph at 2002 and 2023. The sweep compiled all 102 gated words 1985 does not reserve as both computer-names at 85: every
one compiles. At 2002 and later `OBJECT-COMPUTER. CLASS-ID.` now draws COBOL0901 at the word instead of COBOL5506 at the next
token (§2023:8.3.2.1 SR1 bars a reserved word from a system-name).

**PB5795 (under-rejection).** At `--std 85` a computer-name spelled as one of this implementation's implementor-names
(`SOURCE-COMPUTER. CONSOLE.`, `OBJECT-COMPUTER. SWITCH-1.`) compiled and ran. §1985:IV.4.2.2.1.2 makes computer-names,
implementor-names and language-names "disjoint sets" within an implementation; 2002 and 2023 keep the disjointness for
device-name, feature-name and switch-name only (§2002:8.3.1.1.2, §2023:8.3.2.3.1). `VersionConformancePass.GateComputerName`,
asked by both paragraph visitors, now refuses below 2002 a computer-name that is a row of `ImplementorNames` or of
`ImplementorCodeNames`, with the new COBOL7624 naming the kind. DOC-A.1-198 and DOC-A.1-127 state the 1985 exception. The
other 1985 implementor-name positions that take any word here (ASSIGN, VALUE OF, RERUN, RECORD DELIMITER) are a lead in the
wave report, not part of this fix.

**Goldens.** `pb5804_later_reserved_words_as_computer_names_85` and `pb5804_interface_id_repository_as_computer_names_85`
(accept 85), `pb5804-class-id-computer-name-reserved-2002` (refused at 2002, 2014 and 2023, COBOL0901),
`pb5795-computer-name-is-implementor-name-85` (refused at 85, COBOL7624) and `pb5795_computer_name_spelled_as_device_name`
(accept 2002, 2014, 2023), with `ComputerNameSystemNameTests` taking one implementor-name of each kind. Witnesses were added to
`1985-SR-VI.4.3.3-1`, `1985-SR-VI.4.4.3-1` and `1985-SR-XVI.2.2.3-1` (KEPT change records, GAP 25 to 25) and to
`SR-8.3.2.1-1`, `FMT-12.3.5.2` and `FMT-12.3.6.2` (GAP 12 to 12).

