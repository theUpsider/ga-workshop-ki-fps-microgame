---
req-id: SWR-201
status: approved
trace: optional
title: Analyse der Gegner-KI (Erkennung, Schaden, Zustaende)
---

# 01-code-analyze-enemy-ai – Analyse der Gegner-KI

**Status:** erledigt

## Ziel

Die vorhandene Gegner-KI-Codebase analysieren und dokumentieren: Wie erkennen Gegner den Spieler? Wo erhalten Gegner Schaden? Welche Zustaende und Komponenten gibt es?

## Abhaengigkeiten

Keine (erster Task).

## Akzeptanzkriterien

- [x] Alle relevanten Skripte der Gegner-KI sind identifiziert und ihre Verantwortlichkeiten dokumentiert.
- [x] Die Stelle(n), an denen ein Gegner den Spieler **entdeckt** (Sichtfeld, Trigger, etc.), sind genau benannt.
- [x] Die Stelle(n), an denen ein Gegner **Schaden vom Spieler** erhaelt, sind genau benannt.
- [x] Die Zustandslogik der Gegner (z. B. Idle, Patrol, Chase, Attack, Dead) ist verstanden und dokumentiert.
- [x] Es ist geklaert, wie Gegner im Level instanziiert und verwaltet werden.

## Definition of Done

- [x] Alle relevanten Skripte sind gelesen und mit Pfad und Schluesselmethoden in einer kurzen Notiz festgehalten.
- [x] Die zwei relevanten Code-Pfade (Spieler-Erkennung + Schadenserhalt) sind eindeutig identifiziert.
- [x] Ein kurzer Analysebericht liegt vor.
- [x] Die Erkenntnisse reichen aus, um mit Task 02 zu beginnen.
