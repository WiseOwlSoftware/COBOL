      *>--- conformance-case
      *> id: pb5607-string-pointer-binary-container
      *> rules: [SR-14.9.43.3-7]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.43.3 SR7: the pointer shall be "of sufficient size to
      *> contain a value equal to 1 plus the size of the data item
      *> referenced". R is 200 characters, so the pointer must hold 201.
      *> A COMP-5 item with PIC 99 owns its whole binary container,
      *> not its two PICTURE digits, so it holds 201 and is accepted:
      *> COMP-5 is not an ISO usage, so this is the implementor's
      *> choice recorded in docs/conformance/7/DOC-A.1-208.md (kb/Work
      *> R10), agreeing with GnuCOBOL 3.2.0. An unsigned BINARY-CHAR
      *> (one byte, 255) is accepted by 13.18.60.4 GR12 ("the
      *> implementor may allow a wider range"; kb/Work PB5607,
      *> PB6073). STRING moves ABC to positions 1-3, pointer 4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5607E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(200) VALUE SPACES.
       01 P PIC 99 COMP-5 VALUE 1.
       01 Q BINARY-CHAR UNSIGNED VALUE 1.
       01 D1 PIC 99.
       01 D2 PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STRING "ABC" DELIMITED BY SIZE INTO R WITH POINTER P.
           STRING "ABC" DELIMITED BY SIZE INTO R WITH POINTER Q.
           MOVE P TO D1.
           MOVE Q TO D2.
           DISPLAY D1 " " D2.
           STOP RUN.
