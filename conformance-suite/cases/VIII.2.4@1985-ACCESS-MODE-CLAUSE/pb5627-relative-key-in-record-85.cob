      *>--- conformance-case
      *> id: pb5627-relative-key-in-record-85
      *> rules: [1985-SR-VIII.2.4.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5627. §1985:VIII.2.4.3 SR3 (cite.py: OK):
      *> "Data-name-1 must not be defined in a record description entry
      *> associated with that file-name." RL-RK lies inside RL-REC, the
      *> record of RLF, so the RELATIVE KEY clause is refused under the
      *> 1985 rule, not §2023:12.4.5.13.3 SR3. Fixed form: 85 defines no
      *> free form (kb/Work PB5754).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5627F.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "p699lki.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RL-RK.
       DATA DIVISION.
       FILE SECTION.
       FD RLF.
       01 RL-REC.
          05 RL-RK PIC 9(4).
          05 RL-FILLER PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT RLF
           CLOSE RLF
           STOP RUN.
