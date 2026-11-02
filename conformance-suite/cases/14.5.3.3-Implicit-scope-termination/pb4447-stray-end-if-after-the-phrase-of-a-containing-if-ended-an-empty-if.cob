*>--- conformance-case
*> id: pb4447-stray-end-if-after-the-phrase-of-a-containing-if-ended-an-empty-if
*> rules: [GR-14.5.3.3-4]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4447 review R1-1. ISO 14.5.3.3 4): "for a conditional statement contained within another statement, by ... b)
*> the next phrase of any containing statement", and "Any phrase encountered is the next phrase of the most-recently
*> preceding unterminated statement with which that phrase may be syntactically associated. If all permitted
*> occurrences of a phrase have already been specified for a given statement, a subsequent occurrence of that phrase is
*> not syntactically associated with that statement" (cite.py --check 14.5.3.3 -> OK). The inner IF has no statement-1
*> (one fault, COBOL2072, at its first ELSE: ISO 5.2.6.3) and takes the one ELSE its format prints, so the second ELSE is
*> the outer IF's, which ends the inner IF. ISO 14.5.3.2 1): the first END-IF terminates the outer IF, the most-recently
*> preceding unterminated IF, and the second END-IF has none: refused COBOL0306. It fails if the inner IF still awaits
*> its END-IF after the outer IF's phrase ended it, so that the first END-IF is taken for the inner IF's and the true
*> stray END-IF is never reported.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4447PHRASEEND.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC 9(4) VALUE 1.
01 W-K PIC 9(4) VALUE 1.
PROCEDURE DIVISION.
MAIN-P.
    IF W-N = 1 IF W-K = 2 ELSE DISPLAY "X" ELSE DISPLAY "Y" END-IF END-IF.
    STOP RUN.
