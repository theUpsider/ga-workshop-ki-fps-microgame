---
req-id: SWR-205
status: approved
trace: required
title: Alarm-Propagation an nahe Gegner (Selbst-/Toten-Ausschluss)
---

# 05-code-alarm-propagation – Alarm-Propagation zu nahen Gegnern

**Status:** erledigt

## Ziel

Die Alarm-Funktion so implementieren, dass sie alle Gegner im konfigurierten Radius findet, filtert (nicht selbst, nicht tot) und deren KI in den alarmierten Zustand versetzt.

## Abhaengigkeiten

- **02-code-alarm-component** – Alarm-Komponente mit Alarm-Ausloeser.
- **03-code-alarm-on-detection** – Alarm wird bei Erkennung getriggert.
- **04-code-alarm-on-damage** – Alarm wird bei Schaden getriggert.

## Akzeptanzkriterien

- [x] Die Alarm-Funktion findet alle Gegner innerhalb des konfigurierten Radius um den alarmierenden Gegner.
- [x] Der alarmierende Gegner selbst wird **nicht** erneut alarmiert (Selbst-Ausschluss).
- [x] Bereits tote oder zerstoerte Gegner werden **nicht** alarmiert (Toten-Ausschluss).
- [x] Alarmierte Gegner werden in einen Zustand versetzt, in dem ihre bestehende KI den Spieler als Bedrohung behandelt (z. B. Verfolgung, Angriff).
- [x] Die Suche nach nahen Gegnern ist performant (keine ineffizienten Suchen jedes Frame).
- [x] Der Alarm-Radius ist pro Gegner im Inspector individuell einstellbar (bereits in Task 02 umgesetzt, hier validiert).

## Definition of Done

- [x] Die Alarm-Funktion enthaelt die vollstaendige Propagationslogik.
- [x] Play-Test: Zwei Gegner nah beieinander. Gegner A entdeckt Spieler, Gegner B wird alarmiert.
- [x] Play-Test: Gegner ausserhalb des Radius werden **nicht** alarmiert.
- [x] Play-Test: Ein toter Gegner im Radius wird **nicht** alarmiert.
- [x] Play-Test: Gegner B reagiert nach Alarmierung mit seiner normalen KI auf den Spieler.
- [x] Play-Test: Alarm-Radius im Inspector aendern, Verhalten passt sich an (kleiner = nur sehr nahe Gegner, groesser = weiter entfernte Gegner).
