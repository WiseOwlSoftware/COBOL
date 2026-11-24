## PB5725: a >>TURN directive naming one exception-name twice is refused below 2023

§2002:7.2.19.2 SR3 (and §2014:7.3.20.2 SR3 the same): "No exception-name shall be specified more than once in one TURN
directive"; SR4 adds that no file-name is specified twice for one exception condition. §2023:7.3.25.3 SR3 relaxes the
first to the exception-name and file-name COMBINATION. `TurnDirectiveProcessor.ParseTurn` keyed its duplicate test on the
combination at every edition, so `>>TURN EC-I-O F1 EC-I-O F2 CHECKING ON` compiled and ran ("OK", rc 0) at 2002 and at
2014 on the re-probe at this tree (the note named 2002; 2014 carries the identical text and had the same hole).

Fix: `ParseTurn` also keeps the set of exception-names written and, where the new
`DialectBehavior.TurnDirectiveRepeatsOnlyNameFileCombination` (ChangedIn 2023, VCR 7.126) is not active, reports COBOL0718
for the second spelling of a name. The pair set stays at every edition and now cites the edition's own rule (SR4 at 2002 and
2014, SR3 at 2023) through `EditionInfo.CiteRule`; a repeated name reports once, not again for its pairs. Re-probe after:
2002 and 2014 refuse (exit 65, COBOL0718 citing the edition), 2023 still prints OK.

Goldens: `7.3.25-TURN/pb5725_turn_name_with_two_files` (accept 2023) and the refusal
`7.2.19@2002-TURN-DIRECTIVE/pb5725-turn-exception-name-repeated` (refuse at 2002 and 2014, COBOL0718); unit theories in
`TurnDirectiveExceptionNameTests` cover the repeated name, the repeated file (SR4), the repeated pair and the legal
`EC-I-O F1 EC-SIZE EC-I-O-AT-END F1` at 2002, compiled and omitted-branch alike. Row `2002-SR-7.2.19.2-3` recorded
CONFORMS (2002 GAP 1134 to 1133). No new diagnostic code used.
