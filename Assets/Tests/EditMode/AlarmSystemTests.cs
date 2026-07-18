using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.FPS.AI;
using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Tests
{
    /// <summary>
    /// Edit Mode tests for the enemy alarm feature (SWR-202..206).
    /// MonoBehaviour lifecycle methods do not run automatically in Edit Mode, so the
    /// wiring normally done in Awake/Start (enemy registration, module lookup) is set
    /// up explicitly via reflection.
    /// </summary>
    public class AlarmSystemTests
    {
        readonly List<Object> m_Cleanup = new List<Object>();

        EnemyManager m_EnemyManager;
        GameObject m_Player;

        [SetUp]
        public void SetUp()
        {
            var managerGo = new GameObject("TestEnemyManager");
            m_Cleanup.Add(managerGo);
            m_EnemyManager = managerGo.AddComponent<EnemyManager>();
            InvokePrivate(m_EnemyManager, "Awake");

            m_Player = new GameObject("TestPlayer");
            m_Cleanup.Add(m_Player);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in m_Cleanup)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }

            m_Cleanup.Clear();
        }

        [Test]
        [Verifies(SWR.SWR_202)]
        public void AlarmModule_HasConfigurableRadius_AndFiresAlarmEvent()
        {
            EnemyController enemy = CreateEnemy("Source", Vector3.zero, out _, out AlarmModule alarm);

            Assert.AreEqual(30f, alarm.AlarmRadius, "Default alarm radius must be 30.");
            alarm.AlarmRadius = 12.5f;
            Assert.AreEqual(12.5f, alarm.AlarmRadius, "Alarm radius must be configurable.");

            bool alarmFired = false;
            alarm.onAlarmTriggered += () => alarmFired = true;
            alarm.TriggerAlarm(m_Player);
            Assert.IsTrue(alarmFired, "TriggerAlarm must raise the alarm event.");
        }

        [Test]
        [Verifies(SWR.SWR_205)]
        public void AlarmModule_PropagatesOnlyToLivingEnemiesInRadius_ExcludingSelf()
        {
            EnemyController source = CreateEnemy("Source", Vector3.zero, out DetectionModule sourceDetection,
                out AlarmModule alarm);
            EnemyController near = CreateEnemy("NearEnemy", new Vector3(5f, 0f, 0f), out DetectionModule nearDetection);
            EnemyController far = CreateEnemy("FarEnemy", new Vector3(100f, 0f, 0f), out DetectionModule farDetection);
            EnemyController dead = CreateEnemy("DeadEnemy", new Vector3(0f, 0f, 5f), out DetectionModule deadDetection);
            dead.GetComponent<Health>().CurrentHealth = 0f;

            alarm.AlarmRadius = 30f;
            SetPrivateField(alarm, "m_EnemyManager", m_EnemyManager);

            alarm.TriggerAlarm(m_Player);

            Assert.AreEqual(m_Player, nearDetection.KnownDetectedTarget,
                "Living enemy inside the radius must be alarmed.");
            Assert.IsNull(farDetection.KnownDetectedTarget,
                "Enemy outside the radius must not be alarmed.");
            Assert.IsNull(deadDetection.KnownDetectedTarget,
                "Dead enemy must not be alarmed.");
            Assert.IsNull(sourceDetection.KnownDetectedTarget,
                "The alarming enemy must not alarm itself.");
        }

        [Test]
        [Verifies(SWR.SWR_203)]
        public void EnemyController_TriggersAlarm_WhenTargetIsDetected()
        {
            EnemyController enemy = CreateEnemy("Source", Vector3.zero, out DetectionModule detection,
                out AlarmModule alarm);
            enemy.onDetectedTarget += () => { };
            detection.OnDamaged(m_Player); // establishes the known detected target

            bool alarmFired = false;
            alarm.onAlarmTriggered += () => alarmFired = true;

            InvokePrivate(enemy, "OnDetectedTarget");

            Assert.IsTrue(alarmFired, "Detecting the player must trigger the alarm.");
        }

        [Test]
        [Verifies(SWR.SWR_204)]
        public void EnemyController_TriggersAlarm_WhenDamagedByPlayer_ButNotByEnemies()
        {
            EnemyController enemy = CreateEnemy("Source", Vector3.zero, out DetectionModule detection,
                out AlarmModule alarm);

            bool alarmFired = false;
            alarm.onAlarmTriggered += () => alarmFired = true;

            InvokePrivate(enemy, "OnDamaged", 5f, m_Player);
            Assert.IsTrue(alarmFired, "Damage from the player must trigger the alarm.");
            Assert.AreEqual(m_Player, detection.KnownDetectedTarget,
                "The damage source must become the known target.");

            alarmFired = false;
            EnemyController otherEnemy = CreateEnemy("OtherEnemy", new Vector3(3f, 0f, 0f), out _);
            InvokePrivate(enemy, "OnDamaged", 5f, otherEnemy.gameObject);
            Assert.IsFalse(alarmFired, "Damage from another enemy must not trigger the alarm.");
        }

        [Test]
        [Verifies(SWR.SWR_206)]
        public void AlarmSystem_Integration_PlayerDamagePropagatesAlarmToNearbyEnemy()
        {
            EnemyController source = CreateEnemy("Source", Vector3.zero, out DetectionModule sourceDetection,
                out AlarmModule alarm);
            EnemyController near = CreateEnemy("NearEnemy", new Vector3(5f, 0f, 0f), out DetectionModule nearDetection);
            SetPrivateField(alarm, "m_EnemyManager", m_EnemyManager);

            InvokePrivate(source, "OnDamaged", 10f, m_Player);

            Assert.AreEqual(m_Player, sourceDetection.KnownDetectedTarget,
                "The damaged enemy must know the player as target.");
            Assert.AreEqual(m_Player, nearDetection.KnownDetectedTarget,
                "The nearby enemy must be alarmed about the player through the damage chain.");
        }

        EnemyController CreateEnemy(string name, Vector3 position, out DetectionModule detection)
        {
            var go = new GameObject(name);
            m_Cleanup.Add(go);
            go.transform.position = position;

            EnemyController controller = go.AddComponent<EnemyController>();
            go.GetComponent<Health>().CurrentHealth = 100f;

            var detectionGo = new GameObject($"{name}Detection");
            detectionGo.transform.SetParent(go.transform, false);
            detection = detectionGo.AddComponent<DetectionModule>();
            typeof(EnemyController).GetProperty(nameof(EnemyController.DetectionModule))
                .SetValue(controller, detection);

            m_EnemyManager.RegisterEnemy(controller);
            return controller;
        }

        EnemyController CreateEnemy(string name, Vector3 position, out DetectionModule detection,
            out AlarmModule alarm)
        {
            EnemyController controller = CreateEnemy(name, position, out detection);
            alarm = controller.gameObject.AddComponent<AlarmModule>();
            return controller;
        }

        static void InvokePrivate(object target, string methodName, params object[] args)
        {
            target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, args);
        }

        static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }
    }
}
