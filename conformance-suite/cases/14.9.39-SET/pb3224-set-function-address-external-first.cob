*>--- conformance-case
*> id: pb3224-set-function-address-external-first
*> rules: [SR-14.9.39.3-20]
*> source-format: free
*> portable: unverified
*>---
*> ISO §14.9.39.3 SR20 over a receiving LIST (kb/Work PB3224). identifier-12 is written `{ identifier-12 } ...`
*> and the last sentence applies to EACH receiver:
*>   "The function-prototypes associated with identifier-12 and identifier-13 shall have the same signature."
*>   cite.py: OK  §14.9.39.3 20)  (Syntax rules)
*> FPE is restricted to EXTF, a function-prototype the REPOSITORY names but this compilation group does not
*> define (no compile-time signature, §12.3.8.4 GR11 c), and is written FIRST; FPD is restricted to NSDBL (one USING
*> formal). §8.4.3.12.4 GR3 makes ADDRESS OF FUNCTION NSZERO a function-pointer restricted to NSZERO (none), whose
*> signature differs from NSDBL's. The same statement with FPD written first was always refused, so the verdict
*> must not depend on the order: COBOLNET1959 (prototype-pointer-signature).
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
FUNCTION-ID. NSZERO.
DATA DIVISION.
LINKAGE SECTION.
01 L-RES PIC S9(9).
PROCEDURE DIVISION RETURNING L-RES.
    MOVE 0 TO L-RES
    GOBACK.
END FUNCTION NSZERO.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3224N4.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    FUNCTION NSDBL
    FUNCTION NSZERO
    FUNCTION EXTF.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 FPD USAGE FUNCTION-POINTER TO NSDBL.
01 FPE USAGE FUNCTION-POINTER TO EXTF.
PROCEDURE DIVISION.
MAIN.
    SET FPE FPD TO ADDRESS OF FUNCTION NSZERO
    STOP RUN.
END PROGRAM PB3224N4.
