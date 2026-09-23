      *> reject-at: 2002 2014 2023
      *> ISO 10.6.2 SR2: "If a compilation group contains both a program
      *> definition and a program prototype definition with the same
      *> externalized name, the signatures of these two compilation units
      *> shall be the same." The prototype's formal is PIC 9(4), the
      *> definition's PIC X(4). kb/Work PB894.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGNS IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-N PIC 9(4).
       PROCEDURE DIVISION USING L-N.
       END PROGRAM PGNS.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGNS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-N PIC X(4).
       PROCEDURE DIVISION USING L-N.
       P-MAIN.
           GOBACK.
       END PROGRAM PGNS.
