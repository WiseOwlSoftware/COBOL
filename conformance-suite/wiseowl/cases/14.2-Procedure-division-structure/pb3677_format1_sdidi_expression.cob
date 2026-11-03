*>--- conformance-case
*> id: pb3677_format1_sdidi_expression
*> rules: [GR-14.2.3-9, GR-14.2.3-10]
*> accept: [2014, 2023]
*> files: [pb3677_en_callee.cbl]
*> build: ["pb3677_en_callee.cbl > PB3677EN", "{self}"]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3677, the arithmetic-expression sibling of pb3677_format1_sdidi_function_value: a STANDARD-DECIMAL
      *> expression is an SDIDI value, and a CALL whose formal the activating element cannot see (here a CALL by program-
      *> prototype name, whose details come from the REPOSITORY entry) cut the value to 6 fraction digits BEFORE the formal's
      *> COMPUTE. ISO 14.2.3 GR9, the second regime (a program for which there is a program-specifier), and GR10 fill a
      *> numeric formal by a COMPUTE that does not round:
      *>   cite.py --check 14.2.3 "if the formal parameter is numeric, a COMPUTE statement without the ROUNDED phrase"
      *>   -> OK  14.2.3 9)  (General rules)
      *> so the formal PIC 9V9(12) takes the value of the expression truncated at ITS twelfth fraction digit, not at the
      *> activating element's sixth. EXPECTED OUTPUT, derived (R = LN * 10 ** 12, the 13 digits of the formal's value):
      *>   BY CONTENT (2 / 3) = 0.666666666666666... in the SDIDI -> truncated at 12 digits 0.666666666666 -> R = 0666666666666
      *>                        (not 0.666667 and not the rounded 0.666666666667; the 6-digit floor gave 0666666000000)
      *>   BY VALUE   (1 / 8) = 0.125 exactly                      -> R = 0125000000000
      *> PB3677EN and PB3677EV (its BY VALUE twin) are compiled apart, into PB3677EN.dll beside this program (the
      *> build step), so the program-specifiers' details come from the external repository (12.3.8.4 GR10 c); kb/Work PB2103:
      *> PB3677EN used to be a program contained in PB3677EM, which no specifier can name - 12.3.8.3 SR14).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3677EM.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PB3677EN
           PROGRAM PB3677EV.
       PROCEDURE DIVISION.
       MAIN.
           CALL PB3677EN USING BY CONTENT (2 / 3)
           CALL PB3677EV USING BY VALUE (1 / 8)
           STOP RUN.
       END PROGRAM PB3677EM.
