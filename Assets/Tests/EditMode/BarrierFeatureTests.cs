using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;

namespace Unity.FPS.Tests
{
    /// <summary>
    /// Edit Mode tests for the lockable-barrier feature (SWR-101..104).
    /// MonoBehaviour lifecycle methods do not run automatically in Edit Mode,
    /// so Start() is invoked via reflection where the component relies on it.
    /// </summary>
    public class BarrierFeatureTests
    {
        readonly List<Object> m_Cleanup = new List<Object>();

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
        [Verifies(SWR.SWR_101)]
        public void Barrier_StartsLocked_WithActiveCollision()
        {
            Barrier barrier = CreateBarrier(out BoxCollider collider);

            InvokeStart(barrier);

            Assert.AreEqual(BarrierState.Locked, barrier.CurrentState);
            Assert.IsTrue(collider.enabled, "Collider must block passage while locked.");
        }

        [Test]
        [Verifies(SWR.SWR_101)]
        public void Barrier_TryUnlock_DisablesCollision_AndIsPersistent()
        {
            Barrier barrier = CreateBarrier(out BoxCollider collider);
            InvokeStart(barrier);

            var observedStates = new List<BarrierState>();
            barrier.OnStateChanged.AddListener(state => observedStates.Add(state));

            Assert.IsTrue(barrier.TryUnlock(), "First unlock must succeed.");
            Assert.AreEqual(BarrierState.Unlocked, barrier.CurrentState);
            Assert.IsFalse(collider.enabled, "Collider must be disabled after unlocking.");
            CollectionAssert.Contains(observedStates, BarrierState.Unlocked);

            Assert.IsFalse(barrier.TryUnlock(), "Unlocked barrier must ignore further unlocks.");
            Assert.AreEqual(BarrierState.Unlocked, barrier.CurrentState, "Unlocked state is persistent.");
        }

        [Test]
        [Verifies(SWR.SWR_102)]
        public void Barrier_VisualFeedback_SwitchesMaterialWithState()
        {
            Barrier barrier = CreateBarrier(out _);
            MeshRenderer barrierRenderer = CreateRenderer();
            Material locked = CreateMaterial("LockedMat");
            Material unlocked = CreateMaterial("UnlockedMat");

            barrier.BarrierRenderer = barrierRenderer;
            barrier.LockedMaterial = locked;
            barrier.UnlockedMaterial = unlocked;

            InvokeStart(barrier);
            Assert.AreEqual(locked, barrierRenderer.sharedMaterial, "Locked state must show the locked material.");

            barrier.TryUnlock();
            Assert.AreEqual(unlocked, barrierRenderer.sharedMaterial, "Unlocked state must show the unlocked material.");
        }

        [Test]
        [Verifies(SWR.SWR_103)]
        public void UnlockTrigger_UnlocksBarriers_OnlyWhenAllEnemiesAreKilled()
        {
            Barrier barrier = CreateBarrier(out _);
            InvokeStart(barrier);

            UnlockTriggerBarrier trigger = CreateUnlockTrigger(barrier);

            EventManager.Broadcast(new EnemyKillEvent { Enemy = null, RemainingEnemyCount = 2 });
            Assert.AreEqual(BarrierState.Locked, barrier.CurrentState,
                "Barrier must stay locked while enemies remain.");

            EventManager.Broadcast(new EnemyKillEvent { Enemy = null, RemainingEnemyCount = 0 });
            Assert.AreEqual(BarrierState.Unlocked, barrier.CurrentState,
                "Barrier must unlock once all enemies are killed.");
        }

        [Test]
        [Verifies(SWR.SWR_104)]
        public void BarrierFeature_Integration_KillEventUnlocksBarrierIncludingVisuals()
        {
            Barrier barrier = CreateBarrier(out BoxCollider collider);
            MeshRenderer barrierRenderer = CreateRenderer();
            Material locked = CreateMaterial("LockedMat");
            Material unlocked = CreateMaterial("UnlockedMat");
            barrier.BarrierRenderer = barrierRenderer;
            barrier.LockedMaterial = locked;
            barrier.UnlockedMaterial = unlocked;
            InvokeStart(barrier);

            CreateUnlockTrigger(barrier);

            EventManager.Broadcast(new EnemyKillEvent { Enemy = null, RemainingEnemyCount = 0 });

            Assert.AreEqual(BarrierState.Unlocked, barrier.CurrentState);
            Assert.IsFalse(collider.enabled, "Passage must be free after the unlock chain.");
            Assert.AreEqual(unlocked, barrierRenderer.sharedMaterial,
                "Visual feedback must reflect the unlocked state after the unlock chain.");
        }

        Barrier CreateBarrier(out BoxCollider collider)
        {
            var go = new GameObject("TestBarrier");
            m_Cleanup.Add(go);
            collider = go.AddComponent<BoxCollider>();
            return go.AddComponent<Barrier>();
        }

        UnlockTriggerBarrier CreateUnlockTrigger(Barrier barrier)
        {
            var go = new GameObject("TestUnlockTrigger");
            m_Cleanup.Add(go);
            var trigger = go.AddComponent<UnlockTriggerBarrier>();
            trigger.Condition = UnlockCondition.AllEnemiesKilled;
            trigger.Barriers.Add(barrier);
            InvokeStart(trigger);
            return trigger;
        }

        MeshRenderer CreateRenderer()
        {
            var go = new GameObject("TestBarrierRenderer");
            m_Cleanup.Add(go);
            return go.AddComponent<MeshRenderer>();
        }

        Material CreateMaterial(string name)
        {
            var material = new Material(Shader.Find("Sprites/Default")) { name = name };
            m_Cleanup.Add(material);
            return material;
        }

        static void InvokeStart(MonoBehaviour component)
        {
            component.GetType()
                .GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(component, null);
        }
    }
}
