CobolSharp COBOL JSON PARSE/GENERATE, SAX‑Style Event Model & Data Binding Architecture (CIL‑Only)
=================================================================================================

Purpose
-------
Define the authoritative architecture for:
- JSON PARSE statement
- JSON GENERATE statement
- SAX‑style event model
- Data binding to COBOL storage
- WITH DETAIL semantics
- NAME OF / SUPPRESS / OMITTED
- Exception routing (ON EXCEPTION, declaratives)
- UTF‑8/UTF‑16 conversion
- Integration with ExecutionContext.JsonEngine
- AOT/WASM‑safe JSON processing
- CIL‑friendly lowering

This document governs how CobolSharp implements COBOL’s JSON facilities on .NET.

------------------------------------------------------------
SECTION 1 — JSON ENGINE OVERVIEW
------------------------------------------------------------

CobolSharp implements JSON using:
- A streaming SAX‑style parser
- A streaming generator
- Zero‑allocation UTF‑8 processing
- Deterministic event ordering
- Strict COBOL type binding

ExecutionContext.JsonEngine provides:
- Parse(input, handler)
- Generate(output, handler)
- UTF‑8/UTF‑16 conversion
- Error detection and ExceptionState population

------------------------------------------------------------
SECTION 2 — JSON PARSE STATEMENT
------------------------------------------------------------

2.1 Basic form
--------------
JSON PARSE jsonText
    INTO dataItem
    WITH DETAIL
    ON EXCEPTION
        ...
    NOT ON EXCEPTION
        ...

2.2 Modes
---------
CobolSharp supports:
- INTO dataItem (object binding)
- WITH DETAIL (event‑level binding)
- NAME OF (custom field names)
- SUPPRESS (omit fields)
- OMITTED (skip missing fields)

2.3 Input encoding
------------------
Input may be:
- DISPLAY (ASCII/UTF‑8)
- NATIONAL (UTF‑16)
- FD record buffer (raw bytes)

------------------------------------------------------------
SECTION 3 — SAX‑STYLE EVENT MODEL
------------------------------------------------------------

3.1 Events
----------
JsonEngine emits events:
- StartObject
- EndObject
- StartArray
- EndArray
- Key(name)
- Value(string/number/boolean/null)

3.2 Handler
-----------
Compiler generates a handler class with:
- OnStartObject
- OnEndObject
- OnStartArray
- OnEndArray
- OnKey
- OnValue

3.3 WITH DETAIL
---------------
WITH DETAIL causes:
- Each event to populate DETAIL‑item
- DETAIL‑item is a group with:
  - EVENT‑TYPE
  - NAME
  - VALUE
  - DEPTH
  - INDEX

------------------------------------------------------------
SECTION 4 — DATA BINDING MODEL
------------------------------------------------------------

4.1 INTO dataItem
-----------------
Maps JSON object to COBOL group:
- Keys matched to field names
- Case‑insensitive
- Missing fields → default
- Extra fields → ignored unless WITH DETAIL

4.2 Arrays
----------
JSON arrays map to:
- OCCURS tables
- OCCURS DEPENDING ON supported
- Bounds checked

4.3 Scalars
-----------
JSON numbers → Decimal  
JSON strings → DISPLAY or NATIONAL  
JSON booleans → "TRUE"/"FALSE"  
JSON null → spaces/zeros

4.4 Nested objects
------------------
Mapped recursively to nested groups.

4.5 NAME OF
-----------
Overrides field name:
01 Customer.
   05 CustName PIC X(20) NAME OF "name".

4.6 SUPPRESS
------------
Field not generated during JSON GENERATE.

4.7 OMITTED
-----------
Field not required during JSON PARSE.

------------------------------------------------------------
SECTION 5 — JSON GENERATE STATEMENT
------------------------------------------------------------

5.1 Basic form
--------------
JSON GENERATE jsonText
    FROM dataItem
    WITH DETAIL
    ON EXCEPTION
        ...

5.2 Output encoding
-------------------
Output is:
- UTF‑8 for DISPLAY
- UTF‑16 for NATIONAL

5.3 Field rules
---------------
DISPLAY → JSON string  
NATIONAL → JSON string  
COMP/COMP‑3/COMP‑5 → JSON number  
Group → JSON object  
OCCURS → JSON array

5.4 SUPPRESS
------------
Field omitted from output.

5.5 NAME OF
-----------
Custom key name used.

5.6 WITH DETAIL
---------------
Emits DETAIL events for each generated element.

------------------------------------------------------------
SECTION 6 — ERROR HANDLING & EXCEPTIONSTATE
------------------------------------------------------------

6.1 Parse errors
----------------
- Invalid JSON syntax
- Unexpected token
- UTF‑8/UTF‑16 errors
- Numeric overflow
- Array bounds overflow

6.2 Generate errors
-------------------
- Invalid field type
- Non‑ASCII DISPLAY in UTF‑8 mode
- OCCURS DEPENDING ON out of range

6.3 ExceptionState
------------------
Populated with:
- Error category
- Error message
- JSON path
- Event type
- Raw token (optional)

6.4 Routing
-----------
1. ON EXCEPTION  
2. USE AFTER EXCEPTION ON JSON  
3. USE AFTER STANDARD EXCEPTION  

------------------------------------------------------------
SECTION 7 — UTF‑8 / UTF‑16 CONVERSION
------------------------------------------------------------

7.1 UTF‑8 input
---------------
- Parsed directly
- No intermediate string allocation

7.2 UTF‑16 input
----------------
- Converted to UTF‑8 stream
- Surrogate pairs validated

7.3 Output
----------
DISPLAY → UTF‑8  
NATIONAL → UTF‑16

------------------------------------------------------------
SECTION 8 — CIL LOWERING RULES
------------------------------------------------------------

8.1 JSON PARSE lowering
-----------------------
Generated IL:
- Load input buffer
- newobj JsonParseHandler
- call JsonEngine.Parse

8.2 JSON GENERATE lowering
--------------------------
Generated IL:
- newobj JsonGenerateHandler
- call JsonEngine.Generate
- Store output into target

8.3 WITH DETAIL lowering
------------------------
Compiler generates:
- DETAIL group
- Handler writes event info into DETAIL group
- DETAIL group emitted per event

8.4 NAME OF lowering
--------------------
Compiler embeds:
- Custom key name in metadata

8.5 SUPPRESS lowering
---------------------
Compiler marks field as suppressed in metadata.

------------------------------------------------------------
SECTION 9 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Current JSON event
- Key name
- Value
- Depth
- Index
- DETAIL group contents
- ExceptionState
- Bound COBOL fields

------------------------------------------------------------
SECTION 10 — AOT/WASM‑SAFE JSON PROCESSING
------------------------------------------------------------

10.1 No reflection
------------------
Handlers generated statically.

10.2 No dynamic codegen
-----------------------
Parser and generator use:
- Pure managed code
- No dynamic IL

10.3 No unsafe code
-------------------
- No pointers
- No stackalloc

10.4 Deterministic behavior
---------------------------
- Same output across platforms
- No locale‑dependent formatting

------------------------------------------------------------
SECTION 11 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

11.1 JSON null
--------------
Mapped to:
- Spaces (alphanumeric)
- Zero (numeric)

11.2 Empty arrays
-----------------
OCCURS DEPENDING ON = 0.

11.3 Missing keys
-----------------
If OMITTED → allowed  
Else → default value

11.4 Extra keys
---------------
Ignored unless WITH DETAIL.

11.5 Numeric overflow
---------------------
SIZE ERROR → ON EXCEPTION.

11.6 Invalid UTF‑8
------------------
ON EXCEPTION.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp JSON Architecture:
- Implements full COBOL JSON PARSE/GENERATE semantics
- Provides SAX‑style event model and structured data binding
- Supports NAME OF, SUPPRESS, OMITTED, and WITH DETAIL
- Ensures deterministic UTF‑8/UTF‑16 processing
- Integrates tightly with ExecutionContext.JsonEngine
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
