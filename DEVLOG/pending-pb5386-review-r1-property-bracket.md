## A property receiver with a function in its reference modifier carries two brackets

Branch review round 1 of the PB5386 landing (R1-1) found a regression: `MOVE "QQ" TO NM OF AR(1) (FUNCTION INTEGER(K):2)` crashed the
emitter with "the object-property receiver ... was claimed ... but no receiver store placed its steps", source main compiles. The property's
PB2078 bracket and the new PB5386 identification bracket sit in two `BoundReceiverBrackets` nodes, and `ReceiverBracketEmitter.Find` returned
only the first match. It now returns every matching bracket of every scope in force, property bracket outermost; `Receive` opens them in that
order and closes them in reverse. Golden `pb5386_move_property_refmod_function` prints `[AQQDEF]` at 2002, 2014 and 2023 (the object
property needs 2002, so no lower edition has a program to refuse). Sibling arms: READ and RETURN INTO, the arithmetic statements and SET all
reach the same `Receive`, so one fix covers them.
