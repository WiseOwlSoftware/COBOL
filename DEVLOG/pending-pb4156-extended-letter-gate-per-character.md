## PB4156: the COBOL-85 extended-letter gate is decided per character, not per word

`WordCharacterScreen.Check` (the §8.3.2.1 word-character screen; PB3845 moved it out of `VersionConformancePass.VisitCobolWord`, where
the note located it) asked the 2002 extended-letter gate only when `CobolWordRule.CharacterViolation` found nothing. Below 2002 that
function answers for the first character in NO edition's Annex B (U+00A0, the euro sign), so a word holding one of those AND a real
extended letter skipped the gate for the whole word. Probe on the landing tree: `01 CAFÉ€` at `--std 85` drew COBOL2773 for the euro
sign only; the É, which Annex B holds and the '85 word set lacks, went unnamed.

Fix: `CobolCharacterRepertoire.HasExtendedLetter` asks the question per character (any character some edition's Annex B holds, beyond
the basic repertoire), through a private `IsInSomeAnnexB` that `ViolationAtEveryEdition` now shares, so the distinction between "an
extended letter" and "a character no Annex B holds" is drawn in one place. The gate asks it at every edition (from 2002 the construct
is introduced, so it was already a no-op there). At COBOL-85 the word now draws COBOL0900 for the É and COBOL2773 for the euro sign.

The note's second lead (no test pins that COBOL0900 is absent at 85 for a non-Annex-B character) was already closed by PB3845's
landing (`WordCharacterScreenTests`, parsed and unparsed). Its "present at 2002" cannot hold, since the gate is a 2002 introduction.
New pins: golden `pb4156-extended-letter-beside-euro-sign` (refused at 85 with COBOL0900), a conformance test over six edition and
parse combinations (COBOL0900 only at 85, COBOL2773 always), and a unit test for `HasExtendedLetter`. DESIGN-frontend-grammar.md names the
per-character rule. No inventory row claimed or closed.
