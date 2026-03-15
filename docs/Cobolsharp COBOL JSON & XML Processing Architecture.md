CobolSharp COBOL JSON & XML Processing Architecture (CIL‑Only)
=============================================================

Purpose
-------
Define the authoritative architecture for:
- JSON PARSE / JSON GENERATE
- XML PARSE / XML GENERATE
- Mapping COBOL records ↔ JSON/XML structures
- COUNT IN, PROCESSING PROCEDURE, and ON EXCEPTION semantics
- OCCURS and nested group handling
- National vs alphanumeric encoding
- CIL‑friendly lowering
- Integration with CobolSharp.Runtime.JsonEngine and XmlEngine

This document governs how CobolSharp implements COBOL’s structured‑data features on .NET.

------------------------------------------------------------
SECTION 1 — OVERVIEW OF JSON/XML SUPPORT
------------------------------------------------------------

CobolSharp implements:
- Full JSON PARSE and JSON GENERATE (COBOL‑2014+)
- Full XML PARSE and XML GENERATE (COBOL‑2014+)
- Deterministic mapping between COBOL data structures and JSON/XML
- Runtime engines for parsing, serialization, and validation
- CIL‑only lowering with no external dependencies

JSON/XML engines are:
- Pure managed code
- Deterministic
- Unicode‑safe
- AOT/WASM compatible

------------------------------------------------------------
SECTION 2 — JSON PARSE SEMANTICS
------------------------------------------------------------

2.1 Basic form
--------------
JSON PARSE json‑source  
    INTO cobol‑record  
    WITH DETAIL  
    ON EXCEPTION handler  
    NOT ON EXCEPTION handler  
END‑JSON

2.2 Mapping rules
-----------------
JSON object → COBOL group  
JSON array → COBOL OCCURS  
JSON string → PIC X or PIC N  
JSON number → numeric item  
JSON boolean → condition name or PIC X(…)  
JSON null → spaces / zero depending on type  

2.3 WITH DETAIL
---------------
WITH DETAIL:
- Preserves nested structure
- Maps JSON arrays to OCCURS
- Maps nested objects to nested groups

Without WITH DETAIL:
- Flattens structure (not recommended)

2.4 COUNT IN
------------
COUNT IN var:
- Stores number of JSON elements processed
- For arrays: number of array elements
- For objects: number of fields

2.5 PROCESSING PROCEDURE
------------------------
PROCESSING PROCEDURE proc:
- Called for each JSON element
- Receives:
  - Element name
  - Element value
  - Element type
  - Current OCCURS index

2.6 ON EXCEPTION
----------------
Triggered by:
- Malformed JSON
- Type mismatch
- Missing required fields
- Overflow in target fields

------------------------------------------------------------
SECTION 3 — JSON GENERATE SEMANTICS
------------------------------------------------------------

3.1 Basic form
--------------
JSON GENERATE json‑target  
    FROM cobol‑record  
    WITH DETAIL  
    ON EXCEPTION handler  
END‑JSON

3.2 Mapping rules
-----------------
COBOL group → JSON object  
OCCURS → JSON array  
PIC X → JSON string  
PIC N → JSON string (UTF‑16)  
Numeric → JSON number  
88‑levels → JSON boolean  
Empty group → {}  

3.3 WITH DETAIL
---------------
WITH DETAIL:
- Includes all nested groups
- Includes OCCURS arrays
- Includes REDEFINES only for active view

3.4 OMITTED fields
------------------
If a field is OMITTED:
- Not included in JSON output

------------------------------------------------------------
SECTION 4 — XML PARSE SEMANTICS
------------------------------------------------------------

4.1 Basic form
--------------
XML PARSE xml‑source  
    PROCESSING PROCEDURE proc  
    ON EXCEPTION handler  
END‑XML

4.2 Event‑driven model
----------------------
XmlEngine uses a SAX‑like model:
- START‑ELEMENT event
- END‑ELEMENT event
- TEXT event

PROCESSING PROCEDURE receives:
- Event type
- Element name
- Attributes
- Text content

4.3 COUNT IN
------------
COUNT IN var:
- Number of XML nodes processed

4.4 ON EXCEPTION
----------------
Triggered by:
- Malformed XML
- Invalid nesting
- Encoding errors

------------------------------------------------------------
SECTION 5 — XML GENERATE SEMANTICS
------------------------------------------------------------

5.1 Basic form
--------------
XML GENERATE xml‑target  
    FROM cobol‑record  
    WITH ATTRIBUTES  
    ON EXCEPTION handler  
END‑XML

5.2 Mapping rules
-----------------
COBOL group → XML element  
Elementary item → child element  
OCCURS → repeated elements  
88‑levels → attributes or elements (configurable)  
PIC X → text node  
PIC N → text node (UTF‑16)  

5.3 WITH ATTRIBUTES
-------------------
Maps:
- 88‑levels to attributes
- VALUE clauses to attributes
- REDEFINES ignored unless active

------------------------------------------------------------
SECTION 6 — RUNTIME ENGINE ARCHITECTURE
------------------------------------------------------------

6.1 JsonEngine
--------------
Responsibilities:
- Parse JSON text
- Validate structure
- Map JSON → COBOL record
- Map COBOL record → JSON
- Handle OCCURS and nested groups
- Handle COUNT IN and PROCESSING PROCEDURE
- Detect ON EXCEPTION conditions

6.2 XmlEngine
-------------
Responsibilities:
- Parse XML text
- Emit XML text
- Handle SAX‑style events
- Map COBOL record → XML
- Handle COUNT IN and PROCESSING PROCEDURE
- Detect ON EXCEPTION conditions

6.3 Encoding rules
------------------
- All JSON strings are UTF‑8
- All XML text is UTF‑8 or UTF‑16 depending on target
- PIC N always uses UTF‑16 internally

------------------------------------------------------------
SECTION 7 — CIL LOWERING RULES
------------------------------------------------------------

7.1 JSON PARSE lowering
-----------------------
Lowered to:
JsonEngine.Parse(ctx, jsonSource, targetRecord, options)

7.2 JSON GENERATE lowering
--------------------------
Lowered to:
JsonEngine.Generate(ctx, targetString, sourceRecord, options)

7.3 XML PARSE lowering
----------------------
Lowered to:
XmlEngine.Parse(ctx, xmlSource, processingProc, options)

7.4 XML GENERATE lowering
-------------------------
Lowered to:
XmlEngine.Generate(ctx, xmlTarget, sourceRecord, options)

7.5 PROCESSING PROCEDURE lowering
---------------------------------
Lowered to:
- A generated CIL method
- Called by JsonEngine/XmlEngine
- Receives event metadata

------------------------------------------------------------
SECTION 8 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- JSON/XML source
- Parsed structure
- COUNT IN values
- PROCESSING PROCEDURE events
- Mapped COBOL fields
- OCCURS expansions
- REDEFINES active view
- ON EXCEPTION state

Sequence points emitted for:
- Each JSON element
- Each XML event
- Each assignment into COBOL record

------------------------------------------------------------
SECTION 9 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

9.1 JSON null → COBOL
----------------------
- PIC X → spaces
- PIC 9 → zero
- Group → all children defaulted

9.2 XML empty element
---------------------
<name/> → empty string

9.3 Missing JSON field
----------------------
- Default value
- Not ON EXCEPTION

9.4 Extra JSON fields
---------------------
- Ignored unless WITH DETAIL STRICT (planned)

9.5 OCCURS DEPENDING ON mismatch
--------------------------------
- Clamp to legal range

9.6 Invalid numeric in JSON
---------------------------
- ON EXCEPTION

9.7 Mixed national/alphanumeric
-------------------------------
- Illegal unless explicitly converted

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp JSON & XML Processing Architecture:
- Implements full COBOL‑2014+ structured‑data semantics
- Provides deterministic mapping between COBOL records and JSON/XML
- Supports COUNT IN, PROCESSING PROCEDURE, and ON EXCEPTION
- Handles OCCURS, nested groups, REDEFINES, and PIC N
- Uses dedicated runtime engines for correctness and performance
- Generates clean, verifiable CIL
- Integrates deeply with debugging and runtime services
- Ensures correctness across CoreCLR, AOT, and WASM
