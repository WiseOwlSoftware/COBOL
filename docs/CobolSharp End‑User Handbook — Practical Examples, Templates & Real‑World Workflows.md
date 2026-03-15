CobolSharp End‑User Handbook — Practical Examples, Templates & Real‑World Workflows (CIL‑Only)
==============================================================================================

Purpose
-------
Provide a practical, example‑driven handbook for CobolSharp end‑users:
- Real‑world COBOL examples
- Templates for common tasks
- File I/O workflows
- JSON/XML workflows
- SORT/MERGE workflows
- REPORT WRITER workflows
- CALL/ENTRY multi‑module workflows
- OO patterns and .NET interop examples
- Debugging workflows
- Testing workflows
- AOT/WASM deployment workflows

This document is the hands‑on, example‑rich companion to the architecture series.

------------------------------------------------------------
SECTION 1 — BASIC PROGRAM TEMPLATE
------------------------------------------------------------

1.1 Minimal program
-------------------
       IDENTIFICATION DIVISION.
       PROGRAM-ID. HELLO.

       PROCEDURE DIVISION.
           DISPLAY "HELLO, WORLD".
           GOBACK.

1.2 Program with USING and RETURNING
------------------------------------
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ADDER.

       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC S9(9) COMP-5.
       01 B PIC S9(9) COMP-5.

       PROCEDURE DIVISION USING A B RETURNING RESULT.
       01 RESULT PIC S9(9) COMP-5.
           ADD A TO B GIVING RESULT.
           GOBACK.

------------------------------------------------------------
SECTION 2 — FILE I/O WORKFLOWS
------------------------------------------------------------

2.1 Sequential READ loop
------------------------
       READ MYFILE
           AT END MOVE "Y" TO EOF-FLAG
       END-READ.

       PERFORM UNTIL EOF-FLAG = "Y"
           * process record
           READ MYFILE
               AT END MOVE "Y" TO EOF-FLAG
           END-READ
       END-PERFORM.

2.2 Indexed READ with START
---------------------------
       MOVE KEY-VALUE TO MY-KEY.
       START MYFILE KEY >= MY-KEY
           INVALID KEY DISPLAY "NOT FOUND"
       END-START.

       READ MYFILE NEXT RECORD
           AT END MOVE "Y" TO EOF-FLAG
       END-READ.

------------------------------------------------------------
SECTION 3 — JSON WORKFLOWS
------------------------------------------------------------

3.1 JSON GENERATE
-----------------
       JSON GENERATE OUT-JSON
           FROM CUSTOMER-RECORD
           SUPPRESS NULLS.

3.2 JSON PARSE
--------------
       JSON PARSE IN-JSON
           INTO CUSTOMER-RECORD
           ON EXCEPTION
               DISPLAY "BAD JSON"
           END-JSON.

------------------------------------------------------------
SECTION 4 — XML WORKFLOWS
------------------------------------------------------------

4.1 XML GENERATE
----------------
       XML GENERATE OUT-XML
           FROM ORDER-RECORD
           WITH ATTRIBUTES.

4.2 XML PARSE
-------------
       XML PARSE IN-XML
           PROCESSING PROCEDURE XML-HANDLER.

------------------------------------------------------------
SECTION 5 — SORT/MERGE WORKFLOWS
------------------------------------------------------------

5.1 SORT with USING/GIVING
--------------------------
       SORT WORK-FILE
           ON ASCENDING KEY LAST-NAME
           USING INPUT-FILE
           GIVING OUTPUT-FILE.

5.2 SORT with INPUT/OUTPUT procedures
-------------------------------------
       SORT WORK-FILE
           ON ASCENDING KEY ID
           INPUT PROCEDURE LOAD-RECS
           OUTPUT PROCEDURE WRITE-RECS.

------------------------------------------------------------
SECTION 6 — REPORT WRITER WORKFLOWS
------------------------------------------------------------

6.1 Basic report
----------------
       RD SALES-REPORT
           CONTROLS ARE REGION.

       01 DETAIL-LINE TYPE DETAIL.
           05 COL 1  PIC X(20) SOURCE CUSTOMER-NAME.
           05 COL 25 PIC 9(9)  SOURCE SALES-AMOUNT.

       PROCEDURE DIVISION.
           INITIATE SALES-REPORT.
           PERFORM PROCESS-RECORDS.
           TERMINATE SALES-REPORT.

------------------------------------------------------------
SECTION 7 — MULTI‑MODULE CALL WORKFLOWS
------------------------------------------------------------

7.1 CALL with BY REFERENCE
--------------------------
       CALL "CALC" USING A B C.

7.2 CALL with BY CONTENT
------------------------
       CALL "FORMAT" USING BY CONTENT TEMP-STRING.

7.3 CALL with RETURNING
-----------------------
       CALL "ADDER" USING X Y RETURNING RESULT.

------------------------------------------------------------
SECTION 8 — OO WORKFLOWS
------------------------------------------------------------

8.1 Creating an object
----------------------
       INVOKE CLASS MYCLASS "NEW" RETURNING OBJ.

8.2 Calling a method
--------------------
       INVOKE OBJ "Compute" USING A B RETURNING RESULT.

8.3 Using .NET interop
----------------------
       INVOKE TYPE System.Math::Sqrt
           USING VALUE X
           RETURNING ROOT.

------------------------------------------------------------
SECTION 9 — STRING & NUMERIC WORKFLOWS
------------------------------------------------------------

9.1 STRING
----------
       STRING FIRST-NAME " " LAST-NAME
           INTO FULL-NAME.

9.2 UNSTRING
------------
       UNSTRING FULL-NAME
           DELIMITED BY SPACE
           INTO FIRST-NAME LAST-NAME.

9.3 Numeric DISPLAY → COMP‑3
----------------------------
       MOVE DISPLAY-NUM TO PACKED-NUM.

------------------------------------------------------------
SECTION 10 — DEBUGGING WORKFLOWS
------------------------------------------------------------

10.1 Breakpoint strategy
------------------------
- Break on paragraph entry  
- Break on declaratives  
- Break on key statements  

10.2 Inspecting StorageBlocks
-----------------------------
- Raw bytes  
- Decoded fields  
- OCCURS tables  
- REDEFINES overlays  

10.3 Inspecting ExecutionContext
--------------------------------
- PERFORM stack  
- CALL stack  
- ExceptionState  

------------------------------------------------------------
SECTION 11 — TESTING WORKFLOWS
------------------------------------------------------------

11.1 Unit test pattern
----------------------
- Arrange StorageBlocks  
- Execute paragraph  
- Assert field values  

11.2 Snapshot test pattern
--------------------------
- Capture StorageBlock dump  
- Compare to golden file  

11.3 Cross‑target test pattern
------------------------------
- Run on CoreCLR  
- Run on AOT  
- Run on WASM  
- Compare outputs  

------------------------------------------------------------
SECTION 12 — AOT/WASM DEPLOYMENT WORKFLOWS
------------------------------------------------------------

12.1 AOT deployment
-------------------
- Build with net8.0‑aot  
- Publish native binary  
- Deploy with ProgramRegistry  

12.2 WASM deployment
--------------------
- Build with net8.0‑wasm  
- Publish bundle  
- Deploy index.html + runtime.js + dotnet.wasm  

------------------------------------------------------------
SECTION 13 — REAL‑WORLD WORKFLOW EXAMPLES
------------------------------------------------------------

13.1 Customer import workflow
-----------------------------
1. READ CSV  
2. PARSE JSON fields  
3. VALIDATE  
4. SORT  
5. WRITE indexed file  
6. GENERATE report  

13.2 Financial batch workflow
-----------------------------
1. READ transactions  
2. APPLY business rules  
3. ACCUMULATE totals  
4. SORT by account  
5. REPORT WRITER output  

13.3 Web‑service integration workflow
-------------------------------------
1. INVOKE .NET HttpClient wrapper  
2. JSON PARSE response  
3. UPDATE indexed file  
4. REPORT errors  

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp End‑User Handbook:
- Provides practical, real‑world examples for every subsystem
- Offers templates for file I/O, JSON/XML, SORT, REPORT, OO, and interop
- Includes debugging, testing, and deployment workflows
- Completes the example‑driven layer of the CobolSharp ecosystem
