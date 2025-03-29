using FingTools.Internal;
using UnityEngine;

namespace FingTools
{
    public class AudioSourcePool : GenericPool<AudioSource>
    {
        private readonly GameObject _audioSourceContainer;

        public AudioSourcePool(int maxSize) : base(maxSize)
        {
            // Create a container GameObject to hold the AudioSources
            _audioSourceContainer = new GameObject("AudioSourcePoolContainer");
            Object.DontDestroyOnLoad(_audioSourceContainer);

            // Pre-fill the pool with AudioSource instances
            for (int i = 0; i < maxSize; i++)
            {
                var audioSource = CreateNewAudioSource();
                Release(audioSource);
            }
        }

        private AudioSource CreateNewAudioSource()
        {
            var audioSourceObject = new GameObject("PooledAudioSource");
            audioSourceObject.transform.SetParent(_audioSourceContainer.transform);
            return audioSourceObject.AddComponent<AudioSource>();
        }

        public override AudioSource Get()
        {
            var audioSource = base.Get();
            audioSource.gameObject.SetActive(true); // Ensure the AudioSource is active when retrieved
            return audioSource;
        }

        public override void Release(AudioSource item)
        {
            item.Stop(); // Stop any playing audio
            item.clip = null; // Clear the clip
            item.gameObject.SetActive(false); // Deactivate the AudioSource
            base.Release(item);
        }
    }
}