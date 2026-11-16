      *>--- conformance-case
      *> id: g85_bf33_01
      *> rules: [1985-GR-XI.4.4.4-1, 1985-GR-XI.4.4.4-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.4.4 GR1 - a short USING record is space filled on
      *> the right when released to a fixed length SD (lines U-).
      *> §1985:XI.4.4.4 GR13 - a short SD record is space filled on the
      *> right when returned to a fixed length GIVING file (lines G-).
      *> Short records come from RECORD IS VARYING ... DEPENDING ON:
      *> §1985:VII.3.8.4 GR10 a. sizes each WRITE and RELEASE by the
      *> DEPENDING ON item (§1985:XI.3.2.4 GR2 makes the SD RECORD
      *> clause that of Sequential I-O). §1985:VII.3.8.4 GR3: RECORD
      *> CONTAINS 6 CHARACTERS is fixed length. The Q, Z and Y filler
      *> past each DEPENDING ON size is never written or released.
      *> The U leg moves ALL "X" to the SD record area before each
      *> RETURN, so a RETURN that does not deliver a space filled
      *> 6-character record shows X (or low-values) after it. The G
      *> leg fails if a short GIVING record is not space filled: the
      *> fixed length readback then shifts the next record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3301.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT VIN ASSIGN TO "bf3301v.dat".
           SELECT GFX ASSIGN TO "bf3301g.dat".
           SELECT SFX ASSIGN TO "bf3301f.tmp".
           SELECT SVR ASSIGN TO "bf3301s.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD  VIN
           RECORD IS VARYING IN SIZE FROM 2 TO 6 CHARACTERS
           DEPENDING ON W-LEN.
       01  VIN-REC PIC X(6).
       FD  GFX
           RECORD CONTAINS 6 CHARACTERS.
       01  GFX-REC PIC X(6).
       SD  SFX
           RECORD CONTAINS 6 CHARACTERS.
       01  SFX-REC.
           05  SFX-K PIC X.
           05  SFX-T PIC X(5).
       SD  SVR
           RECORD IS VARYING IN SIZE FROM 2 TO 6 CHARACTERS
           DEPENDING ON W-SLEN.
       01  SVR-REC.
           05  SVR-K PIC X.
           05  SVR-T PIC X(5).
       WORKING-STORAGE SECTION.
       01  W-LEN PIC 9.
       01  W-SLEN PIC 9.
       01  W-EOF PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT VIN.
           MOVE "B123QQ" TO VIN-REC.
           MOVE 4 TO W-LEN.
           WRITE VIN-REC.
           MOVE "A1QQQQ" TO VIN-REC.
           MOVE 2 TO W-LEN.
           WRITE VIN-REC.
           MOVE "C12345" TO VIN-REC.
           MOVE 6 TO W-LEN.
           WRITE VIN-REC.
           CLOSE VIN.
           SORT SFX ON ASCENDING KEY SFX-K
               USING VIN
               OUTPUT PROCEDURE IS SHOW-U.
           SORT SVR ON ASCENDING KEY SVR-K
               INPUT PROCEDURE IS REL-V
               GIVING GFX.
           OPEN INPUT GFX.
           MOVE "N" TO W-EOF.
           PERFORM UNTIL W-EOF = "Y"
               READ GFX
                   AT END MOVE "Y" TO W-EOF
                   NOT AT END DISPLAY "G-[" GFX-REC "]"
               END-READ
           END-PERFORM.
           CLOSE GFX.
           STOP RUN.
       SHOW-U.
           MOVE "N" TO W-EOF.
           PERFORM UNTIL W-EOF = "Y"
               MOVE ALL "X" TO SFX-REC
               RETURN SFX
                   AT END MOVE "Y" TO W-EOF
                   NOT AT END DISPLAY "U-[" SFX-REC "]"
               END-RETURN
           END-PERFORM.
       REL-V.
           MOVE "B9ZZZZ" TO SVR-REC.
           MOVE 3 TO W-SLEN.
           RELEASE SVR-REC.
           MOVE "A7YYYY" TO SVR-REC.
           MOVE 2 TO W-SLEN.
           RELEASE SVR-REC.
           MOVE "C65432" TO SVR-REC.
           MOVE 6 TO W-SLEN.
           RELEASE SVR-REC.
