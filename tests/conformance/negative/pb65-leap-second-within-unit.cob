      *> reject-at: 2002 2014 2023
      *> ISO 7.3.17.3 SR1: "The LEAP-SECOND directive shall not be specified within a compilation unit." A
      *> >>LEAP-SECOND after the first IDENTIFICATION DIVISION is inside the unit - COBOLNET1650 (kb/Work PB65).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB65NLEAPIN.
       >>LEAP-SECOND ON
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(6).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION SECONDS-FROM-FORMATTED-TIME("hhmmss", "235960").
           STOP RUN.
