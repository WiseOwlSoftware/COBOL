      *>--- conformance-case
      *> id: pb5427-external-record-name-reused-sibling
      *> rules: [1985-SR-IV.4.3.8.1-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5427 - the REACH of the EXTERNAL half of the name rule. §1985:IV.4.3.8.1 SR4 bounds it to the programs
      *>   "contained within or containing" the one that describes the external record, so two programs side by side (SIB5427A
      *>   and SIB5427B, neither containing the other) escape it at --std 85: conformance:pb5427-external-record-name-reused-
      *>   sibling-85 is that program accepted. From COBOL-2002 the text says "described in the run unit" (§2023:8.4.2.1,
      *>   cite.py: OK on "The same name shall not be used both as the name of an external record and as the name of any other
      *>   external data item described in the run unit"), and the compilation group is the part of the run unit a compiler
      *>   sees, so the same two programs are refused there at the subordinate EXR of SIB5427B (line 28).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SIB5427A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  EXR EXTERNAL PIC X.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
       END PROGRAM SIB5427A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SIB5427B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  OTHER-EXT EXTERNAL.
           05  EXR PIC X.
       PROCEDURE DIVISION.
       Q1.
           STOP RUN.
       END PROGRAM SIB5427B.
