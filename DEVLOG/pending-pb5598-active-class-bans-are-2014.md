## PB5598: the ACTIVE-CLASS bans on property methods, the PROPERTY clause and inline invocation are 2014 text, now gated

At --std 2002 the compiler refused legal 2002 object-oriented source in three places: a GET/SET property method whose
returning item or USING parameter is an ACTIVE-CLASS object reference (COBOL2790, `ScreenPropertyMethodHeader`), a PROPERTY
clause on an ACTIVE-CLASS reference (COBOL2563, `BindPropertyClauses`), and an inline method invocation whose method returns
an ACTIVE-CLASS item (COBOL2140, `OoBindResolvedInvoke`). All three are 2014 additions (§2023:11.7.3 SR6/SR7,
§2023:13.18.42.3 SR6, §2023:8.4.3.4.3 SR4); §2002:11.6.2 SR6 asks only the USING/RETURNING shape, §2002:13.16.40.2 has no
class limit and §2002:8.4.2.4.2 SR4 bans ANY LENGTH alone.

Re-probe on this build before the fix: the note's repros gave COBOL2790, COBOL2563 and COBOL2140 at --std 2002. After the
fix acprop.cob and acinline.cob print SAME-OBJECT at 2002 and still give COBOL2563 and COBOL2140 at 2014. The fix is an
`Edition.Edition.Has(2014)` gate on each ban; the inline-invocation message at 2002 cites §2002:8.4.2.4.2 SR4 and names only
ANY LENGTH.

Goldens: `13.16.40@2002-PROPERTY-CLAUSE/pb5598-active-class-property-2002` (accept 2002; prints SAME three times through
ME OF A, SELFREF OF A and A :: "GETME") and its refused twin `pb5598-active-class-property-refused` (COBOL2790 at 2014 and
2023, `wiseowl/diagnostics/pb5598-active-class-property-refused.json`). Five 2002 rows recorded CONFORMS (2002 GAP 2575 to
2570); seven rows stay claimed because the golden calls no set method and tests no FINAL, superclass property name or exception
resumption.

Four older refusals (pb1273-property-active-class, pb1503-get-property-active-class, pb1503-set-property-active-class, pb428-inline-invocation-returning-active-class) listed 2002 in refuse-at; they now name 2014 and 2023, where the bans are text. RefModCategoryDriftTests exempts a bare 8.4.2.4 preceded by the 2002 edition, since that clause is a real one in 2002. The first gate was RED on exactly these five cases.

Review round 1 (R1-1): OoBindStandardInvoke, the BASE New and FactoryObject arm, still refused an inline invocation at 2002 with the 2014 ACTIVE-CLASS ban. The ban is now gated at Has(2014) there too; below 2014 the invocation delivers into a temporary described as the result class view (CompilerTemps.ForInvocationObject), since BASE's ACTIVE-CLASS returning item has no 2002 description to clone. Golden pb5598-base-inline-new-2002 (SET B TO class :: "NEW" at 2002 prints CREATED) and refused twin pb5598-base-inline-new-refused (COBOL2140 at 2014 and 2023).
