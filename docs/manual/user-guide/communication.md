---
title: The communication facility
description: The COMMUNICATION SECTION of COBOL 1985 and 2002, the message control system WiseOwl COBOL provides, and the mcs.json file that defines its queues and destinations.
sidebar:
  order: 8
---

COBOL 1985 and COBOL 2002 define a communication facility: a `COMMUNICATION SECTION` with CD entries, and the statements
`ACCEPT cd-name MESSAGE COUNT`, `ENABLE`, `DISABLE`, `PURGE`, `RECEIVE` and `SEND`. WiseOwl COBOL compiles them at `--std 85` and
`--std 2002`, where each draws the obsolete-element warning COBOL0903. COBOL 2014 removed the facility, so from `--std 2014` each is
COBOL0902 (a warning under `--permissive`, and the program still runs). The `RECEIVE` and `SEND` of COBOL 2023 are a different facility,
which this compiler declines (`docs/CONFORMANCE.md`, section 4).

## The message control system

The standard's statements talk to a message control system (MCS). WiseOwl COBOL's runtime is one. It keeps its files in one directory,
the environment variable `WISEOWL_COBOL_MCS`, or the current directory when that is unset:

| File | What it is |
|---|---|
| `mcs.json` | Your definitions: the symbolic names of the queues, sources, destinations and terminals |
| `mcs-state.json` | The messages the MCS holds and which paths are enabled. Delete it to start again from your definitions |
| `mcs.lock` | Held for the length of one statement, so run units on one machine can share the directory |
| `<name>.out` | A destination's or terminal's device: each complete message, one per line |

With no `mcs.json`, every symbolic name is unknown: the statements run and their status keys say so (20 or 21).

```json
{
  "queues": [
    { "name": "ORDERS", "messages": [ "A SEEDED MESSAGE" ],
      "subQueues": [ { "name": "URGENT" } ] }
  ],
  "sources": [ { "name": "TERMINAL1" } ],
  "destinations": [
    { "name": "ECHO", "deliverTo": "ORDERS/URGENT", "source": "TERMINAL1" },
    { "name": "PRINTER" }
  ],
  "terminals": [ { "name": "DESK1" } ]
}
```

Every member but `name` is optional. A name is 1 to 12 characters and must be usable as a file name on every host, because a destination's or terminal's `.out` file is named after it: it holds none of the characters `\ / : * ? " < > |` or a control character, and it is not a Windows device name (`CON`, `NUL`, `COM1`, `LPT1` and the rest, in any case, with or without an extension or trailing spaces). Two names that would share one `.out` file make it unusable as well: a destination with no `deliverTo` and a terminal, or two such destinations or two terminals, whose names are the same or differ only in case (`PRINTER` and `printer`, or a terminal `DESK1` and a destination `DESK1`), on every host. A name that breaks any rule makes `mcs.json` unusable: every communication statement answers status 91 and takes no action. `enabled` (default `true`) is a path's starting state. `key` is the
password the COBOL 1985 `WITH KEY` phrase is checked against. `messages` enter once, when the MCS first answers, as a string or as
`{ "segments": [...], "group": true, "source": "...", "date": "YYMMDD", "time": "HHMMSSTT" }`. `capacity` bounds a destination's waiting
messages. `deliverTo` makes a destination feed a queue path or `terminal:<name>`, which is how one run unit sends to another; any other
destination writes to its `.out` file.

A process that starts a program on the MCS's behalf sets `WISEOWL_COBOL_MCS_SCHEDULED` to the queue's levels, separated by `/`, or the
terminal's name; a CD with the `INITIAL` clause then holds those names when the program starts.

## Status keys

Table A.1 of the COBOL 2002 standard gives the status key values. WiseOwl COBOL adds two of the `9x` values it leaves to the implementor: `91`, the
`mcs.json` file cannot be used, and `92`, the host refuses the MCS's files. Each takes no action, so a program can repeat the statement. At COBOL 1985, status `40` reports a wrong `KEY`.

`docs/CONFORMANCE.md`, section 3.3, records every determination, and `docs/rearchitecture/DESIGN-communication-facility.md` the design.

## Message tags (COBOL 2023)

COBOL 2023 describes an asynchronous messaging facility whose statements, `SEND` and `RECEIVE`, exchange messages between run units and name
each party with a message tag. WiseOwl COBOL provides the message-tag data item at `--std 2023`: describe it with `USAGE MESSAGE-TAG`
(the `USAGE IS` words are optional), at level 1 or inside a type declaration that has the `STRONG` phrase.

```cobol
01 REPLY-TAG USAGE MESSAGE-TAG.
01 NOTE-TAG  USAGE MESSAGE-TAG.
...
SET NOTE-TAG TO NULL
SET REPLY-TAG TO NOTE-TAG
IF REPLY-TAG = NULL ... END-IF
INITIALIZE REPLY-TAG
```

A message tag starts as `NULL`. `SET tag TO NULL` and `SET tag TO other-tag` set the tag named first. A relation condition compares two tags,
or a tag and `NULL`, for equality only (`=`, `NOT =`, `<>`, `IS [NOT] EQUAL TO`): two tags are equal when both are `NULL` or they name the
same message server or the same requestor. `INITIALIZE` sets a tag to `NULL`, and `INITIALIZE g REPLACING MESSAGE-TAG DATA BY other-tag` copies one.
A tag passes to a program `BY REFERENCE` or `BY CONTENT`, and a `PROCEDURE DIVISION USING BY VALUE` formal may be one; a `CALL` cannot pass one
`BY VALUE`. A message tag takes no `PICTURE` or `VALUE` clause, and it cannot be moved, displayed, accepted, added to or compared in order.
`FUNCTION BYTE-LENGTH` of a tag is 8. At this release no statement makes a tag that is not `NULL`: `SEND` and `RECEIVE` are accepted and do nothing
(`docs/CONFORMANCE.md`, section 4, item 1), so a program can hold and compare only the null content. Section 7 of that document records the
representation (item 212) and how Format 17 of `SET` is read (D-SET17).
