      *>--- conformance-case
      *> id: pb5627-relative-key-signed-85
      *> rules: [1985-SR-VIII.2.4.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5627. §1985:VIII.2.4.3 SR2 (cite.py: OK): data-name-1
      *> "must reference an unsigned integer data item whose description
      *> does not contain the PICTURE symbol 'P'". W-RK is signed, so the
      *> RELATIVE KEY clause is refused under the 1985 rule, not
      *> §2023:12.4.5.13.3 SR2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5627D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5627d.txt"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS W-RK.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  F-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01  W-RK PIC S9(4).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
