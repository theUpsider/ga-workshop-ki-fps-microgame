---
req-id: SWR-202
status: approved
trace: required
test: required
title: Alarm-Komponente mit konfigurierbarem Radius
---

# 02-code-alarm-component – Alarm-Komponente erstellen

**Status:** erledigt

## Ziel

Eine neue Komponente erstellen, die einem Gegner hinzugefuegt werden kann. Sie enthaelt den konfigurierbaren Alarm-Radius und eine Moeglichkeit, einen Alarm auszuloesen.

## Abhaengigkeiten

- **01-code-analyze-enemy-ai** – Kenntnis der bestehenden Gegner-Struktur.

## Akzeptanzkriterien

- [x] Die neue Komponente existiert und kann einem Gegner hinzugefuegt werden.
- [x] Die Komponente besitzt einen konfigurierbaren Alarm-Radius, der im Inspector editierbar ist.
- [x] Der Default-Wert des Radius ist sinnvoll gewaehlt.
- [x] Die Komponente stellt eine oeffentliche Funktion zum Ausloesen eines Alarms bereit.
- [x] Der Alarm-Ausloeser erzeugt eine Log-Ausgabe zur einfachen Testbarkeit im fruehen Stadium.
- [x] Die Komponente ist einem bestehenden Gegner im Editor hinzufuegbar.

## Definition of Done

- [x] Die neue Komponente ist erstellt und kompiliert fehlerfrei.
- [x] Der Alarm-Radius ist im Inspector sicht- und editierbar.
- [x] Der Alarm kann manuell ausgeloest werden und erzeugt eine Log-Ausgabe.
- [x] Die Komponente ist an mindestens einem Gegner im Level angebracht.
- [x] Code-Stil und Ordnerstruktur passen zur bestehenden Codebase (basierend auf Analyse aus Task 01).
