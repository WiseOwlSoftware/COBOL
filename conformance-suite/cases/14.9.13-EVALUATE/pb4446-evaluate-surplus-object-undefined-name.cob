*>--- conformance-case
*> id: pb4446-evaluate-surplus-object-undefined-name
*> rules: [SR-14.9.13.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4446. ISO 14.9.13.3 SR2: "The number of selection objects within each set of selection objects shall be
*> equal to the number of selection subjects." The WHEN phrase writes two objects for one subject (COBOL2106). The
*> surplus object has no subject to pair with, but it is still a reference: 8.4.2.1, "In order to use a resource, a
*> statement shall contain a reference that uniquely identifies that resource", is broken by UNDEFX, which no
*> declaration gives, and 4.2.2 requires the warning mechanism "to indicate violations of the general formats and the
*> explicit syntax rules". Fails if the count breach hides the COBOL1639 about UNDEFX (the pairing loop stopped at the
*> number of subjects and never bound the rest), or if the program is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4446S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 1.
       PROCEDURE DIVISION.
           EVALUATE WS-N
               WHEN 1 ALSO UNDEFX
                   DISPLAY "B"
           END-EVALUATE.
           STOP RUN.
