---
req-id: SWR-204
status: approved
trace: required
test: required
title: Alarm bei Schaden durch den Spieler ausloesen
---

# 04-code-alarm-on-damage – Alarm bei Spieler-Schaden

**Status:** erledigt

## Ziel

Die Stelle in der Gegner-KI, an der ein Gegner Schaden vom Spieler erhaelt, so erweitern, dass ein Alarm auf der Alarm-Komponente desselben Gegners ausgeloest wird.

## Abhaengigkeiten

- **01-code-analyze-enemy-ai** – Kenntnis der Schadens-Stelle.
- **02-code-alarm-component** – Alarm-Komponente existiert.
- **03-code-alarm-on-detection** – Pattern fuer Alarm-Aufruf ist etabliert.

## Akzeptanzkriterien

- [x] Wenn ein Gegner Schaden vom Spieler erhaelt, wird ein Alarm ausgeloest.
- [x] Der Alarm wird **bei jedem Schadensereignis** ausgeloest (Schuss, Nahkampf, etc.), nicht nur beim ersten Mal.
- [x] Eine Log-Meldung erscheint in der Konsole bei Schaden durch den Spieler.
- [x] Die bestehende Schadenslogik (Gesundheit, Todeszustand) wird nicht beeintraechtigt.
- [x] Die Erweiterung ist minimal-invasiv an der identifizierten Schadens-Stelle.

## Definition of Done

- [x] Die Schadens-Stelle ist um den Alarm-Aufruf ergaenzt.
- [x] Play-Test: Spieler fuegt Gegner Schaden zu, Alarm-Logmeldung erscheint.
- [x] Play-Test: Gegner nimmt Schaden und reagiert normal (Gesundheit sinkt, ggf. Tod).
- [x] Mehrere Treffer auf denselben Gegner erzeugen mehrere Alarm-Logs (oder dedupliziert, je nach Design-Entscheidung).
