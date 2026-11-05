*>--- conformance-case
*> id: pb4707-evaluate-refused-pair-simple-boolean-condition-length
*> rules: [SR-14.9.13.3-10, SR-8.8.4.3.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4707. ISO 14.9.13.3 SR10 and Table 15 refuse the pair (TRUE selection subject, partial-expression object):
*> COBOL1634. The object's own condition "> 1 AND BW" is a second, independent fault: BW, a bare boolean item, is a
*> simple boolean condition (8.8.4.3), and 8.8.4.3.3 SR1, "Boolean-expression-1 shall reference only boolean items of
*> length 1.", refuses it because BW is a boolean item of length 2: COBOL1511, as the same condition in an IF draws
*> (l1-bool-condition-length-8). Fails if the standalone walk of the refused object ends the insertion at the boolean
*> operand without asking the length-1 screen that only the simple-boolean-condition wrapper carries (the walk drew
*> COBOL1634 alone), or if the program is accepted.
*>   cite.py --check 8.8.4.3.3 "Boolean-expression-1 shall reference only boolean items of length 1." -> OK 1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4707C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BW PIC 1(2) VALUE B"10".
       PROCEDURE DIVISION.
           EVALUATE TRUE
               WHEN > 1 AND BW
                   DISPLAY "B"
           END-EVALUATE.
           STOP RUN.
