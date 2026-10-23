*>--- conformance-case
*> id: pb3586_function_returning_usage_bit
*> rules: [GR-8.4.3.2.4-1, GR-15.2-2]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3586 - ISO 8.4.3.2.4 GR1: "the description, class, and category of the temporary data item is that
      *> specified by the description in the linkage section of the item specified in the RETURNING phrase of the
      *> procedure division header of the function prototype", and 14.6.5: "The result of the execution of a program,
      *> function, or method that specifies a RETURNING phrase in its procedure division header, is the content of the
      *> data item referenced by that RETURNING phrase". A RETURNING item described PIC 1(n) USAGE BIT therefore
      *> delivers all n boolean positions it holds to the caller (the same function without USAGE BIT already did; the
      *> bit form delivered a single position, so the caller read all zeros).
      *>
      *> DERIVATION. PB3586F8 moves the boolean literal B"00001010" to its 8-position RETURNING item, PB3586F12 moves
      *> B"101010101111" to its 12-position one (more than one storage character, not a multiple of eight), and the
      *> called program PB3586S moves B"01010101" to its RETURNING item. A boolean MOVE of equal length copies the
      *> positions unchanged, so each receiver, each DISPLAY and the relation condition hold exactly those positions.
      *> Each leg fails alone: F8-ITEM and F12-ITEM (the function result moved to a USAGE BIT receiver), F8-DISP (to a
      *> display-usage boolean receiver), F8-DIRECT (the function-identifier as a DISPLAY operand), F8-EQ (a boolean
      *> relation condition) and CALL-RET (the CALL ... RETURNING twin of the same boundary).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB3586F8.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9.
       01 L-R PIC 1(8) USAGE BIT.
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           MOVE B"00001010" TO L-R.
           GOBACK.
       END FUNCTION PB3586F8.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB3586F12.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9.
       01 L-R PIC 1(12) USAGE BIT.
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           MOVE B"101010101111" TO L-R.
           GOBACK.
       END FUNCTION PB3586F12.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3586MAIN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB3586F8
           FUNCTION PB3586F12.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B8   PIC 1(8) USAGE BIT.
       01 B8D  PIC 1(8).
       01 B12  PIC 1(12) USAGE BIT.
       01 BC   PIC 1(8) USAGE BIT.
       PROCEDURE DIVISION.
           MOVE FUNCTION PB3586F8(3) TO B8
           DISPLAY "F8-ITEM=" B8
           MOVE FUNCTION PB3586F8(3) TO B8D
           DISPLAY "F8-DISP=" B8D
           MOVE FUNCTION PB3586F12(3) TO B12
           DISPLAY "F12-ITEM=" B12
           DISPLAY "F8-DIRECT=" FUNCTION PB3586F8(3)
           IF FUNCTION PB3586F8(3) = B"00001010"
               DISPLAY "F8-EQ=YES"
           ELSE
               DISPLAY "F8-EQ=NO"
           END-IF
           CALL "PB3586S" RETURNING BC
           DISPLAY "CALL-RET=" BC
           STOP RUN.
       END PROGRAM PB3586MAIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3586S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-R PIC 1(8) USAGE BIT.
       PROCEDURE DIVISION RETURNING L-R.
       P.
           MOVE B"01010101" TO L-R.
           GOBACK.
       END PROGRAM PB3586S.
