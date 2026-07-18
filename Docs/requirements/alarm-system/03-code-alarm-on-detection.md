---
req-id: SWR-203
status: approved
trace: required
test: required
title: Alarm bei Spieler-Erkennung ausloesen
---

# 03-code-alarm-on-detection – Alarm bei Spieler-Erkennung

**Status:** erledigt

## Ziel

Die Stelle in der Gegner-KI, an der der Spieler entdeckt wird, so erweitern, dass ein Alarm auf der Alarm-Komponente desselben Gegners ausgeloest wird.

## Abhaengigkeiten

- **01-code-analyze-enemy-ai** – Kenntnis der Erkennungs-Stelle.
- **02-code-alarm-component** – Alarm-Komponente existiert.

## Akzeptanzkriterien

- [x] Sobald ein Gegner den Spieler das erste Mal entdeckt, wird ein Alarm ausgeloest.
- [x] Der Alarm wird nur **einmal pro Entdeckung** ausgeloest (kein Dauer-Feuern jedes Frame).
- [x] Eine Log-Meldung erscheint in der Konsole, wenn ein Gegner den Spieler sieht.
- [x] Die bestehende Gegnerlogik (Verfolgung, Angriff) wird durch die Erweiterung nicht beeintraechtigt.
- [x] Die Erweiterung ist minimal-invasiv, idealerweise nur wenige Zeilen an der identifizierten Stelle.

## Definition of Done

- [x] Die Erkennungs-Stelle ist um den Alarm-Aufruf ergaenzt.
- [x] Play-Test: Sobald ein Gegner den Spieler entdeckt, erscheint die Alarm-Logmeldung.
- [x] Play-Test: Gegner verfolgt/attackiert den Spieler weiterhin normal.
- [x] Kein Alarm-Spam in der Konsole (max. ein Log pro Gegner-Entdeckung).
