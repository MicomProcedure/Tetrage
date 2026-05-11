using UnityEngine;

namespace Tetrage.Audio{
    public class InGameSE : MonoBehaviour
    {
        [SerializeField] private AudioSource seAudioSource;
        [SerializeField] private AudioClip cardMoveSE;

        public static InGameSE Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public static void PlayCardMoveSE()
        {
            if (Instance.cardMoveSE != null)
                Instance.seAudioSource.PlayOneShot(Instance.cardMoveSE);
        }
    }
}