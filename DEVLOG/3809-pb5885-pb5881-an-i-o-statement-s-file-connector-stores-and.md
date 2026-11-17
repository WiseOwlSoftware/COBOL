## Entry 3809 — 2026-11-16 20:07 PST — PB5885, PB5881: an I-O statement's file-connector stores, and an alphabetic slice's class, reach the binder screens

Wave 20262097 group D: two binder screens that missed an operand.

**PB5885.** A READ in a USE BEFORE REPORTING procedure whose record area held a control data item compiled at every edition
(re-probed: `ubr_read.cob` exit 0 at --std 85 and 2023), though §1985:XIII.4.9.3 SR4 and §2023:14.9.49.3 SR11 forbid the
procedure to alter a control data item. `BoundStores.StoresInto`, the one store classification the screen asks, counted the INTO
operand and not the record area. It now takes the places a file connector stores into without the statement naming them
(`PlaceBuilder.ConnectorStores`, by `FileConnectorStore`: record area, FILE STATUS, RELATIVE KEY, DEPENDING ON), and each I-O arm
says which its verb stores: READ all four (§2023:14.9.30.4 GR13, GR25; §2023:13.18.43.4 GR15; §2023:9.1.13.1 with
§2023:12.4.5.8.4 GR1), RETURN the record area and DEPENDING ON item, OPEN, CLOSE, WRITE, REWRITE, DELETE, START and UNLOCK the FILE
STATUS item, a sequential-access relative WRITE the RELATIVE KEY item (§2023:14.9.51.4 GR29 a)), SORT and MERGE what their implicit
READ and WRITE store (§2023:14.9.40.4 GR9). The property-temp and debug-trigger queries pass no connector. The note's other verbs
(RECEIVE, ACCEPT, STRING, UNSTRING, INSPECT, SEARCH, PERFORM VARYING, SET, INITIALIZE) already classified every receiver; a CALL
BY REFERENCE stays uncounted, since whether the callee stores is not known from the source element. After: exit 65, COBOL2378 at
both READs. Goldens `14.9.49-USE/pb5885-read-control-record-area` and `XIII.4.9@1985-.../pb5885-read-control-record-area-85`;
13 new `DeclarativesStructuralRulesTests` cases (the ten refusals go red with the connector disabled). SR-14.9.49.3-11 stays
CONFORMS with its notes restated and three witnesses; 1985-SR-XIII.4.9.3-4 gained its 85 witnesses. Branch review R1-1: the
first cut counted the RELATIVE KEY item as stored by every READ, refusing a legal random READ; only a format 1 (sequential)
READ stores it (§1985:VIII.4.5.4 GR14, §2023:14.9.30.4 GR25), with accepted unit cases for the random and dynamic forms and
the conformance case `pb5885-random-read-relative-key-control-85`.

**PB5881.** `IF AB (1:2) NUMERIC` (AB PIC A) compiled at --std 85 (re-probed exit 0), where §1985:IV.4.3.8.4.4 GR5 keeps the
slice alphabetic and §1985:VI.6.3.1.2 bars the NUMERIC test on it. The class condition read the alphabetic fact off the base
item's description, null for a slice; it now reads `MoveTable16.SenderPosition`, the one operand classifier. Sibling:
`IntrinsicArgumentRules.ClassOfPlace` called an alphabetic slice class alphanumeric and now calls it alphabetic, as the whole
item is; the arithmetic-resultant message names a slice's category through `ItemCategory.CategoryName`. After: COBOL6917 at 85,
exit 0 at 2023 (no such bar there). Golden `IV.4.3.8.4@1985-Reference-Modification/pb5881-refmod-alphabetic-numeric-test-85`;
1985-GR-IV.4.3.8.4.4-5 gained it as its 85 witness.

No NIST program uses USE BEFORE REPORTING. Gate: run by the dispatching session.

