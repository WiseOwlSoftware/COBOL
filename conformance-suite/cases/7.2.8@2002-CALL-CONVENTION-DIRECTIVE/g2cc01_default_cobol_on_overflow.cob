      *>--- conformance-case
      *> id: g2cc01_default_cobol_on_overflow
      *> rules: [2002-GR-7.2.8.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.8.2 GR1 - "The default for the CALL-CONVENTION
      *>   directive is '>>CALL-CONVENTION COBOL'." No >>CALL-CONVENTION
      *>   is written here, so every CALL, CANCEL and
      *>   program-address-identifier below is under the COBOL
      *>   convention: "that program-name or method-name is treated as
      *>   a COBOL word that maps to the externalized name"
      *>   (§2002:7.2.8.2 GR2a; §2002:8.3.1.1.1). Its basic letters are
      *>   then case-insensitive: "each uppercase COBOL basic letter is
      *>   equivalent to its corresponding lowercase COBOL basic letter"
      *>   (§2002:8.1.2.1 GR3a), so every spelling of G2CC01S below names
      *>   the one program G2CC01S.
      *> The 2002-only reach: every CALL is format 1 (on-overflow),
      *>   CALL ... ON OVERFLOW, a format 2023 removed.
      *> EXPECTED OUTPUT, DERIVED (G2CC01S counts its activations in
      *> WORKING-STORAGE, kept between CALLs until a CANCEL):
      *>   SUB-COUNT=1  CALL "g2cc01s": located, first activation; "after
      *>                control is returned from the called program the
      *>                ON OVERFLOW or ON EXCEPTION phrase, if specified,
      *>                is ignored" (§2002:14.8.4.3 GR3).
      *>   SUB-COUNT=2  CALL "G2Cc01S": the same program, state kept.
      *>   SUB-COUNT=1  CANCEL "g2CC01s" cancels that same program, so
      *>                CALL "G2CC01S" finds it in its initial state
      *>                (§2002:14.8.5.3 GR3).
      *>   SUB-COUNT=2  SET ... TO ADDRESS OF PROGRAM "g2cc01S" names the
      *>                same program (GR2 covers program-address-
      *>                identifiers); CALL through the pointer.
      *>   OVF-NOT-FOUND  CALL "G2CC01NONE" names no program of the run
      *>                unit; with "an ON OVERFLOW or ON EXCEPTION phrase
      *>                ... specified in the CALL statement, control is
      *>                transferred to imperative-statement-1"
      *>                (§2002:14.8.4.3 GR3g): the ON OVERFLOW leg is live.
      *> How each leg can fail: a case-sensitive (literal) mapping takes
      *> ON OVERFLOW on a mixed spelling (OVF-BAD-n), or leaves the CANCEL
      *> a no-op (SUB-COUNT=3), or the pointer names no program.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CC01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-PTR USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "g2cc01s"
               ON OVERFLOW DISPLAY "OVF-BAD-1"
           END-CALL
           CALL "G2Cc01S"
               ON OVERFLOW DISPLAY "OVF-BAD-2"
           END-CALL
           CANCEL "g2CC01s"
           CALL "G2CC01S"
               ON OVERFLOW DISPLAY "OVF-BAD-3"
           END-CALL
           SET W-PTR TO ADDRESS OF PROGRAM "g2cc01S"
           CALL W-PTR
               ON OVERFLOW DISPLAY "OVF-BAD-4"
           END-CALL
           CALL "G2CC01NONE"
               ON OVERFLOW DISPLAY "OVF-NOT-FOUND"
           END-CALL
           STOP RUN.
       END PROGRAM G2CC01.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CC01S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-COUNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           ADD 1 TO W-COUNT
           DISPLAY "SUB-COUNT=" W-COUNT
           GOBACK.
       END PROGRAM G2CC01S.
