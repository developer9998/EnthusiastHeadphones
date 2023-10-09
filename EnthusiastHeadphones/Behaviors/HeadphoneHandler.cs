using EnthusiastHeadphones.Behaviors.Holdable;
using EnthusiastHeadphones.Behaviors.Music;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnthusiastHeadphones.Behaviors
{
    public class HeadphoneHandler : MonoBehaviour
    {
        public static HeadphoneHandler Instance { get; private set; }

        public Headphones _activeHeadphones;

        private GameObject _musicControllerSource;
        private readonly List<AudioSource> _registeredSources = new List<AudioSource>();

        public void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(this);

            _musicControllerSource = new GameObject("EH | HeadphoneHandler");
            _musicControllerSource.transform.SetParent(transform);
        }

        public void Register(SynchedMusicController controller, Headphones headphones)
        {
            headphones._musicControllers.Add(controller);

            void Register(AudioSource audioSource, float audioCount, GorillaPressableButton muteButton)
            {
                if (audioSource == null) return;
                if (_registeredSources.Contains(audioSource)) return;

                _registeredSources.Add(audioSource);
                MusicHandler _handler = _musicControllerSource.AddComponent<MusicHandler>();

                _handler._muteButton = muteButton;
                _handler._activeHeadphones = headphones;
                _handler._currentSource = audioSource;
                _handler._perspectiveVolume = Constants.PerspectiveVolume / audioCount;
            }

            bool audioList = controller.audioSourceArray != null && controller.audioSourceArray.Length > 0;
            Register(controller.audioSource, audioList ? controller.audioSourceArray.Length : 1f, controller.muteButton);

            if (!audioList) return;
            controller.audioSourceArray.ToList().ForEach(a => Register(a, controller.audioSourceArray.Length, controller.muteButton));
        }
    }
}
