      *>--- conformance-case
      *> id: pb5884_computer_name_spelled_as_assign_name
      *> rules: [FMT-12.3.5.2, FMT-12.3.6.2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5884, the later edition. §2023:8.3.2.3.1: "Within
      *> an implementation, a given system-name shall not belong to more
      *> than one of the following types of system-names: device-name,
      *> feature-name, and switch-name." Computer-name is not among
      *> them, so a computer-name spelled as this implementation's ASSIGN
      *> device class DISK (DOC-A.1-71) or its RECORD DELIMITER name
      *> LINE-SEQUENTIAL (DOC-A.1-150) is legal, beside a file control
      *> entry that uses both as the implementor-names they are. At
      *> COBOL-85 the three types of system-name are disjoint and the
      *> same program is refused
      *> (pb5884-computer-name-is-assign-or-delimiter-name-85).
      *> A leg fails if either paragraph is refused, or if the DISPLAY
      *> does not print RAN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5884A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. DISK.
       OBJECT-COMPUTER. LINE-SEQUENTIAL.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO DISK "pb5884a.dat"
               RECORD DELIMITER IS LINE-SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD SQF.
       01 SQ-REC PIC X(5).
       PROCEDURE DIVISION.
       P1.
           DISPLAY "RAN".
           STOP RUN.
