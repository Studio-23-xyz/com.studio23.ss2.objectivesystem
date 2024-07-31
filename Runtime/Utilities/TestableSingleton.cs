using UnityEngine;

namespace Studio23.SS2.ObjectiveSystem.Utilities
{
    public abstract class TestMonoSingleton<T> : MonoBehaviour
        where T : Component
    {
        public static T Instance { get; private set; }
        protected bool WillGetDestroyed = false;
        internal virtual void Awake()
        {
            if (Instance == null)
            {
                Instance = this as T;
                DontDestroyOnLoad(this);

                Initialize();
            }
            else
            {
                WillGetDestroyed = true;

                Destroy(gameObject);
            }
        }
        /// <summary>
        /// ONLY USE WHEN ABSOLUTELY SURE
        /// Set Instance as this and call Initialize
        /// DOESN'T SET DontDestroyOnLoad()
        /// </summary>
        public void InitializeAsInstanceForTests()
        {
            Instance = this as T;

            Initialize();
        }
    
        /// <summary>
        /// Initialize is called on awake only if this is the first instance
        /// </summary>
        protected abstract void Initialize();
    }
}