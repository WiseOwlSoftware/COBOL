*>--- conformance-case
*> id: pb3245-national-literal-unterminated-free
*> rules: [FMT-8.3.3.5.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3245 - ISO 8.3.3.5.2 prints a national literal between an
*> opening and a closing quotation symbol, and 6.4.2: "When such a literal
*> is incomplete at the end of a line, the incomplete portion of the
*> literal shall be terminated by a floating continuation indicator". The
*> VALUE literal below ends its line open with no indicator, so it is
*> refused, named by COBOL5563 (it was COBOL0001 at the opening delimiter).
*>   cite.py --check 6.4.2 "When such a literal is incomplete at the end
*>     of a line" -> OK
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3245NAT.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X PIC N(4) VALUE N"AB.
PROCEDURE DIVISION.
    DISPLAY X.
    STOP RUN.
