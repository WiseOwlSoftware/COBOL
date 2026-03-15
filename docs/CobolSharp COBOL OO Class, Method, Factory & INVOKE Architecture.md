CobolSharp COBOL OO Class, Method, Factory & INVOKE Architecture (CIL‑Only)
==========================================================================

Purpose
-------
Define the authoritative architecture for:
- COBOL OO class definitions
- FACTORY and OBJECT sections
- Method definitions and INVOKE semantics
- Constructors (NEW), static methods, instance methods
- Properties (GET/SET)
- Inheritance and interfaces
- Object references and NULL handling
- Integration with .NET types
- CIL‑friendly lowering
- AOT/WASM‑safe object model

This document governs how CobolSharp implements COBOL’s object‑oriented model on .NET.

------------------------------------------------------------
SECTION 1 — OO PROGRAM STRUCTURE
------------------------------------------------------------

A COBOL class contains:
- CLASS‑ID
- ENVIRONMENT DIVISION (optional)
- DATA DIVISION
  - FACTORY
  - OBJECT
- PROCEDURE DIVISION
  - FACTORY
  - OBJECT

CobolSharp maps each COBOL class to:
- A .NET class
- With static members for FACTORY
- With instance members for OBJECT

------------------------------------------------------------
SECTION 2 — FACTORY SECTION
------------------------------------------------------------

2.1 Purpose
-----------
FACTORY section defines:
- Static methods
- Static data
- Constructors (NEW)
- Class‑level utilities

2.2 FACTORY DATA
----------------
FACTORY.
  01 Shared-Value PIC 9.

Maps to:
- static field SharedValue

2.3 FACTORY PROCEDURE
----------------------
FACTORY.
  METHOD-ID. "Create".
  ...

Maps to:
- static method Create()

------------------------------------------------------------
SECTION 3 — OBJECT SECTION
------------------------------------------------------------

3.1 Purpose
-----------
OBJECT section defines:
- Instance fields
- Instance methods
- Properties
- Object state

3.2 OBJECT DATA
---------------
OBJECT.
  01 Balance PIC 9(5)V99.

Maps to:
- instance field Balance

3.3 OBJECT PROCEDURE
---------------------
METHOD-ID. "Deposit".
  ADD amount TO Balance.

Maps to:
- instance method Deposit(amount)

------------------------------------------------------------
SECTION 4 — METHOD DEFINITIONS
------------------------------------------------------------

4.1 METHOD-ID
-------------
METHOD-ID. "Name".

4.2 STATIC vs INSTANCE
----------------------
FACTORY → static  
OBJECT → instance  

4.3 PARAMETERS
--------------
METHOD-ID. "Deposit".
  LOCAL-STORAGE SECTION.
  01 amount PIC 9(5)V99.

Lowering:
- Method receives parameters as .NET arguments
- LOCAL‑STORAGE allocated per call

4.4 RETURNING
-------------
METHOD-ID. "GetBalance" RETURNING result.

Maps to:
decimal GetBalance()

------------------------------------------------------------
SECTION 5 — INVOKE STATEMENT
------------------------------------------------------------

5.1 Basic form
--------------
INVOKE object "Method" USING args RETURNING result

5.2 Static method
-----------------
INVOKE ClassName "Method"

Lowering:
ClassName.Method(args)

5.3 Instance method
-------------------
INVOKE obj "Method"

Lowering:
obj.Method(args)

5.4 Property GET
----------------
INVOKE obj "Property"

Lowering:
obj.Property

5.5 Property SET
----------------
INVOKE obj "Property=" USING value

Lowering:
obj.Property = value

------------------------------------------------------------
SECTION 6 — OBJECT CREATION (NEW)
------------------------------------------------------------

6.1 NEW syntax
--------------
INVOKE ClassName "NEW" RETURNING obj

6.2 Lowering
------------
obj = new ClassName()

6.3 Constructor method
----------------------
If class defines:
METHOD-ID. "NEW".

Then:
- Called automatically after allocation

------------------------------------------------------------
SECTION 7 — INHERITANCE
------------------------------------------------------------

7.1 EXTENDS clause
------------------
CLASS-ID. Child EXTENDS Parent.

Maps to:
class Child : Parent

7.2 Method overriding
---------------------
METHOD-ID. "Name" OVERRIDE.

Maps to:
override Name()

7.3 SUPER invocation
--------------------
INVOKE SUPER "Method"

Maps to:
base.Method()

------------------------------------------------------------
SECTION 8 — INTERFACES
------------------------------------------------------------

8.1 IMPLEMENTS clause
---------------------
CLASS-ID. MyClass IMPLEMENTS IPrintable.

Maps to:
class MyClass : IPrintable

8.2 Interface methods
---------------------
METHOD-ID. "Print" IMPLEMENTS IPrintable.

Maps to:
void Print()

------------------------------------------------------------
SECTION 9 — OBJECT REFERENCES
------------------------------------------------------------

9.1 Reference type
------------------
01 obj OBJECT REFERENCE ClassName.

Maps to:
ClassName obj;

9.2 NULL handling
-----------------
IF obj = NULL
- Standard comparison

9.3 Type checking
-----------------
obj IS ClassName
obj IS NOT ClassName

Lowering:
obj is ClassName

------------------------------------------------------------
SECTION 10 — .NET INTEROP
------------------------------------------------------------

10.1 INVOKE .NET methods
------------------------
INVOKE obj "ToString"

Allowed for:
- Any .NET object
- Any COBOL object

10.2 Using .NET types
---------------------
01 dt TYPE DateTime.

Maps to:
System.DateTime dt;

10.3 Constructors
-----------------
INVOKE TYPE DateTime "NEW" USING args RETURNING dt

------------------------------------------------------------
SECTION 11 — CIL LOWERING RULES
------------------------------------------------------------

11.1 Class lowering
-------------------
COBOL class → .NET class with:
- Static fields/methods for FACTORY
- Instance fields/methods for OBJECT

11.2 Method lowering
--------------------
METHOD-ID → .NET method with:
- ExecutionContext ctx
- Parameters as .NET arguments

11.3 INVOKE lowering
--------------------
Static:
call Class.Method

Instance:
callvirt obj.Method

11.4 NEW lowering
-----------------
newobj Class::.ctor

------------------------------------------------------------
SECTION 12 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- FACTORY vs OBJECT members
- Instance fields
- Method parameters
- RETURNING values
- Object references and types
- Inheritance hierarchy

------------------------------------------------------------
SECTION 13 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

13.1 INVOKE on NULL
-------------------
Runtime exception → ON EXCEPTION if present

13.2 Missing method
-------------------
Compile‑time error

13.3 Overloaded methods
-----------------------
Resolved by parameter count/type

13.4 INVOKE with wrong parameter count
--------------------------------------
Compile‑time error

13.5 NEW on abstract class
--------------------------
Compile‑time error

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp OO Architecture:
- Implements full COBOL OO class, method, property, and INVOKE semantics
- Supports FACTORY/OBJECT sections, inheritance, interfaces, and NEW
- Provides deterministic mapping to .NET classes and methods
- Integrates tightly with ExecutionContext and runtime engines
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
