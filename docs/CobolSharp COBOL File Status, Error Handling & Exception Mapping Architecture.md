CobolSharp COBOL File Status, Error Handling & Exception Mapping Architecture (CIL‑Only)
=======================================================================================

Purpose
-------
Define the authoritative architecture for:
- COBOL file status codes
- INVALID KEY, AT END, and ON EXCEPTION semantics
- SIZE ERROR and arithmetic exceptions
- JSON/XML exception mapping
- Runtime exception propagation
- CIL‑friendly lowering of exception handlers
- Integration with ExecutionContext and FileManager
- Deterministic error behavior across all COBOL features

This document governs how CobolSharp implements COBOL’s error‑handling model on .NET.

------------------------------------------------------------
SECTION 1 — OVERVIEW OF COBOL ERROR MODEL
------------------------------------------------------------

COBOL has a **multi‑layered exception model**:

1. **File I/O exceptions**  
   - INVALID KEY  
   - AT END  
   - File status codes  

2. **Arithmetic exceptions**  
   - SIZE ERROR  
   - Division by zero  
   - Overflow  

3. **General exceptions**  
   - ON EXCEPTION  

4. **JSON/XML exceptions**  
   - Malformed input  
   - Type mismatch  

5. **Runtime exceptions**  
   - Propagated from .NET if not handled  

CobolSharp implements all of these using:
- ExecutionContext.ExceptionState
- Structured try/catch blocks
- FileManager error codes
- NumericEngine overflow detection
- JsonEngine/XmlEngine exception mapping

------------------------------------------------------------
SECTION 2 — FILE STATUS ARCHITECTURE
------------------------------------------------------------

2.1 File status variable
------------------------
Each file descriptor may specify:
FILE STATUS fs-var

CobolSharp updates fs-var after **every** file operation.

2.2 File status codes
---------------------
CobolSharp implements full ANSI file status semantics:

"00" → Success  
"02" → Duplicate key  
"04" → Short record  
"05" → OPEN in progress  
"07" → CLOSE in progress  
"10" → End of file  
"14" → No current record  
"21" → Key not found  
"22" → Invalid key  
"23" → Record locked  
"30" → Permanent error  
"34" → Boundary violation  
"35" → File not found  
"37" → Permission denied  
"90" → Runtime error  
"91" → Lock conflict  
"92" → Logic error  
"93" → File integrity error  

2.3 Updating file status
------------------------
FileManager sets:
- Primary status code
- Secondary status code (optional)

ExecutionContext writes status into fs-var.

------------------------------------------------------------
SECTION 3 — INVALID KEY SEMANTICS
------------------------------------------------------------

INVALID KEY occurs when:
- READ KEY fails
- REWRITE without prior READ
- DELETE without prior READ
- Duplicate key on WRITE
- Key not found in indexed file
- RRN invalid in relative file

Lowering:
- FileManager returns error code
- CIL checks code
- If INVALID KEY → branch to INVALID KEY handler

INVALID KEY handler:
- Executes user‑defined block
- NOT INVALID KEY executes only if no error

------------------------------------------------------------
SECTION 4 — AT END SEMANTICS
------------------------------------------------------------

AT END occurs when:
- READ NEXT hits EOF
- READ PREVIOUS hits BOF
- READ on empty file

Lowering:
- FileManager returns EOF code
- CIL checks code
- If AT END → branch to handler

AT END handler:
- Executes user‑defined block
- NOT AT END executes only if no EOF

------------------------------------------------------------
SECTION 5 — ON EXCEPTION SEMANTICS
------------------------------------------------------------

ON EXCEPTION applies to:
- CALL
- INVOKE
- JSON PARSE
- JSON GENERATE
- XML PARSE
- XML GENERATE
- STRING/UNSTRING (overflow)
- Arithmetic operations (via SIZE ERROR)
- File I/O (general errors)

Lowering:
- Wrap operation in try/catch
- Catch .NET exceptions
- Set ExceptionState
- Branch to ON EXCEPTION handler

NOT ON EXCEPTION executes only if:
- No exception occurred
- No SIZE ERROR
- No INVALID KEY
- No AT END

------------------------------------------------------------
SECTION 6 — SIZE ERROR SEMANTICS
------------------------------------------------------------

SIZE ERROR occurs when:
- Arithmetic overflow
- Packed decimal overflow
- Division by zero
- Invalid numeric conversion
- Result cannot fit in target field

Lowering:
- NumericEngine returns success flag
- If false → branch to SIZE ERROR handler

SIZE ERROR handler:
- Prevents assignment to target
- Executes user‑defined block

NOT SIZE ERROR executes only if:
- No overflow
- No division by zero
- No invalid conversion

------------------------------------------------------------
SECTION 7 — JSON/XML EXCEPTION MAPPING
------------------------------------------------------------

7.1 JSON PARSE exceptions
-------------------------
ON EXCEPTION triggered by:
- Malformed JSON
- Type mismatch
- Missing required field (if STRICT)
- Invalid numeric conversion
- Array length mismatch

7.2 JSON GENERATE exceptions
----------------------------
Triggered by:
- Invalid COBOL data
- Unrepresentable numeric values
- Invalid UTF‑16 sequences

7.3 XML PARSE exceptions
------------------------
Triggered by:
- Malformed XML
- Invalid nesting
- Encoding errors

7.4 XML GENERATE exceptions
---------------------------
Triggered by:
- Invalid COBOL data
- Invalid attribute names
- Invalid element names

Lowering:
- JsonEngine/XmlEngine throw exceptions
- CIL catches and maps to ON EXCEPTION

------------------------------------------------------------
SECTION 8 — RUNTIME EXCEPTION PROPAGATION
------------------------------------------------------------

8.1 COBOL exceptions → .NET exceptions
--------------------------------------
If COBOL code does not handle:
- SIZE ERROR
- INVALID KEY
- AT END
- ON EXCEPTION

Then:
- CobolSharp throws a .NET exception
- Exception type depends on category

Examples:
SIZE ERROR → ArithmeticException  
INVALID KEY → KeyNotFoundException  
AT END → EndOfStreamException  
JSON error → JsonException  
XML error → XmlException  

8.2 .NET exceptions → COBOL exceptions
--------------------------------------
If a .NET exception occurs inside COBOL code:
- If ON EXCEPTION exists → map to COBOL exception
- Otherwise → propagate as .NET exception

------------------------------------------------------------
SECTION 9 — CIL LOWERING RULES
------------------------------------------------------------

9.1 File I/O lowering
---------------------
READ → try/catch + status check  
WRITE → try/catch + status check  
REWRITE → try/catch + status check  
DELETE → try/catch + status check  
START → try/catch + status check  

9.2 Arithmetic lowering
-----------------------
COMPUTE → NumericEngine.Compute  
ADD/SUB/MULT/DIV → NumericEngine  
Check success flag → branch to SIZE ERROR

9.3 JSON/XML lowering
---------------------
Wrap in try/catch:
- JsonEngine.Parse  
- JsonEngine.Generate  
- XmlEngine.Parse  
- XmlEngine.Generate  

9.4 CALL/INVOKE lowering
------------------------
Wrap in try/catch:
- Catch .NET exceptions
- Map to ON EXCEPTION

------------------------------------------------------------
SECTION 10 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- File status codes
- ExceptionState
- Last exception message
- Last failing operation
- Raw storage for failing fields
- JSON/XML error details
- Numeric overflow details

Sequence points emitted for:
- Exception entry
- Exception exit
- SIZE ERROR checks
- INVALID KEY checks
- AT END checks

------------------------------------------------------------
SECTION 11 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

11.1 INVALID KEY + AT END both possible
---------------------------------------
INVALID KEY takes precedence.

11.2 SIZE ERROR inside ON EXCEPTION
-----------------------------------
Allowed; nested handlers apply.

11.3 JSON null into numeric field
---------------------------------
SIZE ERROR → ON EXCEPTION

11.4 XML empty element into numeric field
-----------------------------------------
SIZE ERROR → ON EXCEPTION

11.5 File status variable missing
---------------------------------
Status still updated internally.

11.6 REWRITE without READ
-------------------------
INVALID KEY

11.7 DELETE on sequential file
------------------------------
Permanent error → "30"

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp File Status, Error Handling & Exception Mapping Architecture:
- Implements full COBOL exception semantics
- Supports INVALID KEY, AT END, SIZE ERROR, and ON EXCEPTION
- Maps JSON/XML and .NET exceptions into COBOL handlers
- Uses structured CIL lowering for predictable behavior
- Integrates deeply with ExecutionContext and FileManager
- Provides deterministic, debuggable error handling across all COBOL features
- Ensures correctness across CoreCLR, AOT, and WASM
