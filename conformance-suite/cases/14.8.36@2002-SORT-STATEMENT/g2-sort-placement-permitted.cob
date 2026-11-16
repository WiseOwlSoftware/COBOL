      *>--- conformance-case
      *> id: g2-sort-placement-permitted
      *> rules: [2002-SR-14.8.36.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
       >>TURN EC-USER-G2S1 EC-SORT-MERGE-ACTIVE CHECKING ON
      *> §2002:14.8.36.2 SR1 - "A SORT statement may appear anywhere
      *> in the procedure division except that a format 1 SORT
      *> statement shall not appear in the declarative portion."
      *> The granted placements are exercised here:
      *>  . a format 2 (table) SORT written in a declarative procedure
      *>    (D1-P) - only format 1 is barred from the declaratives;
      *>  . a format 1 (file) SORT written inside another SORT's input
      *>    procedure (IN-P) and output procedure (OUT-P). SR1 bars
      *>    only the declaratives; 2002 bars EXECUTING a file SORT in
      *>    the range of an input/output procedure (§2002:14.8.36.3
      *>    GR9, GR12: EC-SORT-MERGE-ACTIVE at run time), so those
      *>    SORTs are guarded by a condition that is never true.
      *> (The refusal of a file SORT in the declaratives is
      *> conformance-suite/cases/14.9.40-SORT/
      *> pb1139-sort-in-declarative,
      *> refused at 2002.)
      *> EXPECTED, derived from the 2002 rules:
      *>  DECL ABC  - RAISE sets the non-fatal EC-USER-G2S1 (checking
      *>    on), D1 runs (§2002:14.5.12.1.3); its SORT E ASCENDING has
      *>    no data-name-1, so each element is its own key
      *>    (§2002:14.8.36.3 GR22) and "CAB" is stored ascending
      *>    (GR18 a)) as "ABC".
      *>  AFTER ABC - D1 completes normally, execution continues after
      *>    the RAISE (§2002:14.5.12.1.3).
      *>  OUT=A, OUT=B - the file SORT returns B,A ascending (GR7 a));
      *>    the guarded SORTs never execute.
      *>  DONE.
      *> Each leg fails if the compiler refuses the table SORT in a
      *> declarative or a file SORT written in a SORT procedure (a
      *> 2023-style placement refusal), or if the sorts misorder.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRT03.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "g2srt01w.tmp".
           SELECT SW2 ASSIGN TO "g2srt01v.tmp".
           SELECT F1 ASSIGN TO "g2srt01a.dat".
           SELECT F3 ASSIGN TO "g2srt01b.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR PIC X.
       SD SW2.
       01 SR2 PIC X.
       FD F1.
       01 R1 PIC X.
       FD F3.
       01 R3 PIC X.
       WORKING-STORAGE SECTION.
       01 T VALUE "CAB".
          05 E PIC X OCCURS 3.
       01 NEVER PIC X VALUE "N".
       01 EOFF PIC X.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-G2S1.
       D1-P.
           SORT E ASCENDING.
           DISPLAY "DECL " T.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           RAISE EXCEPTION EC-USER-G2S1.
           DISPLAY "AFTER " T.
           SORT SW ASCENDING KEY SR
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "DONE".
           STOP RUN.
       IN-P SECTION.
       IN-1.
           MOVE "B" TO SR.
           RELEASE SR.
           MOVE "A" TO SR.
           RELEASE SR.
           IF NEVER = "Y"
               SORT SW2 ASCENDING KEY SR2 USING F1 GIVING F3
           END-IF.
       OUT-P SECTION.
       OUT-1.
           MOVE "N" TO EOFF.
           PERFORM UNTIL EOFF = "Y"
               RETURN SW AT END MOVE "Y" TO EOFF
                   NOT AT END DISPLAY "OUT=" SR
               END-RETURN
           END-PERFORM.
           IF NEVER = "Y"
               SORT SW2 DESCENDING KEY SR2 USING F1 GIVING F3
           END-IF.
