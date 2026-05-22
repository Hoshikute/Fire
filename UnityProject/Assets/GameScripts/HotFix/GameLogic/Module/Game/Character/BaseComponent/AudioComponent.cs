using UnityEngine;

namespace GameLogic.Game
{
    public class AudioComponent : ComponentBase
    {
        private AudioSource m_audioSource;

        public AudioSource AudioSource
        {
            get { return m_audioSource; }
            set { m_audioSource = value; }
        }

        public void PlayAudio(string audioName)
        {
            // 播放音效
        }

        public void PlayAudioClip(AudioClip clip)
        {
            if (m_audioSource != null && clip != null)
            {
                m_audioSource.PlayOneShot(clip);
            }
        }

        public void Stop()
        {
            if (m_audioSource != null)
            {
                m_audioSource.Stop();
            }
        }
    }
}
