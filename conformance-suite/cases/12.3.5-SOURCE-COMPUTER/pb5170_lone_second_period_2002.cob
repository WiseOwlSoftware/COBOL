      *>--- conformance-case
      *> id: pb5170_lone_second_period_2002
      *> rules: [SR-12.3.5.3-1, SR-12.3.6.3-4, SR-12.3.7.3-31]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5170 - each configuration paragraph written as its header, its period and a second period with
      *> no entry. §2023:12.3.5.3 SR1, §2023:12.3.6.3 SR4 and §2023:12.3.7.3 SR31 let the second period be omitted when
      *> nothing is written, so writing it is the format's full spelling (the 2002 text's 12.2.4.2 SR1, 12.2.5.2 SR4
      *> and 12.2.6.2 SR27 the same). The program compiles and runs: K.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5170LP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. .
       OBJECT-COMPUTER. .
       SPECIAL-NAMES. .
       PROCEDURE DIVISION.
       M.
           DISPLAY "K".
           STOP RUN.
