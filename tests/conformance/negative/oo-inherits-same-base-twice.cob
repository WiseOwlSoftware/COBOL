*> reject-at: 2002 2014 2023
*> ISO 11.3.3 SR7's OWN subject: "A given class name shall not appear more than once in an INHERITS
*> clause." Annex A.4.10 item 1 (multiple inheritance) is DECLINED, so the repetition that would make the
*> rule reachable is refused first. The pre-existing fixture oo-multi-base-inherits uses two DISTINCT base
*> names, so this rule had no witness on its own subject.
       IDENTIFICATION DIVISION.
       CLASS-ID. MBDUP INHERITS FROM MBBASEA MBBASEA.
       END CLASS MBDUP.
       IDENTIFICATION DIVISION.
       CLASS-ID. MBBASEA.
       END CLASS MBBASEA.
