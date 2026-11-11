*>--- conformance-case
*> id: external_relative_key_below
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5069 - the RELATIVE KEY item of an external file connector is an external data
      *> item at every edition (2002:12.3.4.3 GR1 i); 1985:VIII.2.4.4 GR8), so 2014 refuses this
      *> group, whose RELKEY is a local item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XRKCBLBLA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELFILE ASSIGN TO "reldata"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RELKEY.
       DATA DIVISION.
       FILE SECTION.
       FD RELFILE IS EXTERNAL.
       01 RELREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RELKEY IS EXTERNAL PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-A.
           STOP RUN.
       END PROGRAM XRKCBLBLA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XRKCBLB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELFILE ASSIGN TO "reldata"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RELKEY.
       DATA DIVISION.
       FILE SECTION.
       FD RELFILE IS EXTERNAL.
       01 RELREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RELKEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-B.
           GOBACK.
       END PROGRAM XRKCBLB.
