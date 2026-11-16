      *>--- conformance-case
      *> id: g2tn05_with_location
      *> rules: [2002-GR-7.2.19.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.19.3 GR5 - "If the LOCATION phrase is specified, all
      *>   information necessary to identify a source statement for the
      *>   EXCEPTION-LOCATION, EXCEPTION-LOCATION-N, and
      *>   EXCEPTION-STATEMENT functions is made available to the run
      *>   unit." Checking for EC-OVERFLOW-STRING is ON WITH LOCATION;
      *>   the STRING overflows its 3-character receiver
      *>   (§2002:14.8.39.3 GR8b), so the condition is raised and the
      *>   last exception status set to it (§2002:14.5.12.1); ON
      *>   OVERFLOW CONTINUE handles it (GR8c).
      *> The second sentence of GR5 (LOCATION not specified) is the
      *>   implementor's choice, witnessed at 2002 by
      *>   cases/7.3.25-TURN/turn-location-unspecified.
      *> EXPECTED OUTPUT, DERIVED:
      *>   S=[EC-OVERFLOW-STRING<13 sp>]   §2002:15.28.2 RV1, 31 chars.
      *>   L-PREFIX=[G2TN05; MAIN-PARA; ]  §2002:15.25.2 RV2b: the
      *>     PROGRAM-ID name, "; ", the paragraph-name (no section), "; ".
      *>   L-LINE-ID PRESENT  RV2b part 3: an implementor-defined source
      *>     line identifier follows (its value is not pinned: the NOTE
      *>     says it is not consistent between compilations).
      *>   N-PREFIX OK / N-LINE-ID PRESENT  §2002:15.26.2 RV2, the same
      *>     parts as national characters.
      *>   T=[STRING<25 sp>]  §2002:15.27.2 RV2: the statement name in
      *>     upper case, left-justified and space-filled, read through a
      *>     31-character item (RV2's width). GR5 makes the name
      *>     available; the returned value's total width is RV2's own
      *>     row (this build returns 63 characters, a 2002 defect lead).
      *> Fails if: the location is not saved (one space, 15.25.2 RV1),
      *>   a part is missing or misordered, or the statement name wrong.
       >>TURN EC-OVERFLOW-STRING CHECKING ON WITH LOCATION
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TN05.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-DST PIC X(3).
       01 W-LOC PIC X(60).
       01 W-LOCN PIC N(60).
       01 W-STMT PIC X(31).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STRING "ABCDEFG" DELIMITED BY SIZE INTO W-DST
               ON OVERFLOW CONTINUE
           END-STRING
           DISPLAY "S=[" FUNCTION EXCEPTION-STATUS "]"
           MOVE FUNCTION EXCEPTION-LOCATION TO W-LOC
           DISPLAY "L-PREFIX=[" W-LOC(1:19) "]"
           IF W-LOC(20:1) NOT = SPACE
               DISPLAY "L-LINE-ID PRESENT"
           ELSE
               DISPLAY "L-LINE-ID ABSENT"
           END-IF
           MOVE FUNCTION EXCEPTION-LOCATION-N TO W-LOCN
           IF W-LOCN(1:19) = N"G2TN05; MAIN-PARA; "
               DISPLAY "N-PREFIX OK"
           ELSE
               DISPLAY "N-PREFIX BAD"
           END-IF
           IF W-LOCN(20:1) NOT = SPACE
               DISPLAY "N-LINE-ID PRESENT"
           ELSE
               DISPLAY "N-LINE-ID ABSENT"
           END-IF
           MOVE FUNCTION EXCEPTION-STATEMENT TO W-STMT
           DISPLAY "T=[" W-STMT "]"
           STOP RUN.
