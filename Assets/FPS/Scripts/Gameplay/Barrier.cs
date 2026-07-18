using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Barrier state enumeration.
    /// </summary>
    public enum BarrierState
    {
        Locked = 0,
        Unlocked = 1
    }

    /// <summary>
    /// A lockable barrier component that controls collision based on its locked/unlocked state.
    /// Once unlocked, the barrier cannot be locked again (persistent unlock).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [Traces(SWR.SWR_101)]
    public class Barrier : MonoBehaviour
    {
        [Header("Parameters")]
        [Tooltip("Initial state of the barrier.")]
        public BarrierState InitialState = BarrierState.Locked;

        [Header("Visual Feedback")]
        [Tooltip("Renderer whose material changes with barrier state.")]
        public MeshRenderer BarrierRenderer;

        [Tooltip("Material applied when barrier is locked.")]
        public Material LockedMaterial;

        [Tooltip("Material applied when barrier is unlocked.")]
        public Material UnlockedMaterial;

        public BarrierState CurrentState { get; private set; }

        public UnityEvent<BarrierState> OnStateChanged = new UnityEvent<BarrierState>();

        Collider m_BarrierCollider;

        void Start()
        {
            CurrentState = InitialState;
            m_BarrierCollider = GetComponent<Collider>();
            DebugUtility.HandleErrorIfNullGetComponent<Collider, Barrier>(m_BarrierCollider, this, gameObject);
            ApplyState();
            OnStateChanged.Invoke(CurrentState);
        }

        [Traces(SWR.SWR_102)]
        void ApplyState()
        {
            m_BarrierCollider.enabled = (CurrentState == BarrierState.Locked);

            if (BarrierRenderer != null)
            {
                Material targetMaterial = (CurrentState == BarrierState.Locked) ? LockedMaterial : UnlockedMaterial;

                if (targetMaterial == null)
                {
                    Debug.LogWarning($"[Barrier] {gameObject.name}: No material assigned for state {CurrentState}.");
                }
                else
                {
                    BarrierRenderer.sharedMaterial = targetMaterial;
                }
            }
        }

        public bool TryUnlock()
        {
            if (CurrentState == BarrierState.Unlocked)
                return false;

            CurrentState = BarrierState.Unlocked;
            Debug.Log($"[Barrier] {gameObject.name} unlocked.");
            ApplyState();
            OnStateChanged.Invoke(CurrentState);
            return true;
        }
    }
}
