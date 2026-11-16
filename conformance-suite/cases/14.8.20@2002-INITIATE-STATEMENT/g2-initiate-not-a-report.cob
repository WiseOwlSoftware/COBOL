      *>--- conformance-case
      *> id: g2-initiate-not-a-report
      *> rules: [2002-SR-14.8.20.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.20.2 SR1 - "Report-name-1 shall be defined by a
      *>   report description entry in the report section." WS-N is a
      *>   working-storage item, not an RD name, so INITIATE WS-N
      *>   violates SR1 and is refused at the INITIATE (COBOL1757).
      *>   The accepting form is g2_initiate_active_state.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INI02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N   PIC 9 VALUE 4.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INITIATE WS-N.
           STOP RUN.
       END PROGRAM G2INI02.
