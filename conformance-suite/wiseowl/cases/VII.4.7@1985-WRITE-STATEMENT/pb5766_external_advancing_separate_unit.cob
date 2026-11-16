      *>--- conformance-case
      *> id: pb5766_external_advancing_separate_unit
      *> rules: [1985-GR-VII.4.7.4-16]
      *> accept: [85]
      *> files: [pb5766_extb.cbl]
      *> build: ["pb5766_extb.cbl > PB5766B", "{self}"]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5766 - the print-file fact of an EXTERNAL file whose
      *> ADVANCING describer is compiled SEPARATELY. §1985:VII.4.7.4
      *> GR16: "If the ADVANCING phrase is not used, automatic
      *> advancing will be provided by the implementor to act as if
      *> the user had specified AFTER ADVANCING 1 LINE". Whether a
      *> file is a print file is WiseOwl's determination
      *> (docs/conformance/4/WRITE-WITHOUT-ADVANCING.md): (b) a WRITE
      *> of it in the compilation group names ADVANCING, or (c) an
      *> ADVANCING write has reached it since the OPEN. PB5766B is
      *> compiled apart and is not activated until the CALL, so this
      *> program's first two plain WRITEs go to a file that is not
      *> yet a print file: bare 4-byte records P1, P2. PB5766B's
      *> AFTER ADVANCING 2 makes it one (two line feeds, A2), so the
      *> plain WRITE of P3 is AFTER ADVANCING 1 (a line feed, P3)
      *> and the CLOSE ends the open line: 20 bytes,
      *>   P1__ P2__ LF LF A2__ LF P3__ LF
      *> read back as ONE 20-byte record of a plain file description
      *> (P5766-READ: neither LINAGE nor ADVANCING, so it is read).
      *> Why each leg can fail: a describer consulted before its
      *> activation would put a line feed before P1 and P2 (a 22-byte
      *> file, READ status 04 and different positions); a fact lost
      *> at the activation would leave P3 bare.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5766A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P5766-FILE ASSIGN TO "pb5766ext.dat".
           SELECT P5766-READ ASSIGN TO "pb5766ext.dat"
               FILE STATUS IS RS.
       DATA DIVISION.
       FILE SECTION.
       FD  P5766-FILE IS EXTERNAL.
       01  P5766-REC PIC X(4).
       FD  P5766-READ.
       01  R20 PIC X(20).
       WORKING-STORAGE SECTION.
       01  RS PIC XX.
       PROCEDURE DIVISION.
       A-PARA.
           OPEN OUTPUT P5766-FILE.
           MOVE "P1" TO P5766-REC. WRITE P5766-REC.
           MOVE "P2" TO P5766-REC. WRITE P5766-REC.
           CALL "PB5766B".
           MOVE "P3" TO P5766-REC. WRITE P5766-REC.
           CLOSE P5766-FILE.
           OPEN INPUT P5766-READ.
           READ P5766-READ AT END DISPLAY "AT END".
           DISPLAY "READ " RS.
           DISPLAY "[" R20 (1:8) "] [" R20 (11:4) "] [" R20 (16:4) "]".
           READ P5766-READ AT END DISPLAY "AT END".
           CLOSE P5766-READ.
           STOP RUN.
