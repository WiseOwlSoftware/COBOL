      *>--- conformance-case
      *> id: pb5884-computer-name-is-assign-or-delimiter-name-85
      *> rules: [1985-SR-VI.4.3.3-1, 1985-SR-VI.4.4.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5884. "Computer-name is a system-name."
      *> (§1985:VI.4.3.3 SR1, §1985:VI.4.4.3 SR1), and of the three
      *> types of system-names, computer-name, implementor-name and
      *> language-name, §1985:IV.4.2.2.1.2 says: "Within a given
      *> implementation these three types of system-names form disjoint
      *> sets; a given system-name may belong to one and only one of
      *> them." DISK is this implementation's ASSIGN device class
      *> (DOC-A.1-71) and LINE-SEQUENTIAL its RECORD DELIMITER name
      *> (DOC-A.1-150), both implementor-names, so neither is a
      *> computer-name at 85 and each paragraph is refused with
      *> COBOL7624 at its name; the file control entry uses both as the
      *> implementor-names they are. From 2002 the same words are
      *> computer-names (pb5884_computer_name_spelled_as_assign_name).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5884R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. DISK.
       OBJECT-COMPUTER. LINE-SEQUENTIAL.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO DISK "pb5884r.dat"
               RECORD DELIMITER IS LINE-SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD SQF.
       01 SQ-REC PIC X(5).
       PROCEDURE DIVISION.
       P1.
           DISPLAY "RAN".
           STOP RUN.
