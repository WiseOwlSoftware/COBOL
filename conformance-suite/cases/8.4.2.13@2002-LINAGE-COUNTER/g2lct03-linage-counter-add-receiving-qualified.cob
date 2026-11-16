      *>--- conformance-case
      *> id: g2lct03-linage-counter-add-receiving-qualified
      *> rules: [2002-SR-8.4.2.13.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.13.2 SR2 - "The LINAGE-COUNTER identifier shall not
      *> be referenced as a receiving operand." Here it is
      *> the ADD receiving operand, qualified by file-name
      *> (the format admits IN/OF file-name-1, §2002:8.4.2.13.1).
      *> §2002:13.16.32.3 GR7b agrees: "only the input-output control
      *> system may change the value of LINAGE-COUNTER". Everything else
      *> is legal (the same references as sending operands are accepted
      *> in g2lct01), so the refusal is expected on the ADD line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LCT03.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "G2LCT03.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  LPF LINAGE IS 20 LINES.
       01  LREC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT LPF.
           ADD 1 TO LINAGE-COUNTER OF LPF.
           CLOSE LPF.
           STOP RUN.
