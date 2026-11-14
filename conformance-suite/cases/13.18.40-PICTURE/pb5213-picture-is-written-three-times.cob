      *>--- conformance-case
      *> id: pb5213-picture-is-written-three-times
      *> rules: [FMT-13.18.40.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5213 - ISO 13.18.40.2 Format 1 prints PICTURE IS
      *> character-string-1 (cite.py --check "§2023:13.18.40.2"
      *> "IS character-string-1 LOCALE" OK). After the one optional IS
      *> the second IS is character-string-1, and the third IS and
      *> X(4) follow a complete PICTURE clause where no clause of the
      *> entry begins, so the entry is refused at every edition at X,
      *> the first word no format of the entry admits there. It fails
      *> when the lexer skips every IS before the string: the program
      *> then compiled as PIC X(4) and displayed WXYZ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5213THREE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC IS IS IS X(4) VALUE "WXYZ".
       PROCEDURE DIVISION.
       P1.
           DISPLAY A.
           STOP RUN.
