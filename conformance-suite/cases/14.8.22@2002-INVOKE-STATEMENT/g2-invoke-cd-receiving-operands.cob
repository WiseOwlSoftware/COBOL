      *>--- conformance-case
      *> id: g2-invoke-cd-receiving-operands
      *> rules: [2002-SR-14.8.22.2-2, 2002-SR-14.8.22.2-6,
      *>    2002-SR-14.8.22.2-19, 2002-SR-14.8.22.2-21]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> INVOKE arguments and RETURNING items that are record items
      *> under a 2002 CD entry: §2002:14.8.22.2 SR9 and SR11 admit
      *> "communication section" items as identifier-3 and
      *> identifier-4. The CD facility is obsolete at 2002 (COBOL0903
      *> warning, the case is still accepted).
      *> §2002:14.8.22.2 SR2 - "Literal-1 shall be of class
      *> alphanumeric or national": an alphanumeric literal-1 "SETIT"
      *> (A1-A4, A6) and a national literal-1 N"GETIT" (A5) both name
      *> the method (§2002:14.8.22.3 GR2a).
      *> §2002:14.8.22.2 SR6 - through a universal object reference
      *> "the BY REFERENCE phrase, if not specified explicitly, is
      *> assumed implicitly": A2 (no BY phrase) and A6 (explicit BY
      *> REFERENCE) pass the CD item R2/R6 by reference.
      *> §2002:14.8.22.2 SR19 - "If identifier-3 does not reference an
      *> address-identifier, identifier-3 is a receiving operand": the
      *> CD items R1, R2, R3, R6 passed by reference receive the
      *> method's MOVE (A1, A2, A3, A6).
      *> §2002:14.8.22.2 SR21 - "Identifier-4 is a receiving operand":
      *> the CD item R5 receives GETIT's result (§2002:14.8.22.3 GR8).
      *> By reference "the activated runtime element operates as if the
      *> formal parameter occupies the same storage area as the
      *> argument" (§2002:14.1.3 GR8), so SETIT's MOVE "ZZZZ" TO P is
      *> seen in the caller's item.
      *> EXPECTED:
      *>   A1=[ZZZZ] typed O, no BY phrase: R1 meets SR9, the formal is
      *>             BY REFERENCE, so BY REFERENCE is assumed
      *>             (§2002:14.8.22.3 GR6a1)
      *>   A2=[ZZZZ] universal U, no BY phrase: BY REFERENCE (SR6)
      *>   A3=[ZZZZ] typed O, BY REFERENCE R3 (SR19)
      *>   A4=[CCCC] typed O, BY CONTENT R4: the control - a copy is
      *>             passed, the CD item is unchanged
      *>   A5=[RRRR] RETURNING R5 through N"GETIT" (SR2, SR21)
      *>   A6=[ZZZZ] universal U, BY REFERENCE R6 (SR6)
      *> Why each leg can fail: a CD item refused as an argument or
      *> RETURNING item, a universal argument passed by content (A2
      *> would stay BBBB), a national literal-1 not resolved to the
      *> method name, or a result not stored in the CD record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INV01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2INV01C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O  USAGE OBJECT REFERENCE G2INV01C.
       01 U  USAGE OBJECT REFERENCE.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT
           TEXT LENGTH IS OTL.
       01 OUTREC.
          05 R1 PIC X(4).
          05 R2 PIC X(4).
          05 R3 PIC X(4).
          05 R4 PIC X(4).
          05 R5 PIC X(4).
          05 R6 PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE G2INV01C "NEW" RETURNING O.
           SET U TO O.
           MOVE "AAAA" TO R1.
           INVOKE O "SETIT" USING R1.
           DISPLAY "A1=[" R1 "]".
           MOVE "BBBB" TO R2.
           INVOKE U "SETIT" USING R2.
           DISPLAY "A2=[" R2 "]".
           MOVE "DDDD" TO R3.
           INVOKE O "SETIT" USING BY REFERENCE R3.
           DISPLAY "A3=[" R3 "]".
           MOVE "CCCC" TO R4.
           INVOKE O "SETIT" USING BY CONTENT R4.
           DISPLAY "A4=[" R4 "]".
           MOVE SPACES TO R5.
           INVOKE O N"GETIT" RETURNING R5.
           DISPLAY "A5=[" R5 "]".
           MOVE "EEEE" TO R6.
           INVOKE U "SETIT" USING BY REFERENCE R6.
           DISPLAY "A6=[" R6 "]".
           STOP RUN.
       END PROGRAM G2INV01.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2INV01C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SETIT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC X(4).
       PROCEDURE DIVISION USING P.
           MOVE "ZZZZ" TO P.
       END METHOD SETIT.
       METHOD-ID. GETIT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RV PIC X(4).
       PROCEDURE DIVISION RETURNING RV.
           MOVE "RRRR" TO RV.
       END METHOD GETIT.
       END OBJECT.
       END CLASS G2INV01C.
