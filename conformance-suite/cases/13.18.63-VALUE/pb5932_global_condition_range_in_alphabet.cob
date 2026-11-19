*>--- conformance-case
*> id: pb5932_global_condition_range_in_alphabet
*> rules: [GR-13.18.63.4-18, GR-14.7.8-2, GR-13.18.27.4-1, GR-13.18.27.4-2,
*>    SR-14.9.13.3-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> A CONTAINER'S GLOBAL CONDITION-NAME KEEPS ITS IN PHRASE'S SEQUENCE IN EVERY CONTAINED PROGRAM
      *> kb/Work PB5932, review round 1 of listing step 6b slice 4. 13.18.27.4 GR1: "All condition-names
      *> associated with a global name are global names"; GR2: a statement in a program contained directly
      *> or indirectly within the program that describes a global name may reference that name without
      *> describing it again. 14.7.8 GR2: "when the IN alphabet-name phrase is specified, the collating
      *> sequence used for range evaluation is the collating sequence defined by that alphabet" - the
      *> alphabet the VALUE clause names, wherever the condition-name is tested.
      *> The carrier of OK's sequence is OUTER's. A contained program that tested OK read a carrier of its
      *> OWN instead: a position in OUTER's list read against the contained program's list named another
      *> alphabet's carrier (a silent wrong answer) or none (the compiler failed); before that, a lookup by
      *> name found nothing in a contained program that named no range of its own (native order).
      *>
      *> EXPECTED VALUES. ALPHABET AL IS "B" "A" assigns ascending positions in written order (12.3.7.4
      *> GR7 k): position(B)=0, position(A)=1, and every unspecified character a position above them. X
      *> holds "A", so `"B" THRU "A" IN AL` is 0 <= 1 <= 1 -> true. Under the native order the same range
      *> is inverted ("B" > "A") and 14.7.8 makes it empty -> false, so each OK line discriminates.
      *> ALPHABET AR IS "A" "B" "C" orders A=0, B=1, C=2: it gives each contained program a carrier of its
      *> own, made before OK is tested, whose order makes OK false if it is read in OK's place.
      *>   OUTER      IF OK                                          -> OUTER=IN
      *>   INNER1     names no range of its own; IF OK               -> INNER1=IN
      *>   INNER2     Y="B": "A" THRU "C" IN AR is 0 <= 1 <= 2       -> Y=IN;  IF OK -> INNER2=IN
      *>              V is of the GLOBAL type T, whose condition-name OK-T has OK's VALUE clause: the TYPE
      *>              clause's clone keeps the template's IN phrase, and X OF V = "A"   -> V=IN
      *>   INNERMOST  contained in INNER2 (GR2's "indirectly"); Z="A": "A" THRU "B" IN AR is
      *>              0 <= 0 <= 1                                    -> Z=IN;  IF OK -> INNERMOST=IN
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5932OUTER.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS "B" "A"
           ALPHABET AR IS "A" "B" "C".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G GLOBAL.
          05 X PIC X VALUE "A".
             88 OK VALUE "B" THRU "A" IN AL.
       01 T TYPEDEF GLOBAL.
          05 TX PIC X.
             88 OK-T VALUE "B" THRU "A" IN AL.
       PROCEDURE DIVISION.
       MAIN-P.
           IF OK DISPLAY "OUTER=IN" ELSE DISPLAY "OUTER=OUT" END-IF
           CALL "PB5932INNER1"
           CALL "PB5932INNER2"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5932INNER1.
       PROCEDURE DIVISION.
       INNER1-P.
           IF OK DISPLAY "INNER1=IN" ELSE DISPLAY "INNER1=OUT" END-IF
           GOBACK.
       END PROGRAM PB5932INNER1.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5932INNER2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Y PIC X VALUE "B".
       01 V TYPE T.
       PROCEDURE DIVISION.
       INNER2-P.
           EVALUATE Y
               WHEN "A" THRU "C" IN AR  DISPLAY "Y=IN"
               WHEN OTHER               DISPLAY "Y=OUT"
           END-EVALUATE
           IF OK DISPLAY "INNER2=IN" ELSE DISPLAY "INNER2=OUT" END-IF
           MOVE "A" TO TX OF V
           IF OK-T OF V DISPLAY "V=IN" ELSE DISPLAY "V=OUT" END-IF
           CALL "PB5932INNERMOST"
           GOBACK.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5932INNERMOST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Z PIC X VALUE "A".
       PROCEDURE DIVISION.
       INNERMOST-P.
           EVALUATE Z
               WHEN "A" THRU "B" IN AR  DISPLAY "Z=IN"
               WHEN OTHER               DISPLAY "Z=OUT"
           END-EVALUATE
           IF OK DISPLAY "INNERMOST=IN" ELSE DISPLAY "INNERMOST=OUT" END-IF
           GOBACK.
       END PROGRAM PB5932INNERMOST.
       END PROGRAM PB5932INNER2.
       END PROGRAM PB5932OUTER.
