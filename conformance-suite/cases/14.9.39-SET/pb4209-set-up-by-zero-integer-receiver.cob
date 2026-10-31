*>--- conformance-case
*> id: pb4209-set-up-by-zero-integer-receiver
*> rules: [FMT-14.9.39.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39.2 prints no SET general format whose UP BY / DOWN BY receiving
      *> operand is an integer data item: Format 2's receiver is index-name-3
      *> (GR4 "For each occurrence of index-name-3"), Format 10's a data-pointer
      *> and Format 14's a CAPACITY register. The COBOL2112 refusal quotes the
      *> amount AS THE PROGRAMMER WROTE IT (kb/Work PB4209): the token rewriter
      *> gives a bare ZERO the text "0", and the message used to print
      *> UP/DOWN BY 0. Fails if the message spells the rewritten token.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4209B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           SET WS-N UP BY ZERO
           DISPLAY WS-N
           STOP RUN.