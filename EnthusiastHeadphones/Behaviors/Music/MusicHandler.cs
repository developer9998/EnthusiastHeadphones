using EnthusiastHeadphones.Behaviors.Holdable;
using UnityEngine;

namespace EnthusiastHeadphones.Behaviors.Music
{
    public class MusicHandler : MonoBehaviour
    {
        public GorillaPressableButton _muteButton;

        public AudioSource _currentSource;
        public float _perspectiveVolume = Constants.PerspectiveVolume;

        private AudioSource _backupSource;

        public Headphones _activeHeadphones;

        private MusicSettings _musicSettings;

        public void Start()
        {
            _musicSettings ??= new MusicSettings()
            {
                _originalPosition = _currentSource.transform.position,
                _originalParent = _currentSource.transform.parent,
                _originalVolume = _currentSource.volume
            };
        }

        public void Update()
        {
            if (_activeHeadphones._isListening)
            {
                _currentSource.mute = true;
                _currentSource.volume = 0f;

                _backupSource ??= _currentSource.gameObject.AddComponent<AudioSource>();
                _backupSource.volume = _perspectiveVolume;

                if (_currentSource.isPlaying && _backupSource.clip != _currentSource.clip)
                {
                    _backupSource.clip = _currentSource.clip;
                    _backupSource.time = _currentSource.time;
                    _backupSource.Play();
                }
                else if (!_currentSource.isPlaying && _backupSource.isPlaying)
                {
                    _backupSource.Stop();
                    _backupSource.clip = null;
                }

                _backupSource.mute = _muteButton.isOn;
                _backupSource.enabled = _currentSource.enabled;
                return;
            }

            _currentSource.mute = _muteButton.isOn;
            _currentSource.volume = _musicSettings._originalVolume;
            if (_backupSource != null)
            {
                Destroy(_backupSource);
                _backupSource = null;
            }

            if (_activeHeadphones.InHand)
            {
                if (_currentSource.transform.parent != null) _currentSource.transform.SetParent(null);
                _currentSource.transform.position = _activeHeadphones.transform.position;
                return;
            }

            if (_currentSource.transform.parent == null) _currentSource.transform.SetParent(_musicSettings._originalParent);
            _currentSource.transform.position = _musicSettings._originalPosition;
        }
    }
}
