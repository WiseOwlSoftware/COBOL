*>--- conformance-case
*> id: pb4709_integer_row_argument_order
*> rules: [AR-15.3-6, GR-8.4.3.2.4-2]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB4709 review round 2 (R2-1) - an Integer-row argument written with fraction digits (WS-B * 2.0) reaches the function in its
      *> scale-0 form (ISO 15.2 item 5, 15.3 item 6) and is STILL evaluated in its place in the argument list: ISO 8.4.3.2.4 GR2, "At
      *> the time reference is made to a function, its arguments are evaluated individually in the order specified in the list of
      *> arguments, from left to right" (cite.py --check 8.4.3.2.4 OK). RVBMP adds 1 to the EXTERNAL WS-B and answers twice its formal
      *> (the same storage), so with WS-B = 2 the earlier argument is read BEFORE the activation of RVBMP in the later one:
      *>   SUM(WS-B * 2.0, RVBMP(WS-B))      = 4 + 6 = 10      SUM(WS-B + 1.0, RVBMP(WS-B))     = 3 + 6 = 9
      *>   MAX(WS-B * 2.0, RVBMP(WS-B) - 3)  = MAX(4, 3) = 4   SUM(ABS(WS-B + 1.0), RVBMP(WS-B)) = 3 + 6 = 9
      *> and WS-B is 3 afterwards in every line.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. RVBMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-B PIC 9(15) EXTERNAL.
       LINKAGE SECTION.
       01 L-X PIC 9(15).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           ADD 1 TO WS-B.
           COMPUTE L-R = L-X * 2.
           GOBACK.
       END FUNCTION RVBMP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4709ORD.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION RVBMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-B PIC 9(15) EXTERNAL.
       01 WS-R PIC 9(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 2 TO WS-B.
           COMPUTE WS-R =
               FUNCTION SUM(WS-B * 2.0, FUNCTION RVBMP(WS-B)).
           DISPLAY "MUL=" WS-R " B=" WS-B.
           MOVE 2 TO WS-B.
           COMPUTE WS-R =
               FUNCTION SUM(WS-B + 1.0, FUNCTION RVBMP(WS-B)).
           DISPLAY "ADD=" WS-R " B=" WS-B.
           MOVE 2 TO WS-B.
           COMPUTE WS-R =
               FUNCTION MAX(WS-B * 2.0, FUNCTION RVBMP(WS-B) - 3).
           DISPLAY "MAX=" WS-R " B=" WS-B.
           MOVE 2 TO WS-B.
           COMPUTE WS-R =
               FUNCTION SUM(FUNCTION ABS(WS-B + 1.0),
                   FUNCTION RVBMP(WS-B)).
           DISPLAY "NEST=" WS-R " B=" WS-B.
           STOP RUN.
       END PROGRAM PB4709ORD.
