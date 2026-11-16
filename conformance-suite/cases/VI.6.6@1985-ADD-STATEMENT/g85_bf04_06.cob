      *>--- conformance-case
      *> id: g85_bf04_06
      *> rules: [1985-SR-VI.6.6.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.6.3 SR4 - CORR is an abbreviation for CORRESPONDING
      * ADD CORR and ADD CORRESPONDING must be accepted at 85 and do the
      * same thing (format 3, VI.6.6.4 GR3): A and B pair (both
      * elementary numeric); Z does not (G1's Z is alphanumeric,
      * VI.6.4.3 GR2) and stays 7. CORR: A=10+1=11, B=20+2=22.
      * CORRESPONDING then adds again: A=21, B=42. A compiler that
      * refused CORR fails to translate; one that read CORR as anything
      * else answers other numbers.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0406.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
           05 A PIC 9(3) VALUE 10.
           05 B PIC 9(3) VALUE 20.
           05 Z PIC X(3) VALUE "ZZZ".
       01 G2.
           05 A PIC 9(3) VALUE 1.
           05 B PIC 9(3) VALUE 2.
           05 Z PIC 9(3) VALUE 7.
       PROCEDURE DIVISION.
       P1.
           ADD CORR G1 TO G2.
           DISPLAY "CORR A=" A OF G2 " B=" B OF G2 " Z=" Z OF G2.
           ADD CORRESPONDING G1 TO G2.
           DISPLAY "CORRESPONDING A=" A OF G2 " B=" B OF G2
               " Z=" Z OF G2.
           STOP RUN.
