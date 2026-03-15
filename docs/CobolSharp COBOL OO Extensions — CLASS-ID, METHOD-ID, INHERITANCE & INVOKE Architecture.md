CobolSharp COBOL OO Extensions — CLASS-ID, METHOD-ID, INHERITANCE & INVOKE Architecture (CIL‑Only)
=================================================================================================

Purpose
-------
Define the authoritative architecture for:
- CLASS-ID / FACTORY / OBJECT sections
- METHOD-ID (static, instance, property, constructor)
- INHERITANCE (INHERITS FROM)
- INVOKE (static and instance)
- NEW object creation
- Properties (GET/SET)
- Data division inside classes
- ACCESS MODIFIERS (PUBLIC, PRIVATE, PROTECTED)
- OVERRIDE / FINAL
- CIL‑friendly lowering
- AOT/WASM‑safe OO execution

This document governs how CobolSharp implements COBOL’s OO model on .NET.

------------------------------------------------------------
SECTION 1 — OO MODEL OVERVIEW
------------------------------------------------------------

CobolSharp implements the ISO/IEC 1989:2023 OO model:
- Classes map to .NET classes
- Methods map to .NET methods
- FACTORY section → static members
- OBJECT section → instance members
- INHERITS FROM → .NET inheritance
- INVOKE → method dispatch
- NEW → object instantiation
- Properties → .NET properties
- ACCESS modifiers → .NET visibility

OO execution is:
- Deterministic
- Pure managed
- AOT/WASM‑safe

------------------------------------------------------------
SECTION 2 — CLASS-ID ARCHITECTURE
------------------------------------------------------------

2.1 Basic form
--------------
CLASS-ID. MyClass
    INHERITS FROM BaseClass
    IMPLEMENTS Interface1 Interface2.

Compiler generates:
- public class MyClass : BaseClass, Interface1, Interface2

2.2 FACTORY section
-------------------
FACTORY.
- Static fields
- Static methods
- Static properties

2.3 OBJECT section
------------------
OBJECT.
- Instance fields
- Instance methods
- Instance properties

2.4 END CLASS
-------------
Marks end of class definition.

------------------------------------------------------------
SECTION 3 — METHOD-ID ARCHITECTURE
------------------------------------------------------------

3.1 Basic form
--------------
METHOD-ID. MethodName.

3.2 Static vs instance
----------------------
FACTORY section → static  
OBJECT section → instance

3.3 Parameters
--------------
METHOD-ID. Foo USING BY VALUE x BY REFERENCE y.

Lowering:
- .NET method Foo(decimal x, StorageBlock y)

3.4 RETURNING
-------------
METHOD-ID. Bar RETURNING result.

Lowering:
- .NET return type = type of result

3.5 Constructors
----------------
METHOD-ID. NEW.

Lowering:
- public MyClass()

3.6 Properties
--------------
PROPERTY GET Foo.  
PROPERTY SET Foo.

Lowering:
- public decimal Foo { get; set; }

------------------------------------------------------------
SECTION 4 — DATA DIVISION INSIDE CLASSES
------------------------------------------------------------

4.1 FACTORY data
----------------
FACTORY.
DATA DIVISION.
WORKING-STORAGE SECTION.
- Static fields

4.2 OBJECT data
---------------
OBJECT.
DATA DIVISION.
WORKING-STORAGE SECTION.
- Instance fields

4.3 Field lowering
------------------
01 Counter PIC S9(9) COMP-5.

Lowering:
- private long Counter;

------------------------------------------------------------
SECTION 5 — INHERITANCE
------------------------------------------------------------

5.1 INHERITS FROM
-----------------
CLASS-ID. Child INHERITS FROM Parent.

Lowering:
- class Child : Parent

5.2 OVERRIDE
------------
METHOD-ID. Foo OVERRIDE.

Lowering:
- override Foo()

5.3 FINAL
---------
METHOD-ID. Foo FINAL.

Lowering:
- sealed override Foo()

5.4 Polymorphism
----------------
INVOKE obj Foo → virtual dispatch

------------------------------------------------------------
SECTION 6 — INVOKE ARCHITECTURE
------------------------------------------------------------

6.1 Static invoke
-----------------
INVOKE MyClass::Foo USING x.

Lowering:
- MyClass.Foo(x)

6.2 Instance invoke
-------------------
INVOKE obj Foo USING x.

Lowering:
- obj.Foo(x)

6.3 INVOKE with RETURNING
-------------------------
INVOKE obj Bar RETURNING r.

Lowering:
- r = obj.Bar()

6.4 INVOKE property
-------------------
INVOKE obj::Foo OF obj → getter  
INVOKE obj::Foo = value → setter

------------------------------------------------------------
SECTION 7 — NEW OBJECT CREATION
------------------------------------------------------------

7.1 Basic form
--------------
INVOKE MyClass NEW RETURNING obj.

Lowering:
- obj = new MyClass()

7.2 Constructor parameters
--------------------------
INVOKE MyClass NEW USING x y RETURNING obj.

Lowering:
- obj = new MyClass(x, y)

------------------------------------------------------------
SECTION 8 — ACCESS MODIFIERS
------------------------------------------------------------

8.1 PUBLIC
----------
PUBLIC → public

8.2 PRIVATE
-----------
PRIVATE → private

8.3 PROTECTED
-------------
PROTECTED → protected

8.4 Default visibility
----------------------
Default = PUBLIC

------------------------------------------------------------
SECTION 9 — CIL LOWERING RULES
------------------------------------------------------------

9.1 Class lowering
------------------
CLASS-ID → .NET class  
FACTORY → static members  
OBJECT → instance members

9.2 Method lowering
-------------------
METHOD-ID → .NET method  
USING → parameters  
RETURNING → return type

9.3 INVOKE lowering
-------------------
Static:
    call MyClass.Foo

Instance:
    callvirt obj.Foo

9.4 Property lowering
---------------------
PROPERTY GET → get_Foo  
PROPERTY SET → set_Foo

9.5 Data lowering
-----------------
PIC → .NET primitive  
Group items → struct or nested class

------------------------------------------------------------
SECTION 10 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Instance fields
- Static fields
- Method parameters
- Return values
- Property values
- Inheritance hierarchy
- Virtual dispatch target

------------------------------------------------------------
SECTION 11 — AOT/WASM‑SAFE OO EXECUTION
------------------------------------------------------------

11.1 No reflection
------------------
INVOKE uses:
- Static call
- Virtual call
- No Type.GetType

11.2 No dynamic codegen
-----------------------
All methods compiled statically.

11.3 Deterministic dispatch
---------------------------
Virtual dispatch identical across platforms.

------------------------------------------------------------
SECTION 12 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

12.1 INVOKE on null object
--------------------------
Runtime error:
OBJECT-REFERENCE-NOT-SET

12.2 OVERRIDE without matching base method
------------------------------------------
Compile-time error.

12.3 FINAL method overridden
----------------------------
Compile-time error.

12.4 NEW on abstract class
--------------------------
Compile-time error.

12.5 PROPERTY SET on read-only property
---------------------------------------
Compile-time error.

12.6 PROPERTY GET on write-only property
----------------------------------------
Compile-time error.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp OO Architecture:
- Implements full COBOL OO semantics: classes, methods, inheritance, INVOKE, NEW
- Maps cleanly to .NET classes, methods, properties, and inheritance
- Provides deterministic, safe, AOT/WASM‑compatible object execution
- Integrates tightly with ExecutionContext and StorageBlocks
- Generates clean, verifiable, debugger‑friendly CIL
