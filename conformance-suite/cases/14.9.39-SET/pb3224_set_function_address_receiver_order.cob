*>--- conformance-case
*> id: pb3224_set_function_address_receiver_order
*> rules: [SR-14.9.39.3-20, GR-8.4.3.12.4-3]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
*> ISO §14.9.39.3 SR20 over a receiving LIST - identifier-12 is written `{ identifier-12 } ...`, so the last
*> sentence applies to each receiver and WHERE one stands decides nothing:
*>   "The function-prototypes associated with identifier-12 and identifier-13 shall have the same signature."
*>   cite.py: OK  §14.9.39.3 20)  (Syntax rules)
*> GR3: "When function-prototype-name-1 is specified, the function-address-identifier has the characteristics
*>   of a function-pointer restricted to function-prototype-name-1."   cite.py: OK  §8.4.3.12.4 3)
*> FPE is restricted to EXTF, which this compilation group does not define (no compile-time signature,
*> §12.3.8.4 GR11 c)), and FPD to NSDBL. The sender ADDRESS OF FUNCTION NSDBL is restricted to NSDBL, so it
*> meets SR20 for FPD, and EXTF's signature is the external repository's to state, so the compiler cannot find
*> it different. The statement is accepted with FPE written first and with FPD written first; the refusal
*> twin is pb3224-set-function-address-external-first, which differs only in the sender (NSZERO).
*> EXPECTED OUTPUT, derived line by line:
*>   FPE-SET FPD-SET   after `SET FPE FPD TO ADDRESS OF FUNCTION NSDBL` (§14.9.39.4 GR14 stores the address in
*>                     each receiver) neither pointer is NULL. (EXTF is not in the group, so the stored address
*>                     is the one the run unit's registry holds for NSDBL, which is not NULL.)
*>   FPD-SET2 FPE-SET2 the same after the receivers are NULLed and written in the other order.
IDENTIFICATION DIVISION.
FUNCTION-ID. NSDBL.
DATA DIVISION.
LINKAGE SECTION.
01 L-ARG PIC S9(4).
01 L-RES PIC S9(9).
PROCEDURE DIVISION USING L-ARG RETURNING L-RES.
    COMPUTE L-RES = L-ARG * 2
    GOBACK.
END FUNCTION NSDBL.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3224FM.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    FUNCTION NSDBL
    FUNCTION EXTF.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 FPD USAGE FUNCTION-POINTER TO NSDBL.
01 FPE USAGE FUNCTION-POINTER TO EXTF.
PROCEDURE DIVISION.
MAIN.
    SET FPE FPD TO ADDRESS OF FUNCTION NSDBL
    IF FPE NOT = NULL DISPLAY "FPE-SET" END-IF
    IF FPD NOT = NULL DISPLAY "FPD-SET" END-IF
    SET FPE FPD TO NULL
    SET FPD FPE TO ADDRESS OF FUNCTION NSDBL
    IF FPD NOT = NULL DISPLAY "FPD-SET2" END-IF
    IF FPE NOT = NULL DISPLAY "FPE-SET2" END-IF
    STOP RUN.
END PROGRAM PB3224FM.
