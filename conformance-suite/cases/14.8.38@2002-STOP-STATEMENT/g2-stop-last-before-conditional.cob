      *>--- conformance-case
      *> id: g2-stop-last-before-conditional
      *> rules: [2002-SR-14.8.38.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.38.2 SR1 - "If a STOP statement appears in a
      *> consecutive sequence of imperative statements within a
      *> sentence, it shall appear as the last statement in that
      *> sequence." The accepted shape: STOP RUN followed in the same
      *> sentence by a CONDITIONAL statement. §2002:14.4: "Any
      *> statement with a conditional phrase that is not terminated by
      *> its explicit scope terminator is a conditional statement", so
      *> the IF with no END-IF is not imperative and STOP RUN is the
      *> last statement of its imperative sequence. (The refusal - STOP
      *> RUN followed by an imperative - is
      *> conformance-suite/cases/14.9.42-STOP/pb397-stop-not-last,
      *> refused COBOL2968 at 2002.)
      *> EXPECTED: BEFORE; STOP RUN then ends the run unit
      *> (§2002:14.8.38.3 GR6), so MUST-NOT never prints. Fails if the
      *> compiler over-refuses this sentence.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2STP01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "BEFORE"
           STOP RUN
           IF N = 1 DISPLAY "MUST-NOT".
