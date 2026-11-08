---
title: The communication facility
description: The COMMUNICATION SECTION of COBOL 1985 and 2002, the message control system WiseOwl COBOL provides, and the mcs.json file that defines its queues and destinations.
sidebar:
  order: 8
---

COBOL 1985 and COBOL 2002 define a communication facility: a `COMMUNICATION SECTION` with CD entries, and the statements
`ACCEPT cd-name MESSAGE COUNT`, `ENABLE`, `DISABLE`, `PURGE`, `RECEIVE` and `SEND`. WiseOwl COBOL compiles them at `--std 85` and
`--std 2002`, where each draws the obsolete-element warning COBOL0903. COBOL 2014 removed the facility, so from `--std 2014` each is
COBOL0902 (a warning under `--permissive`, and the program still runs). The `RECEIVE` and `SEND` of COBOL 2023 are a different facility, the asynchronous messaging facility, described in the last section of this page.

## The message control system

The standard's statements talk to a message control system (MCS). WiseOwl COBOL's runtime is one. It keeps its files in one directory,
the environment variable `WISEOWL_COBOL_MCS`, or the current directory when that is unset:

| File | What it is |
|---|---|
| `mcs.json` | Your definitions: the symbolic names of the queues, sources, destinations and terminals |
| `mcs.store` | The MCS's store: the messages it holds and which paths are enabled. A changed `mcs.json` is read in by the next run unit: an endpoint it still names keeps its enabled state and its messages, a new one starts as defined, and one it no longer names is dropped with its messages. To start again from your definitions, delete the store while no run unit has it open |
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
`mcs.json` file cannot be used, and `92`, the host refuses the MCS's files or cannot lock them (a file system with no usable locks, such as some network file systems, cannot carry a store that two run units share, and the MCS never runs unlocked). Status `91` and `92` take no action, so a program can repeat the statement and it takes effect once.
A third, `93`, means the store refused the statement and the MCS cannot tell whether it took effect: the CD's area changes only in its status key, and a program that repeats the statement can deliver a message twice. At COBOL 1985, status `40` reports a wrong `KEY`.

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
`FUNCTION BYTE-LENGTH` of a tag is 8. Section 7 of `docs/CONFORMANCE.md` records the representation (item 212) and how Format 17 of `SET` is read (D-SET17).

## Asynchronous messaging (COBOL 2023)

`SEND` and `RECEIVE` exchange messages between run units of one user on one machine, with no broker: each run unit holds its messages in memory and the run units
talk directly over `AF_UNIX` stream sockets. A run unit that answers requests is a **server**. It serves a name when it is started with
`WISEOWL_COBOL_MCS_SERVER=<name>`; the name is compared without regard to case and trailing spaces. Run units share one message control system when they name
the same directory in `WISEOWL_COBOL_MCS` (default: the current directory).

```cobol
*> the requestor
SEND TO "BALANCE" FROM REQUEST-REC RETURNING SERVER-TAG
    ON EXCEPTION DISPLAY "no server: " FUNCTION EXCEPTION-STATUS
END-SEND
RECEIVE SERVER-TAG GIVING REPLY-REC REPLY-LEN
    ON EXCEPTION DISPLAY "no reply"
END-RECEIVE

*> the server
RECEIVE ANY-TAG GIVING REQUEST-REC REQUEST-LEN       *> ANY-TAG holds NULL
SEND TO ANY-TAG FROM REPLY-REC                       *> ANY-TAG now names the requestor
```

A `SEND` waits until a server takes the message, then sets the tag after `RETURNING` to the server's. A `RECEIVE` whose tag is `NULL` waits for any requestor and sets
the tag to the one it received from; a `RECEIVE` of a server's tag waits for that server's reply. The message is `FROM` / `GIVING` item's storage, so both run units
describe it alike, and the length of the message received lands in the last operand. `CONTINUE AFTER MESSAGE RECEIVED` and `CONTINUE AFTER n SECONDS` stop a
`RECEIVE` from waiting: it is then successful, the item is spaces and the length is zero. A server that ends without replying is answered for with
`EC-MCS-NORMAL-TERMINATION`; one that is killed, with `EC-MCS-ABNORMAL-TERMINATION`. `SEND ... RAISING EXCEPTION name` sends a condition to the requestor, whose
`RECEIVE` is then unsuccessful with it. A `RECEIVE` of `NULL` in a run unit that serves no name is `EC-MCS-NO-REQUESTER` at once.

Whether a statement is successful never depends on checking: `ON EXCEPTION` and `NOT ON EXCEPTION` follow the outcome. The exception condition is set only when
checking for it is enabled (`>>TURN EC-MCS CHECKING ON`); `EC-MCS-IMP`, a failure of the message control system itself, is fatal. The determinations are
`docs/CONFORMANCE.md` section 3 (D-MCS-SERVER, D-MCS-LENGTH, D-MCS-WAIT, D-MCS-RAISE, D-MCS-CHECK, D-MCS-TRANSPORT) and the design is
`docs/rearchitecture/DESIGN-message-control.md`. Linux and Windows are claimed; macOS runs the same Unix code but has no CI leg. A group item that holds message tags can be the message area: the tags travel
with the message in storage order, and a receiving group that holds more tags than the message carried gets NULL for the rest. So does a variable-length group (an OCCURS
DYNAMIC table) that holds a tag.
