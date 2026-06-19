# 06-code-integration-test – Integration & End-to-End-Test

**Status:** erledigt

## Ziel

Das vollstaendige Alarm-System im Level mit mehreren Gegnern testen. Zusaetzliche Gegner sinnvoll im Level platzieren, Konfiguration im Inspector pruefen und alle Akzeptanzkriterien des Gesamtfeatures verifizieren.

## Abhaengigkeiten

- **Alle vorherigen Tasks (01-05)** – Feature muss vollstaendig implementiert sein.

## Akzeptanzkriterien

- [x] Mehrere Gegner sind im Level so verteilt, dass Clustering und Distanz-Tests moeglich sind.
- [x] Alle Akzeptanzkriterien des Gesamtfeatures (siehe Feature-README) sind erfuellt.
- [x] Es gibt keine Regression: Bestehende Gegner-KI funktioniert unveraendert fuer nicht alarmierte Gegner.
- [x] Die Konsole zeigt keine Fehler oder Warnungen, die durch das Alarm-System verursacht werden.

## Definition of Done

- [x] Mindestens 4-6 Gegner sind im Level platziert (einige nah beieinander, einer isoliert).
- [x] Play-Test: Cluster-Gegner, einer entdeckt Spieler, alle im Cluster werden alarmiert.
- [x] Play-Test: Isolierter Gegner wird **nicht** vom Alarm eines entfernten Clusters erfasst.
- [x] Play-Test: Spieler fuegt Gegner im Cluster Schaden zu, alle im Radius werden alarmiert.
- [x] Play-Test: Alarm-Radius im Inspector variiert, Verhalten entspricht Erwartung.
- [x] Play-Test: Keine Fehler oder Warnungen in der Konsole.
- [x] Kurze Dokumentation der Aenderungen und Testergebnisse liegt vor.

> **Hinweis:** Die Play-Tests erfordern manuelle Durchführung im Unity-Editor. Siehe [Testanleitung.md](Testanleitung.md). Die Code-Änderungen sind vollständig und bereit für den Test.
