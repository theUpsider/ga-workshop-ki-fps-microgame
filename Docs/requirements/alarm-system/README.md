---
req-id: SWR-200
status: approved
trace: optional
title: Epic: Alarm-System fuer Gegner
---

# Alarm-System für Gegner

## Feature-Beschreibung

Im Unity FPS Microgame soll ein Alarm-System für Gegner implementiert werden. Wenn ein Gegner den Spieler entdeckt oder vom Spieler Schaden erhält, sollen andere Gegner in der Nähe ebenfalls alarmiert werden. Alarmierte Gegner sollen den Spieler daraufhin als Bedrohung behandeln und mit ihrer normalen Gegnerlogik auf ihn reagieren (z. B. verfolgen oder angreifen).

Die Lösung soll mit den vorhandenen Systemen der Gegner-KI arbeiten und keine unnötige doppelte Logik erzeugen.

## Akzeptanzkriterien

- [x] Wenn ein Gegner den Spieler entdeckt, werden nahe Gegner in einem definierten Radius ebenfalls alarmiert.
- [x] Wenn ein Gegner vom Spieler Schaden erhält, werden nahe Gegner in einem definierten Radius ebenfalls alarmiert.
- [x] Nur Gegner innerhalb dieses Radius duerfen alarmiert werden.
- [x] Ein Gegner darf sich nicht selbst erneut alarmieren.
- [x] Bereits tote oder zerstoerte Gegner duerfen nicht alarmiert werden.
- [x] Alarmierte Gegner sollen den Spieler anschliessend mit ihrer bestehenden KI weiterverarbeiten, also zum Beispiel verfolgen oder angreifen, sofern ihre normale Logik das vorsieht.
- [x] Der Alarm-Radius soll als konfigurierbarer Wert im Inspector einstellbar sein.
- [x] Die Implementierung soll in die vorhandene Struktur des Projekts integriert werden und keine unnoetige doppelte Logik erzeugen.

## Subtasks

| #   | Task                                                 | Beschreibung                                                          |
| --- | ---------------------------------------------------- | --------------------------------------------------------------------- |
| 01  | [Analyse Gegner-KI](01-code-analyze-enemy-ai.md)     | Codebase verstehen: Spieler-Erkennung, Schadenslogik, Gegnerzustaende |
| 02  | [Alarm-Komponente](02-code-alarm-component.md)       | Neue Komponente mit konfigurierbarem Radius                           |
| 03  | [Alarm bei Erkennung](03-code-alarm-on-detection.md) | Alarm ausloesen, wenn Gegner den Spieler entdeckt                     |
| 04  | [Alarm bei Schaden](04-code-alarm-on-damage.md)      | Alarm ausloesen, wenn Gegner Schaden vom Spieler erhaelt              |
| 05  | [Alarm-Propagation](05-code-alarm-propagation.md)    | Alarm an nahe Gegner weitergeben (Selbst-/Toten-Ausschluss)           |
| 06  | [Integration & Test](06-code-integration-test.md)    | End-to-End-Verifikation mit mehreren Gegnern im Level                 |
