CobolSharp COBOL JSON PARSE / JSON GENERATE Architecture (CIL‑Only)
===================================================================

Purpose
-------
Define the authoritative architecture for:
- JSON PARSE semantics
- JSON GENERATE semantics
- Mapping COBOL data items ↔ JSON values
- Handling of NATIONAL and DISPLAY strings
- Handling of numeric, boolean, and null values
- OCCURS and nested group mapping
- Exception routing (ON EXCEPTION, SIZE ERROR)
- Integration with ExecutionContext.JsonEngine
- CIL‑friendly lowering
- AOT/WASM‑safe JSON processing

This document governs how CobolSharp implements COBOL’s JSON facilities on .NET.

------------------------------------------------------------
SECTION 1 — JSON ENGINE OVERVIEW
------------------------------------------------------------

CobolSharp implements:
- Full ISO/IEC 1989:2023 JSON PARSE and JSON GENERATE
- Deterministic mapping rules
- Unicode‑safe string handling
- Strict type validation
- Declarative exception routing
- AOT/WASM‑safe JSON processing (no dynamic codegen)

JSON operations are handled by:
ExecutionContext.JsonEngine

Which provides:
- Parse(jsonString, targetRecord)
- Generate(sourceRecord) → jsonString
- Exception metadata

------------------------------------------------------------
SECTION 2 — JSON PARSE SEMANTICS
------------------------------------------------------------

2.1 Basic form
--------------
JSON PARSE json-text INTO data-item

2.2 WITH DETAIL
---------------
JSON PARSE json-text
    INTO data-item
    WITH DETAIL name-value-pairs

CobolSharp supports:
- INTO only
- INTO + WITH DETAIL

2.3 ON EXCEPTION
----------------
Triggered by:
- Invalid JSON
- Type mismatch
- Missing required field
- Numeric overflow
- Invalid UTF‑8/UTF‑16 conversion

2.4 NOT ON EXCEPTION
--------------------
Executed only if parse succeeds.

------------------------------------------------------------
SECTION 3 — JSON → COBOL MAPPING RULES
------------------------------------------------------------

3.1 JSON object → COBOL group
-----------------------------
Each JSON property maps to:
- A child data item with matching name
- Case‑insensitive match
- Hyphens and underscores normalized

3.2 JSON array → OCCURS
------------------------
JSON array maps to:
- OCCURS table
- Logical length = array length
- Must not exceed max OCCURS

3.3 JSON string → PIC X / PIC N
-------------------------------
- UTF‑8 → UTF‑16 conversion
- Truncated if longer than target
- Padded with spaces if shorter

3.4 JSON number → numeric PIC
-----------------------------
- Decimal conversion
- Overflow triggers ON EXCEPTION
- COMP‑3 and COMP‑5 supported

3.5 JSON boolean → PIC X or PIC 9
----------------------------------
- TRUE → "TRUE" or 1
- FALSE → "FALSE" or 0

3.6 JSON null
-------------
Mapped as:
- Spaces for PIC X/N
- Zero for numeric
- False for condition names

------------------------------------------------------------
SECTION 4 — COBOL → JSON MAPPING RULES
------------------------------------------------------------

4.1 Group item → JSON object
----------------------------
Each child becomes:
"field-name": value

4.2 OCCURS → JSON array
------------------------
Each element becomes:
[ element1, element2, ... ]

4.3 PIC X / PIC N → JSON string
-------------------------------
- Trim trailing spaces
- Convert UTF‑16 → UTF‑8

4.4 Numeric → JSON number
-------------------------
- Decimal → JSON number
- COMP‑3 unpacked
- COMP‑5 converted to integer

4.5 Condition names (88‑levels)
-------------------------------
- TRUE → true
- FALSE → false

4.6 REDEFINES
-------------
Only the active field is emitted.

------------------------------------------------------------
SECTION 5 — WITH DETAIL SEMANTICS
------------------------------------------------------------

5.1 Purpose
-----------
Captures:
- JSON property names
- JSON property values
- JSON types

5.2 Detail item structure
-------------------------
Each detail entry contains:
- Name
- Type (string, number, boolean, null, object, array)
- Value (string or numeric)
- Depth (optional)

5.3 Lowering
------------
JsonEngine emits:
- A table of detail entries
- Stored in target detail group

------------------------------------------------------------
SECTION 6 — ERROR HANDLING
------------------------------------------------------------

6.1 ON EXCEPTION triggers on:
-----------------------------
- Invalid JSON syntax
- Missing required fields
- Type mismatch
- Numeric overflow
- Array too large for OCCURS
- Invalid UTF‑8/UTF‑16
- Unexpected JSON type

6.2 ExceptionState contents
---------------------------
- Category = JSON EXCEPTION
- Error message
- Property name (if applicable)
- Expected type
- Actual type

6.3 Declarative routing
-----------------------
If no ON EXCEPTION:
- USE AFTER EXCEPTION
- USE AFTER ERROR
- USE AFTER STANDARD EXCEPTION

------------------------------------------------------------
SECTION 7 — CIL LOWERING RULES
------------------------------------------------------------

7.1 JSON PARSE lowering
-----------------------
Generated IL:
- Load json-text
- Load target record pointer
- Call ctx.JsonEngine.Parse
- Check ExceptionState
- Branch to ON EXCEPTION or NOT ON EXCEPTION

7.2 JSON GENERATE lowering
--------------------------
Generated IL:
- Load source record pointer
- Call ctx.JsonEngine.Generate
- Store result into target

7.3 WITH DETAIL lowering
------------------------
JsonEngine populates:
- Detail OCCURS table
- Name/value/type fields

------------------------------------------------------------
SECTION 8 — JSON ENGINE IMPLEMENTATION
------------------------------------------------------------

8.1 Parser
----------
- UTF‑8 decoder
- Recursive descent parser
- No dynamic codegen
- AOT/WASM‑safe

8.2 Generator
-------------
- UTF‑16 → UTF‑8 encoder
- Minimal escaping
- Deterministic field ordering (declaration order)

8.3 Performance
---------------
- Zero allocations for numeric conversion
- Pooled buffers
- Streaming parser for large JSON

------------------------------------------------------------
SECTION 9 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Parsed JSON tree
- Detail entries (if WITH DETAIL)
- ExceptionState
- Mapped COBOL fields
- OCCURS logical length

------------------------------------------------------------
SECTION 10 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

10.1 JSON array too large
-------------------------
ON EXCEPTION

10.2 JSON string too long
-------------------------
Truncated; no exception unless STRICT mode enabled

10.3 Numeric overflow
---------------------
ON EXCEPTION

10.4 JSON null into numeric
---------------------------
Value = 0

10.5 JSON boolean into PIC X
----------------------------
"TRUE" or "FALSE"

10.6 JSON object missing fields
-------------------------------
Missing fields left unchanged

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp JSON Architecture:
- Implements full COBOL JSON PARSE and JSON GENERATE semantics
- Provides deterministic mapping between COBOL records and JSON
- Supports OCCURS, NATIONAL, numeric, boolean, and null values
- Integrates tightly with ExecutionContext.JsonEngine
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
