*>--- conformance-case
*> id: pb6303-linkage-odo-object-condition-name
*> rules: [SR-13.7.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB6303 -- ISO §2023:13.7.3 SR4: a linkage item "may be referenced within the procedure division of
      *> that source element if, and only if," it meets one of the rule's conditions; §2023:8.4.5: "An implicit
      *> reference in a procedure division statement occurs when the name of the data item is not written in that
      *> procedure division statement and it contributes to, or is affected by, the execution of the statement."
      *> The condition-name W-BLANK is tested by IF and set by SET ... TO TRUE; either way the statement evaluates its conditional
      *> variable W-REC, whose length is the value of L-N, the OCCURS DEPENDING ON object of W-T, so the statement references L-N.
      *> L-N's record L-REC is no USING operand (the procedure division has no USING phrase) and not BASED, so each statement is
      *> refused with COBOL2746. It used to compile clean and read the length of W-REC from storage the caller never passed.
      *> Why it can fail: the condition-name's place is built by an entry that skips the implicit LINKAGE screen, so the program compiles.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6303A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-REC.
          88 W-BLANK VALUE SPACES.
          05 W-T PIC X OCCURS 1 TO 5 DEPENDING ON L-N.
       LINKAGE SECTION.
       01 L-REC.
          05 L-N PIC 9.
       PROCEDURE DIVISION.
           IF W-BLANK
               DISPLAY "BLANK"
           END-IF
           SET W-BLANK TO TRUE
           STOP RUN.
