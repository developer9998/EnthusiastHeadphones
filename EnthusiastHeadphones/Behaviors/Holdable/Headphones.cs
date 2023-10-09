using GorillaLocomotion;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace EnthusiastHeadphones.Behaviors.Holdable
{
    public class Headphones : DevHoldable
    {
        public bool _isListening = false, _isDetached = true;
        public bool IsPlaying => _activeController != null && _activeController.isPlayingCurrently;

        public EventHandler _onDock, _onDockRemove;

        public GorillaPressableButton _muteButton;
        public readonly List<SynchedMusicController> _musicControllers = new List<SynchedMusicController>();

        private SynchedMusicController _activeController;

        private CultureInfo _cultureInfo;

        private bool _primaryHeld, _lastPrimary;

        private RawImage _playing, _muted;
        private Text _track, _timer;
        private Vector3 _originalPosition, _originalEuler, _originalScale;

        private Vector3
            _leftPosition = new Vector3(-0.04900022f, 0.07400074f, 0.01900004f),
            _leftEuler = new Vector3(-249.059f, 163.566f, 72.127f);

        private Vector3
            _rightPosition = new Vector3(0.04900029f, 0.07400086f, 0.01900008f),
            _rightEuler = new Vector3(-68.592f, -163.978f, 252.512f);

        private LTDescr _posLerp, _rotLerp, _scaleLerp;

        public void Start()
        {
            _cultureInfo = new CultureInfo("en-US");

            if (_originalPosition == default)
            {
                _originalPosition = transform.position;
                _originalEuler = transform.eulerAngles;
                _originalScale = transform.localScale;
            }

            _playing = transform.Find("Screen/Canvas/PlayingCheck").GetComponent<RawImage>();
            _muted = transform.Find("Screen/Canvas/MutedCheck").GetComponent<RawImage>();

            _track = transform.Find("Screen/Canvas/TrackText").GetComponent<Text>();
            _timer = transform.Find("Screen/Canvas/TimerText").GetComponent<Text>();
        }

        public void LateUpdate()
        {
            _activeController = (_activeController == null && _musicControllers.Count > 0) ? _musicControllers.First(a => a.audioSource.enabled && a.audioSource.gameObject.activeSelf) : ((_activeController != null && (!_activeController.audioSource.enabled || !_activeController.audioSource.gameObject.activeSelf)) ? _musicControllers.First(a => a.audioSource.enabled && a.audioSource.gameObject.activeSelf) : _activeController);
            if (InHand)
            {
                _primaryHeld = InLeftHand ? ControllerInputPoller.instance.leftControllerPrimaryButton : ControllerInputPoller.instance.rightControllerPrimaryButton;
                if (_primaryHeld != _lastPrimary && _primaryHeld)
                {
                    _muteButton.onPressButton.Invoke();
                    _muteButton.ButtonActivation();
                    GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(_muteButton.isOn ? 211 : 212, InLeftHand, 0.33f);
                }
                _lastPrimary = _primaryHeld;
                return;
            }
            _lastPrimary = true;
        }

        public void FixedUpdate()
        {
            if (InHand)
            {
                if (_isDetached && Vector3.Distance(transform.position, _originalPosition) > 5f)
                {
                    Respawn();
                    OnDrop(InLeftHand);
                    return;
                }
            }

            if (_playing && _muted)
            {
                _playing.uvRect = IsPlaying ? new Rect(0f, 0f, 0.5f, 1f) : new Rect(0.5f, 0f, 0.5f, 1f);
                _muted.uvRect = _muteButton.isOn ? new Rect(0f, 0f, 0.5f, 1f) : new Rect(0.5f, 0f, 0.5f, 1f);
            }

            if (_activeController && _activeController.gameObject.activeInHierarchy && _timer && _track)
            {
                TimeSpan timeSpan = TimeSpan.FromSeconds((int)((_activeController.songStartTimes[(_activeController.lastPlayIndex + 1) % _activeController.songStartTimes.Length] - _activeController.currentTime) / 1000) + 1f);
                _timer.text = string.Concat((new DateTime() + timeSpan).ToString("mm:ss", _cultureInfo));

                bool usingNewCode = (bool)AccessTools.Field(_activeController.GetType(), "usingNewSyncedSongsCode").GetValue(_activeController);
                SynchedMusicController.SyncedSongInfo[] syncInfo = (SynchedMusicController.SyncedSongInfo[])AccessTools.Field(_activeController.GetType(), "syncedSongs").GetValue(_activeController);
                _track.text = string.Concat("TRACK: ", usingNewCode ? (_activeController.lastPlayIndex < 0 ? "1/1" : $"{_activeController.audioClipsForPlaying[_activeController.lastPlayIndex] + 1}/{syncInfo.Length}") : "1/1");
            }
        }

        private async void Respawn()
        {
            PickUp = false;
            Lerp(_originalPosition, _originalEuler, _originalScale, 0.02f);

            await Task.Delay(Mathf.RoundToInt(0.02f * 1000f));
            PickUp = true;
        }

        private void Lerp(Vector3 position, Vector3 rotation, Vector3 scale, float time = Constants.HoldableLerp)
        {
            if (_posLerp != null && _rotLerp != null)
            {
                LeanTween.cancel(_posLerp.uniqueId);
                LeanTween.cancel(_rotLerp.uniqueId);
                _posLerp = null; _rotLerp = null;
            }

            if (_scaleLerp == null || (_scaleLerp != null && _scaleLerp.to != scale))
            {
                if (_scaleLerp != null) LeanTween.cancel(_scaleLerp.uniqueId);
                _scaleLerp = LeanTween.scale(gameObject, scale, time).setEaseLinear();
            }

            _posLerp = LeanTween.moveLocal(gameObject, position, time).setEaseLinear();
            _rotLerp = LeanTween.rotateLocal(gameObject, rotation, time).setEaseLinear();
        }

        public override void OnGrab(bool isLeft)
        {
            base.OnGrab(isLeft);
            Lerp(isLeft ? _leftPosition : _rightPosition, isLeft ? _leftEuler : _rightEuler, Vector3.one);

            if (_isListening) _onDockRemove.Invoke(isLeft, null);
            _isListening = false;

            HeadphoneHandler.Instance._activeHeadphones = HeadphoneHandler.Instance._activeHeadphones == this ? null : HeadphoneHandler.Instance._activeHeadphones;
        }

        public override void OnDrop(bool isLeft)
        {
            base.OnDrop(isLeft);

            var _position = Player.Instance.headCollider.transform.position;
            var _minimumDistance = Player.Instance.headCollider.radius * 1.7f * Player.Instance.scale;

            bool _listeningDist = Vector3.Distance(_position, transform.position) < _minimumDistance;
            HeadphoneHandler.Instance._activeHeadphones = _listeningDist && HeadphoneHandler.Instance._activeHeadphones == null ? this : (!_listeningDist && HeadphoneHandler.Instance._activeHeadphones == this ? null : HeadphoneHandler.Instance._activeHeadphones);

            _isListening = _listeningDist && HeadphoneHandler.Instance._activeHeadphones == this;
            _isDetached = !_isListening;
            if (_isListening && PickUp)
            {
                _onDock.Invoke(isLeft, null);

                Distance = 0.155f;
                transform.SetParent(GorillaTagger.Instance.offlineVRRig.headMesh.transform);

                Lerp(new Vector3(0f, 0.2203f, 0.0639f), Vector3.zero, gameObject.transform.localScale);
                return;
            }

            Distance = 0.11f * (_muteButton.transform.localScale.x / 0.0854f);

            if (!PickUp) return;
            Lerp(_originalPosition, _originalEuler, _originalScale);
        }
    }
}
