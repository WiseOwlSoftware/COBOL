      *>--- conformance-case
      *> id: g2call04_default_entry_convention
      *> rules: [2002-GR-14.8.4.3-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.4.3 GR10 -- "Control is transferred to the called
      *> program in a manner consistent with the entry convention
      *> specified for the program." No program here has an
      *> ENTRY-CONVENTION clause; §2002:11.8.5.3 GR2: "The entry
      *> convention used when this clause is not specified is
      *> implementor-defined", except that naming follows 8.3.1.1.1.
      *> WiseOwl's one convention is COBOL (docs/conformance DOC-A.1-64;
      *> any entry-convention-name-1 is refused, COBOL2385, case
      *> pb232-entry-convention-name), its name mapping case-insensitive
      *> (DOC-A.1-68). §2002:8.3.1.1.1: a CALL that "specifies a
      *> program-prototype-name" takes "the naming convention and
      *> mapping used for the program-name" from "the entry convention
      *> of the called program"; §2002:14.8.4.3 GR3: otherwise
      *> "program-prototype-name-1 determines the externalized
      *> program-name of the program being called".
      *> The callee adds 1 to its BY REFERENCE formal and returns ten
      *> times the result.
      *>   CALL G2C4S USING WN RETURNING WR, WN=5  -> P1 WN=0006 WR=0060
      *>   CALL "g2c4s" AS G2C4S (lower case literal, the convention's
      *>   case-insensitive mapping reaches G2C4S) -> P2 WN=0007 WR=0070
      *> Fails if: control or the arguments do not cross the default
      *> convention (EC-PROGRAM-NOT-FOUND -> NF line, or wrong values).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2C4S IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-N PIC 9(4).
       01 LK-R PIC 9(4).
       PROCEDURE DIVISION USING LK-N RETURNING LK-R.
       END PROGRAM G2C4S.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2C4M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM G2C4S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WN PIC 9(4) VALUE 5.
       01 WR PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           CALL G2C4S USING WN RETURNING WR
               ON EXCEPTION DISPLAY "NF"
           END-CALL
           DISPLAY "P1 WN=" WN " WR=" WR
           CALL "g2c4s" AS G2C4S USING WN RETURNING WR
               ON EXCEPTION DISPLAY "NF"
           END-CALL
           DISPLAY "P2 WN=" WN " WR=" WR
           STOP RUN.
       END PROGRAM G2C4M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2C4S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-N PIC 9(4).
       01 LK-R PIC 9(4).
       PROCEDURE DIVISION USING LK-N RETURNING LK-R.
       S1.
           ADD 1 TO LK-N
           COMPUTE LK-R = LK-N * 10
           GOBACK.
       END PROGRAM G2C4S.
