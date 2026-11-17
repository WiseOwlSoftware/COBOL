*>--- conformance-case
*> id: pb5876-linkage-odo-object-table-all-argument
*> rules: [SR-13.7.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5876 (review finding R1-1, sibling) -- ISO §2023:13.7.3 SR4: a linkage item "may be referenced within the
      *> procedure division of that source element if, and only if," it meets one of the rule's conditions;
      *> §2023:8.4.5: "An implicit reference in a procedure division statement occurs when the name of the data
      *> item is not written in that procedure division statement and it contributes to, or is affected by, the
      *> execution of the statement." The argument L-E (ALL) stands for every current occurrence of the table
      *> (§2023:15.3), whose number is the value of L-N, its OCCURS DEPENDING ON object, so the statement references L-N;
      *> L-N's record L-2 is no USING operand and not BASED, so the program is refused with COBOL2746. It used to
      *> compile and fail at run time with EC-ARGUMENT-FUNCTION (L-N read from storage the caller never passed).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5876FM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-1.
          05 W-E PIC X OCCURS 5 TIMES.
       PROCEDURE DIVISION.
           MOVE "EDCBA" TO W-1
           CALL "PB5876FN" USING W-1
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5876FN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-1.
          05 L-E PIC X OCCURS 1 TO 5 DEPENDING ON L-N.
       01 L-2.
          05 L-N PIC 9.
       PROCEDURE DIVISION USING L-1.
           DISPLAY FUNCTION MAX (L-E (ALL))
           GOBACK.
       END PROGRAM PB5876FN.
       END PROGRAM PB5876FM.
