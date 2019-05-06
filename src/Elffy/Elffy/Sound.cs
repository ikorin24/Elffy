using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NAudio.Wave;

namespace Elffy
{
    #region class Sound
    /// <summary>
    /// 単一の音声の再生/停止の機能を提供します。このクラスはスレッドセーフです。
    /// </summary>
    public class Sound : IDisposable
    {
        #region private member
        private bool _isLoaded;
        private WaveStream _reader;
        private WaveChannel32 _pcm;
        private DirectSoundOut _sound;
        private readonly Stack<IDisposable> _disposables = new Stack<IDisposable>();
        private readonly object _sync = new object();
        #endregion

        /// <summary>音声の状態変化時イベント</summary>
        public event EventHandler<SoundStateChangedEventArgs> StateChanged;

        /// <summary>音声の状態</summary>
        public SoundState State => (SoundState)(_sound?.PlaybackState ?? PlaybackState.Stopped);

        #region Volume
        /// <summary>音声のボリューム (0.0 ~ 1.0)</summary>
        public float Volume
        {
            get {
                if(!_isLoaded) { return 0f; }
                return _pcm.Volume;
            }
            set {
                if(!_isLoaded) { return; }
                _pcm.Volume = (value > 1f) ? 1f : (value < 0f) ? 0f : value;
            }
        }
        #endregion

        #region Position
        /// <summary>現在の再生位置</summary>
        public TimeSpan Position
        {
            get {
                if(!_isLoaded) { return TimeSpan.Zero; }
                return _reader.CurrentTime;
            }
            set {
                if(!_isLoaded) { return; }
                _reader.CurrentTime = value;
            }
        }
        #endregion

        #region Load
        /// <summary>音声ファイルを読み込みます</summary>
        /// <param name="filename">ファイル名</param>
        /// <exception cref="NotSupportedException">Not supported file extension type</exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void Load(string filename)
        {
            if(filename == null) { throw new ArgumentNullException(nameof(filename)); }
            var ext = Path.GetExtension(filename);
            Load(File.OpenRead(filename), GetSoundTypeFromExtension(ext));
        }

        /// <summary>音声のストリームを読み込みます</summary>
        /// <param name="stream">ストリーム</param>
        /// <param name="type">ファイルタイプ</param>
        /// <exception cref="NotSupportedException">Not supported file extension type</exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void Load(Stream stream, SoundType type)
        {
            if(stream == null) { throw new ArgumentNullException(nameof(stream)); }
            lock(_sync) {
                _reader = OpenReader(stream, type);
                DisposeAllResource();
                _sound = new DirectSoundOut();
                _pcm = new WaveChannel32(_reader);
                _sound.Init(_pcm);
                _disposables.Push(_reader);
                _disposables.Push(_sound);
                _disposables.Push(_pcm);
                _isLoaded = true;
            }
        }
        #endregion

        #region Play
        /// <summary>音声を再生します。</summary>
        public void Play()
        {
            if(!_isLoaded) { throw new InvalidOperationException(); }
            lock(_sync) {
                var oldState = State;
                _sound.Play();
                RaiseIfStateChanged(oldState, State);
            }
        }
        #endregion

        #region Pause
        /// <summary>音声を一時停止します。</summary>
        public void Pause()
        {
            if(!_isLoaded) { throw new InvalidOperationException(); }
            lock(_sync) {
                var oldState = State;
                _sound.Pause();
                RaiseIfStateChanged(oldState, State);
            }
        }
        #endregion

        #region Stop
        /// <summary>音声を停止します。</summary>
        public void Stop()
        {
            if(!_isLoaded) { throw new InvalidOperationException(); }
            lock(_sync) {
                var oldState = State;
                _sound.Stop();
                RaiseIfStateChanged(oldState, State);
            }
        }
        #endregion

        #region Dispose
        /// <summary>IDisposable.Dispose() の実装</summary>
        public void Dispose()
        {
            lock(_sync) {
                DisposeAllResource();
            }
        }
        #endregion

        #region private Method
        private void RaiseIfStateChanged(SoundState oldState, SoundState newState)
        {
            if(oldState == newState) { return; }
            StateChanged?.Invoke(this, new SoundStateChangedEventArgs(oldState, newState));
        }

        private void DisposeAllResource()
        {
            var oldState = State;
            while(_disposables.Count > 0) {
                _disposables.Pop().Dispose();
            }
            RaiseIfStateChanged(oldState, State);
            _isLoaded = false;
        }

        private WaveStream OpenReader(Stream stream, SoundType type)
        {
            switch(type) {
                case SoundType.Wav:
                    return new WaveFileReader(stream);
                case SoundType.Mp3:
                    return new Mp3FileReader(stream);
                default:
                    throw new NotSupportedException();
            }
        }

        private SoundType GetSoundTypeFromExtension(string extension)
        {
            switch(extension) {
                case ".mp3":
                    return SoundType.Mp3;
                case ".wav":
                    return SoundType.Wav;
                default:
                    throw new NotSupportedException($"'{extension}' is not Supported");
            }
        }
        #endregion private Method
    }
    #endregion class Sound

    #region class SoundStateChangedEventArgs
    /// <summary>音声の状態変化時イベントの引数クラス</summary>
    public class SoundStateChangedEventArgs : EventArgs
    {
        /// <summary>変更前の音声の状態</summary>
        public SoundState OldState { get; }
        /// <summary>変更後の音声の状態</summary>
        public SoundState NewState { get; }
        /// <summary>コンストラクタ</summary>
        /// <param name="oldState">変更前の音声の状態</param>
        /// <param name="newState">変更後の音声の状態</param>
        public SoundStateChangedEventArgs(SoundState oldState, SoundState newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    }
    #endregion

    #region enum SoundState
    /// <summary>音声の状態を表す列挙体</summary>
    public enum SoundState
    {
        /// <summary>停止中</summary>
        Stopped,
        /// <summary>再生中</summary>
        Playing,
        /// <summary>一時停止中</summary>
        Paused,
    }
    #endregion

    #region enum SoundType
    /// <summary>音声ファイルタイプの列挙体</summary>
    public enum SoundType
    {
        Mp3,
        Wav,
    }
    #endregion
}
