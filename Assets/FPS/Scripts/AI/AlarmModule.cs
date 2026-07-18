using UnityEngine;
using UnityEngine.Events;
using Unity.FPS.Game;

namespace Unity.FPS.AI
{
    /// <summary>
    /// Alarm-Komponente für Gegner.
    /// Ermöglicht das Auslösen eines Alarms mit einem konfigurierbaren Radius,
    /// innerhalb dessen andere Gegner benachrichtigt werden können.
    /// </summary>
    [Traces(SWR.SWR_202)]
    public class AlarmModule : MonoBehaviour
    {
        [Tooltip("Radius innerhalb dessen andere Gegner alarmiert werden")]
        public float AlarmRadius = 30f;

        /// <summary>
        /// Event, das beim Auslösen eines Alarms gefeuert wird.
        /// </summary>
        public UnityAction onAlarmTriggered;

        EnemyManager m_EnemyManager;

        void Start()
        {
            m_EnemyManager = FindAnyObjectByType<EnemyManager>();
        }

        /// <summary>
        /// Löst einen Alarm aus und propagiert ihn an alle Gegner im AlarmRadius.
        /// Selbst-Ausschluss und Toten-Ausschluss werden berücksichtigt.
        /// </summary>
        [Traces(SWR.SWR_205)]
        public void TriggerAlarm(GameObject target)
        {
            Debug.Log($"[AlarmModule] Alarm triggered on {gameObject.name}. Alarm radius: {AlarmRadius}");

            if (target != null && m_EnemyManager != null)
            {
                Vector3 myPosition = transform.position;
                float sqrAlarmRadius = AlarmRadius * AlarmRadius;

                foreach (EnemyController enemy in m_EnemyManager.Enemies)
                {
                    // Selbst-Ausschluss
                    if (enemy.gameObject == gameObject)
                        continue;

                    // Toten-Ausschluss
                    if (enemy.TryGetComponent<Health>(out Health health) && health.CurrentHealth <= 0f)
                        continue;

                    // Distanz-Check
                    if ((enemy.transform.position - myPosition).sqrMagnitude > sqrAlarmRadius)
                        continue;

                    // Gegner alarmieren
                    enemy.DetectionModule.OnDamaged(target);
                    Debug.Log($"[AlarmModule] Propagated alarm to {enemy.gameObject.name}");
                }
            }

            onAlarmTriggered?.Invoke();
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            // Alarm-Radius als gelbe WireSphere visualisieren
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, AlarmRadius);
        }
#endif
    }
}
