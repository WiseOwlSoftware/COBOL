// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>
/// ONE CLASS OR INTERFACE ENTRY OF A PROCEDURE-DIVISION-HEADER RAISING PHRASE, RESOLVED to its symbol (ISO §14.2.1;
/// kb/Work PB1121, PB2403): an <see cref="RaisingTargetKind.ObjectClass"/> entry's class with its FACTORY phrase, or an
/// <see cref="RaisingTargetKind.Interface"/> entry's interface. It is what the §14.6.13.1.5 items 1 and 3 census reads (an
/// exception object is applicable when it is an instance of the class, of the factory of the class, or of a class that
/// implements the interface), so the backend renders that census from resolved symbols and never asks the class table.
/// Written at the end of the bind (<c>BackendAnswers</c>) for each program unit (<see cref="BoundUnit.RaisingObjects"/>) and
/// each method (<see cref="OoMethodBinding.RaisingObjects"/>); an entry that names no class or interface of the group's table
/// has none, and an exception-name entry is not one.
/// </summary>
/// <param name="Class">The class an object-class-name-1 entry names; null for an interface entry.</param>
/// <param name="Factory">The FACTORY phrase of a class entry (§14.2.1: <c>[ FACTORY OF ] object-class-name-1</c>).</param>
/// <param name="Interface">The interface an interface-name-1 entry names; null for a class entry.</param>
public readonly record struct RaisingObject(OoClassSymbol? Class, bool Factory, OoInterfaceSymbol? Interface);
