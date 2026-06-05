      *> ISO 7.3.11 / 7.3.16 — conditional compilation: >>DEFINE + >>IF/>>ELSE/>>END-IF defined-condition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CCTEST.
       PROCEDURE DIVISION.
       MAIN.
>>DEFINE DBG AS 1
>>IF DBG IS DEFINED
           DISPLAY "DEBUG-ON".
>>ELSE
           DISPLAY "DEBUG-OFF".
>>END-IF
           STOP RUN.
