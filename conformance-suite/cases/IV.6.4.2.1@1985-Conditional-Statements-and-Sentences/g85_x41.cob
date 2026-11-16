      *>--- conformance-case
      *> id: g85_x41
      *> rules: [1985-GR-IV.6.4.2.1.1-7, 1985-GR-IV.6.4.2.1.1.1-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1 GR7 (1985-GR-IV.6.4.2.1.1-7) and
      *> §1985:IV.6.4.2.1.1.1 GR6 (1985-GR-IV.6.4.2.1.1.1-6). An output
      *> test, the accepted half: STRING and UNSTRING, each with ON
      *> OVERFLOW and with NOT ON OVERFLOW and delimited by END-STRING
      *> / END-UNSTRING, stand as imperative-statement-1 of an inline
      *> PERFORM. Derived: ABCD into 2 positions overflows before the
      *> third character (§1985:VI.6.25.4 GR9), into 4 positions does
      *> not; UNSTRING without DELIMITED BY examines as many characters
      *> as the receiving area holds, so one 2-character receiver
      *> leaves CD unexamined (§1985:VI.6.27.4 GR13b; overflow, GR17b)
      *> and two receivers examine all four.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85X41.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S1 PIC X(4) VALUE "ABCD".
       01  S2 PIC X(2) VALUE SPACES.
       01  S4 PIC X(4) VALUE SPACES.
       01  U1 PIC X(2).
       01  U2 PIC X(2).
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       P0.
           PERFORM
               STRING S1 DELIMITED BY SIZE INTO S2
                   ON OVERFLOW
                       DISPLAY "STRING OVERFLOW " S2
               END-STRING
               STRING S1 DELIMITED BY SIZE INTO S4
                   NOT ON OVERFLOW
                       DISPLAY "STRING NOT OVERFLOW " S4
               END-STRING
               UNSTRING S1 INTO U1
                   ON OVERFLOW
                       DISPLAY "UNSTRING OVERFLOW " U1
               END-UNSTRING
               UNSTRING S1 INTO U1 U2
                   NOT ON OVERFLOW
                       DISPLAY "UNSTRING NOT OVERFLOW " U1 U2
               END-UNSTRING
           END-PERFORM.
           DISPLAY "END".
           STOP RUN.
