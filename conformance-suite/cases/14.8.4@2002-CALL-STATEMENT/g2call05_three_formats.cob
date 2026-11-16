      *>--- conformance-case
      *> id: g2call05_three_formats
      *> rules: [2002-FMT-14.8.4.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.4.1 - the three 2002 CALL formats: Format 1
      *> (on-overflow, the form 2023 removed: "ON OVERFLOW is an archaic
      *> feature and its use should be avoided"), Format 2 (on-exception,
      *> the ON EXCEPTION / NOT ON EXCEPTION pair in brackets with choice
      *> indicators, so "The alternatives may be specified in any order",
      *> §2002:5.1.5.3) and Format 3 (program-prototype). Each leg writes
      *> a different spelling of the format; the values follow from
      *> §2002:14.8.4.3:
      *>  GR5  BY REFERENCE / BY CONTENT are transitive and BY REFERENCE
      *>       is assumed first (Formats 1, 2); GR4 places the result in
      *>       identifier-3; GR3 / GR3g a missing program raises
      *>       EC-PROGRAM-NOT-FOUND and "control is transferred to
      *>       imperative-statement-1"; GR3h after a successful call ON
      *>       OVERFLOW / ON EXCEPTION "is ignored" and NOT ON EXCEPTION
      *>       runs; GR11 OMITTED makes the omitted-argument condition
      *>       true in the callee.
      *> Derived lines:
      *>  1 F1 literal-1, BY REFERENCE B BY CONTENT N, RETURNING, ON
      *>    OVERFLOW, END-CALL: "S1 ABC 0001" then "1 XYZ 0001 0005"
      *>  2 F1 identifier-1, bare USING (implied BY REFERENCE), no
      *>    RETURNING, no END-CALL: "S2 ABC 0001" then "2 XYZ 0009"
      *>  3 F1 missing program: "3 OVERFLOW TAKEN"
      *>  4 F2 NOT ON EXCEPTION written before ON EXCEPTION, success:
      *>    "S1 ABC 0001" then "4 NOT-EXC 0005"
      *>  5 F2 missing program: "5 EXC TAKEN"
      *>  6 F3 program-prototype-name-1, BY VALUE arithmetic-expression
      *>    3*2+1, BY REFERENCE B, BY CONTENT boolean-expression
      *>    B"1010" B-AND B"0110" = B"0010": "P B=ABC", "P A=0007
      *>    C=0010", "6 R=0070"
      *>  7 F3 literal-1 AS prototype, BY VALUE literal, OMITTED, BY
      *>    CONTENT literal: "P B-OMITTED", "P A=0007 C=1111",
      *>    "7 R=0070"
      *>  8 F3 identifier-1 AS prototype: "P B=ABC", "P A=0002 C=1010",
      *>    "8 R=0020"
      *>  9 F3 literal-1 AS NESTED (contained program): "N ABC"
      *> 10 F3 program-prototype-name-1, BY VALUE identifier-4 (A = 3,
      *>    §2002:14.8.4.3 GR3a), BY REFERENCE B, BY CONTENT C:
      *>    "P B=ABC", "P A=0003 C=1010", "10 R=0030" (L-R = L-A * 10)
      *> 11 F3 BY CONTENT arithmetic-expression-1 A + 4 = 7 to a numeric
      *>    BY REFERENCE formal (§2002:14.8.4.2 SR16): "Q N=0007"
      *> Fails if: any spelling is refused at 2002 (Format 1 above all),
      *> an ON OVERFLOW / ON EXCEPTION branch runs after a successful
      *> call or is skipped after a failed one, the choice-indicator
      *> order is not accepted, or an argument mode is misapplied.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CFP IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-A PIC S9(4) BINARY.
       01 L-B PIC X(3).
       01 L-C PIC 1(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING BY VALUE L-A
           BY REFERENCE OPTIONAL L-B L-C RETURNING L-R.
       END PROGRAM G2CFP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CFQ IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-N PIC S9(4) BINARY.
       PROCEDURE DIVISION USING L-N.
       END PROGRAM G2CFQ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CFM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM G2CFP
           PROGRAM G2CFQ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A   PIC S9(4) BINARY VALUE 3.
       01 B   PIC X(3) VALUE "ABC".
       01 C   PIC 1(4) VALUE B"1010".
       01 N   PIC 9(4) VALUE 1.
       01 R   PIC 9(4) VALUE 0.
       01 NM1 PIC X(8) VALUE "G2CFS1".
       01 NM2 PIC X(8) VALUE "G2CFS2".
       01 NMP PIC X(8) VALUE "G2CFP".
       PROCEDURE DIVISION.
       MAIN.
           CALL "G2CFS1" USING BY REFERENCE B BY CONTENT N
               RETURNING R
               ON OVERFLOW DISPLAY "1 OVF"
           END-CALL
           DISPLAY "1 " B " " N " " R
           MOVE "ABC" TO B
           MOVE 1 TO N
           CALL NM2 USING B N ON OVERFLOW DISPLAY "2 OVF".
           DISPLAY "2 " B " " N
           CALL "G2NOSUCH" ON OVERFLOW DISPLAY "3 OVERFLOW TAKEN"
           END-CALL
           MOVE "ABC" TO B
           MOVE 1 TO N
           MOVE 0 TO R
           CALL NM1 USING B BY CONTENT N RETURNING R
               NOT ON EXCEPTION DISPLAY "4 NOT-EXC " R
               ON EXCEPTION DISPLAY "4 EXC"
           END-CALL
           CALL "G2NOSUCH" ON EXCEPTION DISPLAY "5 EXC TAKEN"
               NOT ON EXCEPTION DISPLAY "5 ?"
           END-CALL
           MOVE "ABC" TO B
           MOVE 0 TO R
           CALL G2CFP USING BY VALUE A * 2 + 1 BY REFERENCE B
               BY CONTENT C B-AND B"0110"
               RETURNING R
               ON EXCEPTION DISPLAY "6 EXC"
               NOT ON EXCEPTION DISPLAY "6 R=" R
           END-CALL
           MOVE 0 TO R
           CALL "G2CFP" AS G2CFP USING BY VALUE 7 OMITTED
               BY CONTENT B"1111" RETURNING R
           DISPLAY "7 R=" R
           MOVE 0 TO R
           CALL NMP AS G2CFP USING BY VALUE 2 BY REFERENCE B
               BY CONTENT C RETURNING R
           END-CALL
           DISPLAY "8 R=" R
           CALL "G2CFN" AS NESTED USING B END-CALL
           MOVE 0 TO R
           CALL G2CFP USING BY VALUE A BY REFERENCE B BY CONTENT C
               RETURNING R
           END-CALL
           DISPLAY "10 R=" R
           CALL G2CFQ USING BY CONTENT A + 4 END-CALL
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CFN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-B PIC X(3).
       PROCEDURE DIVISION USING L-B.
       N1.
           DISPLAY "N " L-B
           GOBACK.
       END PROGRAM G2CFN.
       END PROGRAM G2CFM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CFS1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-B PIC X(3).
       01 L-N PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-B L-N RETURNING L-R.
       S1.
           DISPLAY "S1 " L-B " " L-N
           MOVE "XYZ" TO L-B
           MOVE 9 TO L-N
           MOVE 5 TO L-R
           GOBACK.
       END PROGRAM G2CFS1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CFS2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-B PIC X(3).
       01 L-N PIC 9(4).
       PROCEDURE DIVISION USING L-B L-N.
       S2.
           DISPLAY "S2 " L-B " " L-N
           MOVE "XYZ" TO L-B
           MOVE 9 TO L-N
           GOBACK.
       END PROGRAM G2CFS2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CFP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DA PIC 9(4).
       01 DC PIC X(4).
       LINKAGE SECTION.
       01 L-A PIC S9(4) BINARY.
       01 L-B PIC X(3).
       01 L-C PIC 1(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING BY VALUE L-A
           BY REFERENCE OPTIONAL L-B L-C RETURNING L-R.
       P1.
           IF L-B IS OMITTED
               DISPLAY "P B-OMITTED"
           ELSE
               DISPLAY "P B=" L-B
           END-IF
           MOVE L-A TO DA
           MOVE "????" TO DC
           IF L-C = B"0010" MOVE "0010" TO DC END-IF
           IF L-C = B"1010" MOVE "1010" TO DC END-IF
           IF L-C = B"1111" MOVE "1111" TO DC END-IF
           DISPLAY "P A=" DA " C=" DC
           COMPUTE L-R = L-A * 10
           GOBACK.
       END PROGRAM G2CFP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CFQ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DN PIC 9(4).
       LINKAGE SECTION.
       01 L-N PIC S9(4) BINARY.
       PROCEDURE DIVISION USING L-N.
       Q1.
           MOVE L-N TO DN
           DISPLAY "Q N=" DN
           GOBACK.
       END PROGRAM G2CFQ.
