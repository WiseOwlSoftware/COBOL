*>--- conformance-case
*> id: pb3246_sort_terminated_after_raising_pickup
*> rules: [GR-14.9.18.4-1, GR-14.9.40.4-17]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3246 - the order of two effects of ONE activation's return.
      *> ISO 14.9.18.4 GR1 b): "If the RAISING phrase is specified, an
      *> exception condition is raised in the activating runtime element if
      *> checking for that exception condition is enabled in the activating
      *> runtime element, and execution continues in that runtime element as
      *> specified in the rules for the activating statement after the
      *> result, if any, of the activated element is returned". ISO 14.9.40.4
      *> GR17: "If a USE procedure invoked while a format 1 SORT statement is
      *> active does not complete normally, the SORT statement is
      *> terminated." The called program B (and the invoked method MB) has a
      *> USE procedure that ends in GOBACK RAISING EXCEPTION
      *> EC-SIZE-OVERFLOW, which both stages the condition for the activator
      *> and does not complete normally; the activator A runs it from the
      *> INPUT PROCEDURE of a format 1 SORT.
      *> EXPECTED (computed from the rules, not from a run): the condition is
      *> raised in A at the CALL (the INVOKE) - "A-CAUGHT EC-SIZE-OVERFLOW",
      *> A's own USE procedure, whose RESUME NEXT STATEMENT completes it
      *> normally - and THEN the SORT is terminated, so the statement after
      *> the CALL (the INVOKE) in the INPUT PROCEDURE never runs and control
      *> continues after the SORT statement. Nothing is staged afterwards:
      *> the next CALL C (the next INVOKE MC) raises nothing in A, so no
      *> second "A-CAUGHT" line appears. Before the fix the termination
      *> left before the pickup and the condition was raised at the NEXT
      *> activation instead (an "A-CAUGHT" line after "IN-C" / "IN-MC").
>>TURN EC-ALL CHECKING ON
IDENTIFICATION DIVISION.
CLASS-ID. PB3246K INHERITS FROM BASE.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY. CLASS BASE CLASS PB3246K.
IDENTIFICATION DIVISION.
OBJECT.
PROCEDURE DIVISION.
IDENTIFICATION DIVISION.
METHOD-ID. MB.
DATA DIVISION.
LOCAL-STORAGE SECTION.
01 Z PIC 9 VALUE 0.
01 X PIC 9 VALUE 0.
PROCEDURE DIVISION.
DECLARATIVES.
DB SECTION.
    USE AFTER EXCEPTION CONDITION EC-SIZE-ZERO-DIVIDE.
DB1.
    DISPLAY "MB-DECL"
    GOBACK RAISING EXCEPTION EC-SIZE-OVERFLOW.
END DECLARATIVES.
BM SECTION.
BM1.
    COMPUTE X = 1 / Z
    DISPLAY "MB-AFTER-COMPUTE (not reached)"
    GOBACK.
END METHOD MB.
IDENTIFICATION DIVISION.
METHOD-ID. MC.
PROCEDURE DIVISION.
    DISPLAY "IN-MC"
    GOBACK.
END METHOD MC.
END OBJECT.
END CLASS PB3246K.

>>TURN EC-ALL CHECKING ON
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3246A.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY. CLASS PB3246K.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SF ASSIGN TO "pb3246.srt".
DATA DIVISION.
FILE SECTION.
SD SF.
01 SR PIC X(4).
WORKING-STORAGE SECTION.
01 O USAGE OBJECT REFERENCE PB3246K.
PROCEDURE DIVISION.
DECLARATIVES.
DA SECTION.
    USE AFTER EXCEPTION CONDITION EC-SIZE-OVERFLOW.
DA1.
    DISPLAY "A-CAUGHT " FUNCTION EXCEPTION-STATUS
    RESUME NEXT STATEMENT.
END DECLARATIVES.
MAIN SECTION.
M1.
    INVOKE PB3246K "NEW" RETURNING O
    SORT SF ON ASCENDING KEY SR INPUT PROCEDURE IP1 OUTPUT PROCEDURE OP
    DISPLAY "AFTER-SORT-1"
    DISPLAY "CALL-C"
    CALL "PB3246C"
    DISPLAY "AFTER-CALL-C"
    SORT SF ON ASCENDING KEY SR INPUT PROCEDURE IP2 OUTPUT PROCEDURE OP
    DISPLAY "AFTER-SORT-2"
    DISPLAY "INVOKE-MC"
    INVOKE O "MC"
    DISPLAY "AFTER-INVOKE-MC"
    STOP RUN.
IP1 SECTION.
IP1A.
    DISPLAY "IP1-CALLING-B"
    CALL "PB3246B"
    DISPLAY "IP1-AFTER-CALL-B (not reached: the SORT is terminated)"
    MOVE "AAAA" TO SR
    RELEASE SR.
IP2 SECTION.
IP2A.
    DISPLAY "IP2-INVOKING-MB"
    INVOKE O "MB"
    DISPLAY "IP2-AFTER-INVOKE-MB (not reached: the SORT is terminated)"
    MOVE "AAAA" TO SR
    RELEASE SR.
OP SECTION.
OP1.
    DISPLAY "OP (not reached)".
END PROGRAM PB3246A.

>>TURN EC-ALL CHECKING ON
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3246B.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 Z PIC 9 VALUE 0.
01 X PIC 9 VALUE 0.
PROCEDURE DIVISION.
DECLARATIVES.
DB SECTION.
    USE AFTER EXCEPTION CONDITION EC-SIZE-ZERO-DIVIDE.
DB1.
    DISPLAY "B-DECL"
    GOBACK RAISING EXCEPTION EC-SIZE-OVERFLOW.
END DECLARATIVES.
BM SECTION.
BM1.
    COMPUTE X = 1 / Z
    DISPLAY "B-AFTER-COMPUTE (not reached)"
    GOBACK.
END PROGRAM PB3246B.

>>TURN EC-ALL CHECKING ON
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3246C.
PROCEDURE DIVISION.
    DISPLAY "IN-C"
    GOBACK.
END PROGRAM PB3246C.
