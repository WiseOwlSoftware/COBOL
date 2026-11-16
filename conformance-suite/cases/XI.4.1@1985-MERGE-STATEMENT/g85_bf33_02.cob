      *>--- conformance-case
      *> id: g85_bf33_02
      *> rules: [1985-GR-XI.4.1.4-2, 1985-GR-XI.4.1.4-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.1.4 GR2 - a short record of file-name-2 or
      *> file-name-3 is space filled on the right when released to a
      *> fixed length SD (lines M-, MERGE ... OUTPUT PROCEDURE).
      *> §1985:XI.4.1.4 GR12 - a short SD record is space filled on the
      *> right when returned to a fixed length GIVING file (lines G-).
      *> The SD of the second MERGE is variable length, so each record
      *> keeps the size it was read with (§1985:XI.4.1.4 GR7) and is
      *> short until the GIVING transfer fills it. The USING files are
      *> RECORD IS VARYING ... DEPENDING ON, each WRITE sized by W-LEN
      *> (§1985:VII.3.8.4 GR10; §1985:XI.3.2.4 GR2 for the SD), and
      *> RECORD CONTAINS 6 CHARACTERS is fixed (§1985:VII.3.8.4 GR3).
      *> The Q filler past each DEPENDING ON size is never written.
      *> The M leg moves ALL "X" to the SD record area before each
      *> RETURN, so a RETURN that does not deliver a space filled
      *> 6-character record shows X (or low-values) after it. The G
      *> leg fails if a short GIVING record is not space filled: the
      *> fixed length readback then shifts the next record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3302.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT M1 ASSIGN TO "bf3302a.dat".
           SELECT M2 ASSIGN TO "bf3302b.dat".
           SELECT GFX ASSIGN TO "bf3302g.dat".
           SELECT SMF ASSIGN TO "bf3302f.tmp".
           SELECT SMV ASSIGN TO "bf3302v.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD  M1
           RECORD IS VARYING IN SIZE FROM 2 TO 6 CHARACTERS
           DEPENDING ON W-LEN.
       01  M1-REC PIC X(6).
       FD  M2
           RECORD IS VARYING IN SIZE FROM 2 TO 6 CHARACTERS
           DEPENDING ON W-LEN.
       01  M2-REC PIC X(6).
       FD  GFX
           RECORD CONTAINS 6 CHARACTERS.
       01  GFX-REC PIC X(6).
       SD  SMF
           RECORD CONTAINS 6 CHARACTERS.
       01  SMF-REC.
           05  SMF-K PIC X.
           05  SMF-T PIC X(5).
       SD  SMV
           RECORD IS VARYING IN SIZE FROM 2 TO 6 CHARACTERS.
       01  SMV-REC.
           05  SMV-K PIC X.
           05  SMV-T PIC X(5).
       WORKING-STORAGE SECTION.
       01  W-LEN PIC 9.
       01  W-EOF PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT M1.
           MOVE "A1QQQQ" TO M1-REC.
           MOVE 2 TO W-LEN.
           WRITE M1-REC.
           MOVE "C123QQ" TO M1-REC.
           MOVE 4 TO W-LEN.
           WRITE M1-REC.
           CLOSE M1.
           OPEN OUTPUT M2.
           MOVE "B12QQQ" TO M2-REC.
           MOVE 3 TO W-LEN.
           WRITE M2-REC.
           MOVE "D12345" TO M2-REC.
           MOVE 6 TO W-LEN.
           WRITE M2-REC.
           CLOSE M2.
           MERGE SMF ON ASCENDING KEY SMF-K
               USING M1 M2
               OUTPUT PROCEDURE IS SHOW-M.
           MERGE SMV ON ASCENDING KEY SMV-K
               USING M1 M2
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
       SHOW-M.
           MOVE "N" TO W-EOF.
           PERFORM UNTIL W-EOF = "Y"
               MOVE ALL "X" TO SMF-REC
               RETURN SMF
                   AT END MOVE "Y" TO W-EOF
                   NOT AT END DISPLAY "M-[" SMF-REC "]"
               END-RETURN
           END-PERFORM.
