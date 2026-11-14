- **D-RELINDEXEXPR — an index-name inside a relation's arithmetic expression is refused (kb/Work PB2018).**
  **An index-name is not an operand of an arithmetic expression:** §2023:13.18.38.3 SR7 admits it "as an operand in a
  relation condition" and §2023:8.8.4.2.13 says "Relation tests may be made only between" an index-name and "a numeric
  data item or numeric literal", so a relation operand that is itself an arithmetic expression (`IF N < (K + 2)`,
  `IF K + 1 = 4`, `EVALUATE N WHEN K + 1`) holds the index-name as an operand of the EXPRESSION, where §2023:8.8.1.1 names
  only numeric data items, numeric literals and ZERO; it is COBOL1637 (a warning under `--permissive`, the
  occurrence number computes) exactly as `COMPUTE N = K + 2`, and an index data item there is COBOL0844. kb/Work
  R29's golden had read "inside an expression" as legal; that reading was wider than r7 and §2023:8.8.4.2.13 and is
  withdrawn. Pinned by `conformance:cases/8.4.2.3-Subscripts/index_name_r7_windows` and the `pb2018-*` refusals of `conformance-suite/`.
