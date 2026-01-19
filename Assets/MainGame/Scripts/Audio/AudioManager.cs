using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance = null;
        public static event Action<bool> actionSound;
        private static AudioConfiguration _audioConfiguaration;
        public static bool flagAdShowing = false;

        [SerializeField] AudioContainerSO commonSound;
        [SerializeField] AudioContainerSO musics;
        [SerializeField] AudioSource soundPlayer;
        [SerializeField] AudioSource musicPlayer;
        [SerializeField] AudioSource uiSoundPlayer;

        AudioClip memMusicPlay = null;

        private Dictionary<AudioClip, AudioSource> loopingSounds = new Dictionary<AudioClip, AudioSource>();

        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            _audioConfiguaration = AudioSettings.GetConfiguration();

            //DontDestroyOnLoad(gameObject);

            if (PlayerPrefs.GetInt("FirstTimeEnableAudio", 0) == 0)
            {
                PlayerPrefs.SetInt("GunIO_EnableMusic", 1);
                PlayerPrefs.SetInt("GunIO_EnableSound", 1);
                PlayerPrefs.SetInt("FirstTimeEnableAudio", 1);
            }
        }
#if UNITY_IOS || UNITY_IPHONE
        private void OnApplicationPause(bool pause)
        {
            if (!pause)
            {
                if (flagAdShowing)
                {
                    AudioSettings.Reset(_audioConfiguaration);
                    resetAudio();
                }
                flagAdShowing = false;
            }

        }
#endif

        public void resetAudio()
        {
            Debug.Log("aaaaa resetAudio");
            if (PlayerPrefs.GetInt("GunIO_EnableMusic") != 0)
            {
                Debug.Log("aaaaa resetAudio 1");
                musicPlayer.volume = PlayerPrefs.GetInt("GunIO_EnableMusic");
                if (memMusicPlay != null)
                {
                    PlayMusic(memMusicPlay, PlayerPrefs.GetInt("GunIO_EnableMusic"), true);
                }
            }

            if (PlayerPrefs.GetInt("GunIO_EnableSound") != 0)
            {
                Debug.Log("aaaaa resetAudio 2");
                soundPlayer.volume = PlayerPrefs.GetInt("GunIO_EnableSound");
                uiSoundPlayer.volume = PlayerPrefs.GetInt("GunIO_EnableSound");
            }
        }

        public void PlayOneShot(AudioClip audioClip, float volume, float delay = 0, Transform target = null)
        {
            if (PlayerPrefs.GetInt("GunIO_EnableSound") != 1) return;
            if (audioClip == null) return;
            if (delay == 0)
            {
                float newVolume = volume;
                soundPlayer.PlayOneShot(audioClip, newVolume);
            }
            else
            {
                StartCoroutine(IEDelayPlayOneShot(audioClip, volume, delay, target));
            }
        }

        public void PlayOneShot(string clipName, float volume, float delay = 0, Transform target = null)
        {
            AudioClip clip = commonSound.GetClip(clipName);

            if (clip != null)
            {
                if (delay == 0)
                {
                    float newVolume = volume;
                    soundPlayer.PlayOneShot(clip, newVolume);
                }
                else
                {
                    StartCoroutine(IEDelayPlayOneShot(clip, volume, delay, target));
                }
            }
        }

        private IEnumerator IEDelayPlayOneShot(AudioClip audioClip, float volume, float delay = 0, Transform target = null)
        {
            float timer = delay;
            while (timer > 0)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            float newVolume = volume;
            soundPlayer.PlayOneShot(audioClip, newVolume);
        }

        public void PlayMusic(string clipName, float volume, bool isLoop)
        {
            AudioClip clip = musics.GetClip(clipName);
            if (clip == null) return;
            musicPlayer.clip = clip;
            musicPlayer.loop = isLoop;
            if (isLoop)
            {
                memMusicPlay = clip;
            }
            else
            {
                memMusicPlay = null;
            }
            if (PlayerPrefs.GetInt("GunIO_EnableMusic") != 1) musicPlayer.volume = 0;
            else musicPlayer.volume = volume;
            musicPlayer.Play();
        }

        public void PlayMusic(AudioClip clipName, float volume, bool isLoop)
        {
            if (clipName == null) return;
            musicPlayer.clip = clipName;
            musicPlayer.loop = isLoop;
            if (isLoop)
            {
                memMusicPlay = clipName;
            }
            else
            {
                memMusicPlay = null;
            }
            if (PlayerPrefs.GetInt("GunIO_EnableMusic") != 1) musicPlayer.volume = 0;
            else musicPlayer.volume = volume;
            musicPlayer.Play();
        }

        public void PlaySoundWithLoop(AudioClip clip, float volume)
        {
            if (loopingSounds.ContainsKey(clip))
            {
                return;
            }
            AudioSource newSource = gameObject.AddComponent<AudioSource>();
            newSource.clip = clip;
            if (PlayerPrefs.GetInt("GunIO_EnableSound") != 1) newSource.volume = 0;
            else newSource.volume = volume;
            newSource.loop = true;
            newSource.Play();
            loopingSounds.Add(clip, newSource);
        }

        public void StopSoundLoop(AudioClip audioClip)
        {
            if (loopingSounds.ContainsKey(audioClip))
            {
                AudioSource source = loopingSounds[audioClip];
                source.Stop();
                Destroy(source);
                loopingSounds.Remove(audioClip);
            }
            else
            {
                Debug.LogWarning($"No looping sound found for '{audioClip}'.");
            }
        }

        public void StopAllLoopingSounds()
        {
            foreach (var pair in loopingSounds)
            {
                AudioSource source = pair.Value;
                source.Stop();
                Destroy(source);
            }

            loopingSounds.Clear();
        }

        public void StopAll()
        {
            StopMusic();
            StopSound();
            StopAllLoopingSounds();
        }

        public void StopSound()
        {
            soundPlayer.Stop();
        }

        public void StopMusic()
        {
            musicPlayer.Stop();
        }

        public void ResumeMusic()
        {
            musicPlayer.Play();
        }

        public void EnableMusic(bool status)
        {
            PlayerPrefs.SetInt("GunIO_EnableMusic", status ? 1 : 0);
            if (PlayerPrefs.GetInt("GunIO_EnableMusic") != 1)
            {
                musicPlayer.volume = 0;
            }
            else
            {
                musicPlayer.volume = 1f;
            }
        }

        public void EnableSound(bool status)
        {
            PlayerPrefs.SetInt("GunIO_EnableSound", status ? 1 : 0);
            foreach (var pair in loopingSounds)
            {
                AudioSource source = pair.Value;
                source.volume = PlayerPrefs.GetInt("GunIO_EnableSound");
            }
            actionSound?.Invoke(status);
        }

        public void PlayUIOneShot(string clipName, float volume, float delay = 0, Transform target = null)
        {
            if (PlayerPrefs.GetInt("GunIO_EnableSound") != 1) return;
            AudioClip clip = commonSound.GetClip(clipName);

            if (clip != null)
            {
                if (delay == 0)
                {
                    float newVolume = volume;
                    uiSoundPlayer.PlayOneShot(clip, newVolume);
                }
                else
                {
                    StartCoroutine(IEDelayPlayUIOneShot(clip, volume, delay, target));
                }
            }
        }

        private IEnumerator IEDelayPlayUIOneShot(AudioClip audioClip, float volume, float delay = 0,
            Transform target = null)
        {
            float timer = delay;
            while (timer > 0)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            float newVolume = volume;
            uiSoundPlayer.PlayOneShot(audioClip, newVolume);
        }

        public void PlayUIButtonTapSound()
        {
            PlayUIOneShot("ui_button_tap", 1);
        }

        public void PlayUIButtonCloseSound()
        {
            PlayUIOneShot("ui_popup_close", 1);
        }

        public bool IsEnableSound()
        {
            return PlayerPrefs.GetInt("GunIO_EnableSound") == 1;
        }

        public bool IsEnableMusic()
        {
            return PlayerPrefs.GetInt("GunIO_EnableMusic") == 1;
        }
    }