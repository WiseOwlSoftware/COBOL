*>--- conformance-case
*> id: pb3665-inspect-identifier1-integer-function
*> rules: [SR-14.9.22.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3665 (review round 1, R1-1) - ISO 14.9.22.3 SR1: "Identifier-1 shall reference either an
*> alphanumeric or national group item or an elementary item described implicitly or explicitly as usage
*> display or national." A Format 1 (TALLYING only) INSPECT admits a function-identifier as identifier-1
*> (8.4.3.1.2), the data item its 15.2 type describes (8.4.3.2.1), and 15.2 5) states no usage for an integer
*> function, so FUNCTION INTEGER(N) is not described as usage display or national. The statement compiled
*> clean and tallied the digits of the value. Same sentence, same reader as identifier-3 ... identifier-n
*> (pb3665-inspect-integer-function-operand). Unchanged at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3665I1I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 7.
       01 T PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           INSPECT FUNCTION INTEGER(N) TALLYING T FOR ALL "7"
           STOP RUN.
