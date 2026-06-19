using Unity.FPS.Game;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Condition that must be met to unlock the associated barriers.
    /// </summary>
    public enum UnlockCondition
    {
        /// <summary>
        /// All registered enemies must be killed.
        /// </summary>
        AllEnemiesKilled = 0
    }

    /// <summary>
    /// Listens for gameplay events and unlocks one or more <see cref="Barrier"/> instances
    /// when a configurable condition is met. Triggers only once.
    /// </summary>
    public class UnlockTriggerBarrier : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("The condition that must be met to unlock the barriers.")]
        public UnlockCondition Condition = UnlockCondition.AllEnemiesKilled;

        [Tooltip("The barriers to unlock when the condition is met.")]
        public List<Barrier> Barriers = new List<Barrier>();

        bool m_HasTriggered;

        void Start()
        {
            EventManager.AddListener<EnemyKillEvent>(OnEnemyKilled);
        }

        void OnEnemyKilled(EnemyKillEvent evt)
        {
            if (m_HasTriggered)
                return;

            if (Condition == UnlockCondition.AllEnemiesKilled && evt.RemainingEnemyCount == 0)
            {
                foreach (var barrier in Barriers)
                {
                    if (barrier != null)
                    {
                        Debug.Log($"[UnlockTriggerBarrier] Unlocking barrier '{barrier.gameObject.name}'.");
                        barrier.TryUnlock();
                    }
                }

                m_HasTriggered = true;
            }
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<EnemyKillEvent>(OnEnemyKilled);
        }
    }
}
