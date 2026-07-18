---
req-id: SWR-103
status: approved
trace: required
title: Freischaltungs-Trigger fuer Barrieren
---

# 03-code-unlock-trigger – Freischaltungs-Trigger implementieren

**Status:** erledigt

## Ziel

Ein System implementieren, das die Freischaltung der Barriere ausloest, wenn eine definierte Bedingung erfuellt ist.

## Kurzbeschreibung

Erstellung einer Freischaltungs-Komponente, die:

- Ueber eine Referenz mit einer oder mehreren Barrier-Instanzen verbunden ist.
- Eine Freischaltbedingung ueberwacht (z. B. "alle Gegner im Level besiegt", "Schalter aktiviert", "Schluessel eingesammelt").
- Bei erfuellter Bedingung die verknuepften Barrieren freischaltet.
- Initiale Umsetzung mit der Bedingung "Alle Gegner besiegt" (Integration mit dem Gegner-Management-System des Projekts).

## Abhaengigkeiten

- **01-code-barrier-component** – Die Barrier-Komponente mit Freischalt-Funktion.
- **02-code-visual-feedback** – Visuelles Feedback sollte vorhanden sein, ist aber nicht zwingend.

## Akzeptanzkriterien

- [x] Die Freischaltungs-Komponente existiert und kann im Editor konfiguriert werden.
- [x] Die Komponente haelt eine Referenz auf eine oder mehrere Barrier-Instanzen.
- [x] Die Bedingung "Alle Gegner besiegt" ist implementiert und loest die Freischaltung aus.
- [x] Die Freischaltung wird nur einmal ausgeloest (wird nicht wiederholt).
- [x] Das System arbeitet mit dem bestehenden Gegner-Management des Projekts zusammen.

## Definition of Done

- [x] Die Komponente kompiliert fehlerfrei.
- [x] Freischaltung durch "Alle Gegner besiegt" funktioniert im Play Mode.
- [x] Mehrere Barrieren koennen gleichzeitig freigeschaltet werden.
- [x] Feature wurde im Play Mode geprueft.
- [x] Keine bekannten Regressionen (Gegner-KI, Gegner-Management unbeeintraechtigt).
