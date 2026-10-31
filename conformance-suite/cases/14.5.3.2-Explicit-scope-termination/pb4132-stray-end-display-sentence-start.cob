*>--- conformance-case
*> id: pb4132-stray-end-display-sentence-start
*> rules: [GR-14.5.3.2-1, SR-8.3.2.1-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4132 (review R1-1). ISO 14.5.3.2: "The scope of a statement may be
*> explicitly terminated by using its associated scope terminator as specified
*> in Table 12" (cite.py --check 14.5.3.2 -> OK). END-DISPLAY below begins a
*> sentence: no statement is open, so it terminates nothing and is refused
*> COBOL0306, the misplaced scope terminator, at the word, as a stray END-ADD in
*> the same place is.
*> Nor is it a paragraph-name: ISO 14.4.3 "A paragraph consists of a
*> paragraph-name followed by a separator period" (cite.py --check 14.4.3 -> OK),
*> and no period follows the word. END-DISPLAY is reserved from 2002 (ISO 8.9),
*> so there it is that keyword. It used to be read as a paragraph header missing
*> its period, refused at MOVE with COBOL0307 "A period may be missing", and the
*> period that advice adds makes it a header naming a reserved word (ISO 8.3.2.1
*> 1), COBOL0901). At 85, where 8.9 leaves END-DISPLAY free, the same text is
*> that paragraph header missing its period, a different refusal, so 85 is not
*> listed.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4132SS.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC 9 VALUE 0.
01 W-K PIC 9 VALUE 0.
PROCEDURE DIVISION.
MAIN-P.
    MOVE 1 TO W-N.
    END-DISPLAY MOVE 2 TO W-K.
    DISPLAY W-N W-K.
    STOP RUN.
