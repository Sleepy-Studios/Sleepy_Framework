using HotUpdate.Base;
using System;
using System.Collections.Generic;
using Core;
using UnityEngine;
using YooAsset;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;

namespace HotUpdate.GameUtils
{
    /// <summary>
    /// 全局音频管理器，负责管理音乐和音效的播放
    /// </summary>
    public class AudioManager : LazyMonoSingleton<AudioManager>
    {
        ///全局单例实例
        public new static AudioManager Instance => LazyMonoSingleton<AudioManager>.Instance;

        #region 字段和属性

        /// <summary>
        /// 音频路径的根目录
        /// </summary>
        private const string AudioRootPath = "Assets/GameRes/Audios";

        /// <summary>
        /// 音效资源目录
        /// </summary>
        private const string SoundPath = "Sound";

        /// <summary>
        /// 音乐资源目录
        /// </summary>
        private const string MusicPath = "Music";

        /// <summary>
        /// 背景音乐播放器
        /// </summary>
        private AudioSource musicPlayer;

        /// <summary>
        /// 音效播放器
        /// </summary>
        private AudioSource soundPlayer;

        /// <summary>
        /// 全局音频监听器
        /// </summary>
        private AudioListener audioListener;

        /// <summary>
        /// 全局背景音乐音量
        /// </summary>
        private float musicVolume = 1f;

        /// <summary>
        /// 音效音量
        /// </summary>
        private float soundVolume = 1f;

        /// <summary>
        /// 背景音乐是否静音
        /// </summary>
        private bool isMusicMute = false;

        /// <summary>
        /// 音效是否静音
        /// </summary>
        private bool isSoundMute = false;

        /// <summary>
        /// 当前播放的背景音乐
        /// </summary>
        private AudioMusicEnum currentMusic = AudioMusicEnum.None;

        /// <summary>
        /// 背景音乐资源句柄缓存
        /// </summary>
        private Dictionary<string, AssetHandle> musicHandleDict = new Dictionary<string, AssetHandle>();

        /// <summary>
        /// 音效资源句柄缓存
        /// </summary>
        private Dictionary<string, AssetHandle> soundHandleDict = new Dictionary<string, AssetHandle>();

        /// <summary>
        /// 临时音频源预制体
        /// </summary>
        private GameObject audioSourcePrefab;

        /// <summary>
        /// 临时音频源对象池初始化标志
        /// </summary>
        private bool audioSourcePoolInitialized = false;

        /// <summary>
        /// 音频GameObject
        /// </summary>
        private GameObject audioGameObject;

        /// <summary>
        /// 音频渐变状态跟踪
        /// </summary>
        private bool isFading = false;

        /// <summary>
        /// 当前渐变操作的取消令牌
        /// </summary>
        private System.Threading.CancellationTokenSource fadeTokenSource;

        #endregion

        #region 初始化和生命周期

        protected override void Awake()
        {
            base.Awake();
            InitAudio();
            LoadConfiguration();
        }

        private void InitAudio()
        {
            // 创建音频根对象
            audioGameObject = this.gameObject;

            // 添加全局音频监听器
            audioListener = audioGameObject.AddComponent<AudioListener>();

            // 创建音乐播放器
            GameObject musicObj = new GameObject("MusicPlayer");
            musicObj.transform.SetParent(audioGameObject.transform);
            musicPlayer = musicObj.AddComponent<AudioSource>();
            musicPlayer.playOnAwake = false;
            musicPlayer.loop = true;
            musicPlayer.volume = musicVolume;

            // 创建音效播放器
            GameObject soundObj = new GameObject("SoundPlayer");
            soundObj.transform.SetParent(audioGameObject.transform);
            soundPlayer = soundObj.AddComponent<AudioSource>();
            soundPlayer.playOnAwake = false;
            soundPlayer.loop = false;
            soundPlayer.volume = soundVolume;

            // 创建音频源预制体用于对象池
            audioSourcePrefab = new GameObject("AudioSourcePool");
            AudioSource audioSource = audioSourcePrefab.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSourcePrefab.SetActive(false);
            GameObject.DontDestroyOnLoad(audioSourcePrefab);
        }

        /// <summary>
        /// 加载音频配置
        /// </summary>
        private void LoadConfiguration()
        {
            // 从PlayerPrefs加载设置
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            soundVolume = PlayerPrefs.GetFloat("SoundVolume", 1f);
            isMusicMute = PlayerPrefs.GetInt("MusicMute", 0) == 1;
            isSoundMute = PlayerPrefs.GetInt("SoundMute", 0) == 1;

            // 应用设置
            SetMusicVolume(musicVolume);
            SetSoundVolume(soundVolume);
            SetMusicMute(isMusicMute);
            SetSoundMute(isSoundMute);
        }

        /// <summary>
        /// 保存音频配置
        /// </summary>
        private void SaveConfiguration()
        {
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("SoundVolume", soundVolume);
            PlayerPrefs.SetInt("MusicMute", isMusicMute ? 1 : 0);
            PlayerPrefs.SetInt("SoundMute", isSoundMute ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 预加载常用音频资源
        /// </summary>
        public void PreloadAudioResources()
        {
            PreloadAudioResourcesAsync().Forget();
        }

        private async UniTaskVoid PreloadAudioResourcesAsync()
        {
            // 预加载主背景音乐
            await LoadMusicClip(AudioMusicEnum.BGM_Main);

            // 预加载常用音效
            await LoadSoundClip(AudioSoundEnum.Button_Click);
            await LoadSoundClip(AudioSoundEnum.UI_Popup);
        }

        #endregion

        #region 音乐控制

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="musicType">音乐类型</param>
        /// <param name="fadeTime">淡入时间(秒)</param>
        /// <param name="isLoop">是否循环播放</param>
        /// <param name="pitch">播放速度倍数</param>
        public void PlayMusic(AudioMusicEnum musicType, float fadeTime = 0.5f, bool isLoop = true, float pitch = 1.0f)
        {
            if (currentMusic == musicType && musicPlayer.isPlaying)
                return;

            currentMusic = musicType;
            if (musicType == AudioMusicEnum.None)
            {
                StopMusic(fadeTime);
                return;
            }

            string musicPath = GetMusicPath(musicType);
            PlayMusicAsync(musicPath, fadeTime, isLoop, pitch).Forget();
        }

        /// <summary>
        /// 通过路径播放背景音乐
        /// </summary>
        /// <param name="musicPath">音乐路径</param>
        /// <param name="fadeTime">淡入时间(秒)</param>
        /// <param name="isLoop">是否循环播放</param>
        /// <param name="pitch">播放速度倍数</param>
        public void PlayMusic(string musicPath, float fadeTime = 0.5f, bool isLoop = true, float pitch = 1.0f)
        {
            PlayMusicAsync(musicPath, fadeTime, isLoop, pitch).Forget();
        }

        private async UniTaskVoid PlayMusicAsync(string musicPath, float fadeTime, bool isLoop = true,
            float pitch = 1.0f)
        {
            // 如果当前有播放的音乐，先淡出
            if (musicPlayer.isPlaying)
            {
                await FadeOutMusic(fadeTime);
                musicPlayer.Stop();
            }

            // 加载音乐资源
            AudioClip clip = await LoadMusicClip(musicPath);
            if (clip != null)
            {
                PlayMusicDirectly(clip, fadeTime, isLoop, pitch);
            }
        }

        private void PlayMusicDirectly(AudioClip clip, float fadeTime, bool isLoop = true, float pitch = 1.0f)
        {
            if (clip == null) return;

            musicPlayer.clip = clip;
            musicPlayer.loop = isLoop;
            musicPlayer.pitch = pitch;
            musicPlayer.volume = isMusicMute ? 0 : 1;
            musicPlayer.Play();

            // 淡入音乐
            if (fadeTime > 0 && !isMusicMute)
            {
                FadeInMusicAsync(fadeTime).Forget();
            }
            else
            {
                musicPlayer.volume = isMusicMute ? 0 : musicVolume;
            }
        }

        /// <summary>
        /// 停止背景音乐
        /// </summary>
        /// <param name="fadeTime">淡出时间(秒)</param>
        public void StopMusic(float fadeTime = 0.5f)
        {
            if (!musicPlayer.isPlaying)
                return;

            if (fadeTime > 0)
            {
                FadeOutMusicAsync(fadeTime).Forget();
            }
            else
            {
                musicPlayer.Stop();
            }

            currentMusic = AudioMusicEnum.None;
        }

        /// <summary>
        /// 暂停背景音乐
        /// </summary>
        /// <param name="fadeTime">淡出时间(秒)</param>
        public void PauseMusic(float fadeTime = 0.5f)
        {
            if (!musicPlayer.isPlaying)
                return;

            if (fadeTime > 0)
            {
                FadeOutMusicAsync(fadeTime, true).Forget();
            }
            else
            {
                musicPlayer.Pause();
            }
        }

        /// <summary>
        /// 恢复背景音乐
        /// </summary>
        /// <param name="fadeTime">淡入时间(秒)</param>
        public void ResumeMusic(float fadeTime = 0.5f)
        {
            if (musicPlayer.isPlaying)
                return;

            musicPlayer.UnPause();

            if (fadeTime > 0)
            {
                FadeInMusicAsync(fadeTime).Forget();
            }
            else
            {
                musicPlayer.volume = isMusicMute ? 0 : musicVolume;
            }
        }

        private async UniTask FadeInMusic(float fadeTime)
        {
            float startVolume = 0;
            float targetVolume = isMusicMute ? 0 : musicVolume;
            float currentTime = 0;

            while (currentTime < fadeTime)
            {
                currentTime += Time.deltaTime;
                musicPlayer.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / fadeTime);
                await UniTask.Yield();
            }

            musicPlayer.volume = targetVolume;
        }

        private async UniTaskVoid FadeInMusicAsync(float fadeTime)
        {
            // 取消正在进行的任何渐变操作
            CancelCurrentFade();

            // 创建新的取消令牌
            fadeTokenSource = new System.Threading.CancellationTokenSource();
            var token = fadeTokenSource.Token;

            isFading = true;

            try
            {
                float startVolume = musicPlayer.volume;
                float targetVolume = isMusicMute ? 0 : musicVolume;
                float currentTime = 0;

                while (currentTime < fadeTime && !token.IsCancellationRequested)
                {
                    currentTime += Time.deltaTime;
                    musicPlayer.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / fadeTime);
                    await UniTask.Yield(token);
                }

                if (!token.IsCancellationRequested)
                {
                    musicPlayer.volume = targetVolume;
                }
            }
            catch (System.OperationCanceledException)
            {
                // 渐变被取消，不做任何处理
            }
            finally
            {
                isFading = false;
            }
        }

        /// <summary>
        /// 取消当前正在进行的任何渐变操作
        /// </summary>
        private void CancelCurrentFade()
        {
            if (isFading && fadeTokenSource != null)
            {
                fadeTokenSource.Cancel();
                fadeTokenSource.Dispose();
                fadeTokenSource = null;
            }
        }

        private async UniTask FadeOutMusic(float fadeTime, bool pause = false)
        {
            float startVolume = musicPlayer.volume;
            float targetVolume = 0;
            float currentTime = 0;

            while (currentTime < fadeTime)
            {
                currentTime += Time.deltaTime;
                musicPlayer.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / fadeTime);
                await UniTask.Yield();
            }

            musicPlayer.volume = targetVolume;
            if (pause)
            {
                musicPlayer.Pause();
            }
            else
            {
                musicPlayer.Stop();
            }
        }

        private async UniTaskVoid FadeOutMusicAsync(float fadeTime, bool pause = false)
        {
            // 取消正在进行的任何渐变操作
            CancelCurrentFade();

            // 创建新的取消令牌
            fadeTokenSource = new System.Threading.CancellationTokenSource();
            var token = fadeTokenSource.Token;

            isFading = true;

            try
            {
                float startVolume = musicPlayer.volume;
                float targetVolume = 0;
                float currentTime = 0;

                while (currentTime < fadeTime && !token.IsCancellationRequested)
                {
                    currentTime += Time.deltaTime;
                    musicPlayer.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / fadeTime);
                    await UniTask.Yield(token);
                }

                if (!token.IsCancellationRequested)
                {
                    musicPlayer.volume = targetVolume;

                    if (pause)
                    {
                        musicPlayer.Pause();
                    }
                    else
                    {
                        musicPlayer.Stop();
                    }
                }
            }
            catch (System.OperationCanceledException)
            {
                // 渐变被取消，不做任何处理
            }
            finally
            {
                isFading = false;
            }
        }

        #endregion

        #region 音效控制

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="soundType">音效类型</param>
        /// <param name="volumeScale">音量缩放</param>
        /// <param name="callback">播放完成回调</param>
        public void PlaySound(AudioSoundEnum soundType, float volumeScale = 1.0f, UnityAction callback = null)
        {
            if (isSoundMute)
            {
                callback?.Invoke();
                return;
            }

            string soundPath = GetSoundPath(soundType);
            PlaySound(soundPath, volumeScale, callback);
        }

        /// <summary>
        /// 通过路径播放音效
        /// </summary>
        /// <param name="soundPath">音效路径</param>
        /// <param name="volumeScale">音量缩放</param>
        /// <param name="callback">播放完成回调</param>
        public void PlaySound(string soundPath, float volumeScale = 1.0f, UnityAction callback = null)
        {
            if (isSoundMute)
            {
                callback?.Invoke();
                return;
            }

            // 使用默认音效播放器播放
            PlaySoundAsync(soundPath, soundPlayer, volumeScale, callback).Forget();
        }

        /// <summary>
        /// 播放3D音效
        /// </summary>
        /// <param name="soundType">音效类型</param>
        /// <param name="position">3D位置</param>
        /// <param name="volumeScale">音量缩放</param>
        /// <param name="callback">播放完成回调</param>
        public void PlaySound3D(AudioSoundEnum soundType, Vector3 position, float volumeScale = 1.0f,
            UnityAction callback = null)
        {
            if (isSoundMute)
            {
                callback?.Invoke();
                return;
            }

            string soundPath = GetSoundPath(soundType);
            PlaySound3D(soundPath, position, volumeScale, callback);
        }

        /// <summary>
        /// 通过路径播放3D音效
        /// </summary>
        /// <param name="soundPath">音效路径</param>
        /// <param name="position">3D位置</param>
        /// <param name="volumeScale">音量缩放</param>
        /// <param name="callback">播放完成回调</param>
        public void PlaySound3D(string soundPath, Vector3 position, float volumeScale = 1.0f,
            UnityAction callback = null)
        {
            if (isSoundMute)
            {
                callback?.Invoke();
                return;
            }

            PlaySound3DAsync(soundPath, position, volumeScale, callback).Forget();
        }

        private async UniTaskVoid PlaySound3DAsync(string soundPath, Vector3 position, float volumeScale,
            UnityAction callback)
        {
            // 确保对象池已初始化
            while (!audioSourcePoolInitialized)
            {
                await UniTask.Yield();
            }

            // 从对象池获取一个临时音频源
            GameObject tempObj = await ObjectPoolManager.Instance.GetPooledObject(audioSourcePrefab);
            tempObj.transform.position = position;

            AudioSource tempAudioSource = tempObj.GetComponent<AudioSource>();
            tempAudioSource.spatialBlend = 1.0f; // 设置为3D音效
            tempAudioSource.rolloffMode = AudioRolloffMode.Linear;
            tempAudioSource.minDistance = 1f;
            tempAudioSource.maxDistance = 20f;

            // 播放音效
            AudioClip clip = await LoadSoundClip(soundPath);
            if (clip == null)
            {
                Log.Error($"Failed to load sound: {soundPath}");
                ObjectPoolManager.Instance.ReturnPooledObject(tempObj);
                callback?.Invoke();
                return;
            }

            // 播放音效
            float actualVolume = soundVolume * volumeScale;
            tempAudioSource.PlayOneShot(clip, actualVolume);

            // 等待播放完成
            await UniTask.Delay(TimeSpan.FromSeconds(clip.length));

            // 归还对象到对象池
            ObjectPoolManager.Instance.ReturnPooledObject(tempObj);

            // 执行回调
            callback?.Invoke();
        }

        private async UniTask PlaySoundAsync(string soundPath, AudioSource audioSource, float volumeScale,
            UnityAction callback)
        {
            // 加载音效资源
            AudioClip clip = await LoadSoundClip(soundPath);
            if (clip == null)
            {
                Log.Error($"Failed to load sound: {soundPath}");
                callback?.Invoke();
                return;
            }

            // 播放音效
            float actualVolume = soundVolume * volumeScale;
            audioSource.PlayOneShot(clip, actualVolume);

            // 等待播放完成
            await UniTask.Delay(TimeSpan.FromSeconds(clip.length));

            // 执行回调
            callback?.Invoke();
        }

        /// <summary>
        /// 停止所有音效
        /// </summary>
        public void StopAllSounds()
        {
            soundPlayer.Stop();
        }

        #endregion

        #region 音量控制

        /// <summary>
        /// 设置音乐音量
        /// </summary>
        /// <param name="volume">音量值(0-1)</param>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (!isMusicMute)
            {
                musicPlayer.volume = musicVolume;
            }

            SaveConfiguration();
        }

        /// <summary>
        /// 设置音效音量
        /// </summary>
        /// <param name="volume">音量值(0-1)</param>
        public void SetSoundVolume(float volume)
        {
            soundVolume = Mathf.Clamp01(volume);
            SaveConfiguration();
        }

        /// <summary>
        /// 获取音乐音量
        /// </summary>
        public float GetMusicVolume()
        {
            return musicVolume;
        }

        /// <summary>
        /// 获取音效音量
        /// </summary>
        public float GetSoundVolume()
        {
            return soundVolume;
        }

        /// <summary>
        /// 设置音乐静音状态
        /// </summary>
        public void SetMusicMute(bool isMute)
        {
            isMusicMute = isMute;
            musicPlayer.volume = isMusicMute ? 0 : musicVolume;
            SaveConfiguration();
        }

        /// <summary>
        /// 设置音效静音状态
        /// </summary>
        public void SetSoundMute(bool isMute)
        {
            isSoundMute = isMute;
            SaveConfiguration();
        }

        /// <summary>
        /// 获取音乐静音状态
        /// </summary>
        public bool IsMusicMute()
        {
            return isMusicMute;
        }

        /// <summary>
        /// 获取音效静音状态
        /// </summary>
        public bool IsSoundMute()
        {
            return isSoundMute;
        }

        #endregion

        #region 资源加载

        /// <summary>
        /// 获取音效资源路径
        /// </summary>
        private string GetSoundPath(AudioSoundEnum soundType)
        {
            return $"{AudioRootPath}/{SoundPath}/{soundType.ToString()}";
        }

        /// <summary>
        /// 获取音乐资源路径
        /// </summary>
        private string GetMusicPath(AudioMusicEnum musicType)
        {
            return $"{AudioRootPath}/{MusicPath}/{musicType.ToString()}";
        }

        /// <summary>
        /// 异步加载音乐片段
        /// </summary>
        private async UniTask<AudioClip> LoadMusicClip(string musicPath)
        {
            // 检查是否已有操作句柄
            if (musicHandleDict.TryGetValue(musicPath, out AssetHandle existingHandle))
            {
                if (existingHandle.IsDone && existingHandle.Status == EOperationStatus.Succeed)
                {
                    return existingHandle.AssetObject as AudioClip;
                }
            }

            // 使用YooAsset加载音乐资源
            AssetHandle handle = YooAssets.LoadAssetAsync<AudioClip>(musicPath);
            await handle;

            if (handle.Status == EOperationStatus.Succeed)
            {
                AudioClip clip = handle.AssetObject as AudioClip;

                // 缓存操作句柄
                musicHandleDict.TryAdd(musicPath, handle);

                return clip;
            }

            Log.Error($"Failed to load music: {musicPath}, error: {handle.LastError}");
            return null;
        }

        /// <summary>
        /// 异步加载音乐片段（通过枚举）
        /// </summary>
        private async UniTask<AudioClip> LoadMusicClip(AudioMusicEnum musicType)
        {
            string musicPath = GetMusicPath(musicType);
            return await LoadMusicClip(musicPath);
        }

        /// <summary>
        /// 异步加载音效片段
        /// </summary>
        private async UniTask<AudioClip> LoadSoundClip(string soundPath)
        {
            // 检查是否已有操作句柄
            if (soundHandleDict.TryGetValue(soundPath, out AssetHandle existingHandle))
            {
                if (existingHandle.IsDone && existingHandle.Status == EOperationStatus.Succeed)
                {
                    return existingHandle.AssetObject as AudioClip;
                }
            }

            // 使用YooAsset加载音效资源
            AssetHandle handle = YooAssets.LoadAssetAsync<AudioClip>(soundPath);
            await handle;

            if (handle.Status == EOperationStatus.Succeed)
            {
                AudioClip clip = handle.AssetObject as AudioClip;

                // 缓存操作句柄
                soundHandleDict.TryAdd(soundPath, handle);

                return clip;
            }

            Log.Error($"Failed to load sound: {soundPath}, error: {handle.LastError}");
            return null;
        }

        /// <summary>
        /// 异步加载音效片段（通过枚举）
        /// </summary>
        private async UniTask<AudioClip> LoadSoundClip(AudioSoundEnum soundType)
        {
            string soundPath = GetSoundPath(soundType);
            return await LoadSoundClip(soundPath);
        }

        #endregion

        #region 释放资源

        /// <summary>
        /// 释放所有音频资源
        /// </summary>
        public void ReleaseAllAudioResources()
        {
            // 停止所有音频播放
            StopMusic(0);
            StopAllSounds();

            // 释放所有音乐资源句柄
            foreach (var handle in musicHandleDict.Values)
            {
                handle.Release();
            }

            musicHandleDict.Clear();

            // 释放所有音效资源句柄
            foreach (var handle in soundHandleDict.Values)
            {
                handle.Release();
            }

            soundHandleDict.Clear();
        }

        /// <summary>
        /// 释放指定音频资源
        /// </summary>
        /// <param name="musicType">音乐类型</param>
        public void ReleaseMusicResource(AudioMusicEnum musicType)
        {
            string musicPath = GetMusicPath(musicType);
            if (musicHandleDict.TryGetValue(musicPath, out AssetHandle handle))
            {
                handle.Release();
                musicHandleDict.Remove(musicPath);
            }
        }

        /// <summary>
        /// 释放指定音效资源
        /// </summary>
        /// <param name="soundType">音效类型</param>
        public void ReleaseSoundResource(AudioSoundEnum soundType)
        {
            string soundPath = GetSoundPath(soundType);
            if (soundHandleDict.TryGetValue(soundPath, out AssetHandle handle))
            {
                handle.Release();
                soundHandleDict.Remove(soundPath);
            }
        }

        protected void OnDestroy()
        {
            ReleaseAllAudioResources();

            // 清理对象池
            if (audioSourcePoolInitialized && ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ClearPool(audioSourcePrefab).Forget();
            }

            // 销毁音频源预制体
            if (audioSourcePrefab != null)
            {
                GameObject.Destroy(audioSourcePrefab);
            }
        }

        #endregion
    }
}