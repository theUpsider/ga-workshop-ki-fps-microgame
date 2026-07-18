---
req-id: SWR-101
status: approved
trace: required
title: Barrier-Komponente mit locked/unlocked-Zustand und Kollisionslogik
---

# 01-code-barrier-component – Barrier-Komponente erstellen

**Status:** erledigt

## Ziel

Eine neue Komponente fuer eine sperrbare Barriere erstellen, die einem GameObject im Level hinzugefuegt werden kann. Sie verwaltet die Zustaende `locked` und `unlocked` und steuert die Kollision der Barriere entsprechend. Der `unlocked`-Zustand ist persistent und faellt nicht zurueck.

## Kurzbeschreibung

Die Komponente soll:

- Zwei Zustaende besitzen: `locked` und `unlocked` (als serialisierbaren Wert).
- Im `locked`-Zustand die Kollision aktiv halten (Blockade).
- Im `unlocked`-Zustand die Kollision deaktivieren (Durchgang frei).
- Eine oeffentliche Methode bereitstellen, die den Zustand auf `unlocked` wechselt.
- Den Zustandswechsel per Log-Ausgabe dokumentieren (Testbarkeit).
- Einen konfigurierbaren Initialzustand im Editor bieten (Default: `locked`).

## Abhaengigkeiten

- Keine – dies ist der erste Task des Features.

## Akzeptanzkriterien

- [x] Die Komponente existiert und kann einem GameObject hinzugefuegt werden.
- [x] Sie besitzt die Zustaende `locked` und `unlocked` als serialisierten Wert.
- [x] Im `locked`-Zustand ist die Kollision der Barriere aktiv.
- [x] Im `unlocked`-Zustand ist die Kollision deaktiviert.
- [x] Eine oeffentliche Methode schaltet von `locked` auf `unlocked` um.
- [x] Der `unlocked`-Zustand ist persistent – eine einmal freigeschaltete Barriere kann nicht zurueck in `locked` fallen.
- [x] Der Zustandswechsel wird per Log-Ausgabe ausgegeben (Testbarkeit).
- [x] Der Initialzustand ist im Inspector konfigurierbar (Default: `locked`).
- [x] Code-Stil und Ordnerstruktur passen zur bestehenden Codebase.

## Definition of Done

- [x] Die Komponente ist erstellt und kompiliert fehlerfrei.
- [x] Beide Zustaende sind im Play Mode testbar (manuelles Umschalten moeglich).
- [x] Kollision blockiert im `locked`-Zustand, laesst durch im `unlocked`-Zustand.
- [x] Zustandswechsel wird geloggt.
- [x] Keine bekannten Regressionen.
