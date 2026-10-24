*>--- conformance-case
*> id: pb1606_float_sender_exact_value
*> rules: [GR-14.6.8.4-1, GR-14.7.4.3-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1606 - a floating-point data item SENDING into a FLOATING-POINT numeric-edited receiver is read by its
      *> EXACT algebraic value, the manner every float-to-fixed landing uses (docs/CONFORMANCE.md, section 3, kb/Work PB623):
      *> 14.6.8.4 rule 1 adjusts "the algebraic value of the sending operand" and rule 2 truncates by the PICTURE's
      *> editing rules; 14.7.4.3 rule 7: "If the PROHIBITED phrase is specified, and the arithmetic value cannot be
      *> represented exactly in the resultant identifier, the EC-SIZE-TRUNCATION exception condition is set to exist,
      *> the size error condition exists, and the content of the resultant identifier is unchanged."
      *> It used to be read through its shortest decimal image, so a double that is not its decimal spelling passed for it.
      *>
      *> HAND-DERIVED: FE PIC +9.99E+99 holds a three-digit significand. The double nearest 0.1 is
      *> 0.1000000000000000055511151231257827..., which three digits cannot hold exactly: PROHIBITED is the size error
      *> condition and FE keeps +1.50E+00 (a shortest-decimal read called 0.1 exact and stored +1.00E-01). 0.5 is exact:
      *> +5.00E-01. The double nearest 0.29 is 0.28999999999999998001598555674718...: truncated to three digits
      *> +2.89E-01 (no ROUNDED phrase is TRUNCATION, 14.7.4.3 rule 2; a MOVE truncates, 14.6.8.4 rule 2), ROUNDED
      *> +2.90E-01 (nearest, rule 4).
      *> Each leg can fail on its own.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1606F.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 F1   USAGE FLOAT-LONG VALUE 0.1.
01 FH   USAGE FLOAT-LONG VALUE 0.5.
01 F29  USAGE FLOAT-LONG VALUE 0.29.
01 FE   PIC +9.99E+99 VALUE 1.5E0.
PROCEDURE DIVISION.
MAIN.
    COMPUTE FE ROUNDED MODE IS PROHIBITED = F1
        ON SIZE ERROR DISPLAY "PROHIBITED-0.1=SIZE-ERROR"
        NOT ON SIZE ERROR DISPLAY "PROHIBITED-0.1=OK"
    END-COMPUTE
    DISPLAY "UNCHANGED=" FE
    COMPUTE FE ROUNDED MODE IS PROHIBITED = FH
        ON SIZE ERROR DISPLAY "PROHIBITED-0.5=SIZE-ERROR"
        NOT ON SIZE ERROR DISPLAY "PROHIBITED-0.5=OK"
    END-COMPUTE
    DISPLAY "HALF=" FE
    COMPUTE FE = F29
    DISPLAY "TRUNCATED=" FE
    COMPUTE FE ROUNDED = F29
    DISPLAY "ROUNDED=" FE
    MOVE F29 TO FE
    DISPLAY "MOVED=" FE
    STOP RUN.
