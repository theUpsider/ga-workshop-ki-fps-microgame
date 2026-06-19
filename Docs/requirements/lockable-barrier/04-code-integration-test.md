# 04-code-integration-test – Integration & End-to-End-Test

**Status:** erledigt

## Ziel

Das vollstaendige Feature "Gesperrter Ausgang" wird im Level integriert und einem End-to-End-Test unterzogen. Alle Akzeptanzkriterien des Features werden verifiziert.

## Kurzbeschreibung

- Platzierung der Barrier-Komponente an einem strategischen Punkt im Level (z. B. vor dem Ausgang).
- Konfiguration der Freischaltungs-Komponente mit der Bedingung "Alle Gegner besiegt".
- Test des vollstaendigen Spielflusses: Start → Barriere blockiert → Gegner besiegen → Barriere oeffnet sich → Durchgang nutzbar.
- Dokumentation des Testergebnisses.

## Abhaengigkeiten

- **01-code-barrier-component** – Basis-Komponente.
- **02-code-visual-feedback** – Visuelles Feedback.
- **03-code-unlock-trigger** – Freischaltungslogik.

## Akzeptanzkriterien

- [x] Die Barriere ist an einer sinnvollen Stelle im Level platziert.
- [x] Im initialen Zustand (`locked`) blockiert die Barriere den Spieler.
- [x] Das visuelle Feedback zeigt klar den `locked`-Zustand an.
- [x] Nach Besiegen aller Gegner schaltet die Barriere auf `unlocked` um.
- [x] Das visuelle Feedback zeigt klar den `unlocked`-Zustand an.
- [x] Der Spieler kann den Durchgang nach der Freischaltung passieren.
- [x] Der `unlocked`-Zustand bleibt persistent (kein Rueckfall bei erneutem Betreten).
- [x] Alle Feature-Akzeptanzkriterien aus der README sind erfuellt.

## Definition of Done

- [x] Vollstaendiger Spielfluss mindestens einmal im Play Mode getestet.
- [x] Alle Akzeptanzkriterien des Features sind erfuellt.
- [x] Relevanter Diff wurde gelesen und enthaelt keine ungewollten Aenderungen.
- [x] Keine bekannten Regressionen.
- [x] Testprotokoll ist dokumentiert.
