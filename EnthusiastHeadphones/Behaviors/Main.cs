using EnthusiastHeadphones.Behaviors.Holdable;
using GorillaExtensions;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace EnthusiastHeadphones.Behaviors
{
    public class Main : MonoBehaviour, IInitializable
    {
        private AssetLoader _assetLoader;

        private GameObject _headphoneBase;

        private AudioClip _jackIn, _jackOut;

        [Inject]
        public void Construct(AssetLoader assetLoader)
        {
            _assetLoader = assetLoader;
        }

        public async void Initialize()
        {
            gameObject.AddComponent<HeadphoneHandler>();

            _jackIn = await _assetLoader.LoadAsset<AudioClip>("JackIn");
            _jackOut = await _assetLoader.LoadAsset<AudioClip>("JackOut");

            _headphoneBase = await _assetLoader.LoadAsset<GameObject>("Headphone Grabbable");
            _headphoneBase.transform.Find("Screen/Canvas/VersionText").GetComponent<Text>().text = string.Concat("VERSION: ", Constants.Version);

            CreateHeadphones(SceneManager.GetActiveScene());
        }

        private void CreateHeadphones(Scene currentScene)
        {
            List<SynchedMusicController> _syncControllerList = currentScene.GetComponentsInHierarchy<SynchedMusicController>();
            List<GorillaPressableButton> _completedMuteList = new List<GorillaPressableButton>();
            Dictionary<GorillaPressableButton, Headphones> _registeredDict = new Dictionary<GorillaPressableButton, Headphones>();

            for (int i = 0; i < _syncControllerList.Count; i++)
            {
                SynchedMusicController _controller = _syncControllerList[i];

                // Make sure this isn't a duplicate SynchedMusicController
                GorillaPressableButton _muteButton = _controller.muteButton;
                if (_completedMuteList.Contains(_muteButton))
                {
                    HeadphoneHandler.Instance.Register(_controller, _registeredDict[_muteButton]);
                    continue;
                }
                _completedMuteList.Add(_muteButton);

                // Hide the mute button, we're going to use the Headphones component
                _muteButton.gameObject.SetActive(false);
                _muteButton.myText?.gameObject?.SetActive(false);

                GameObject _headphones = Instantiate(_headphoneBase);
                _headphones.transform.position = _muteButton.transform.position + (_muteButton.transform.up * (0.93676814988f * _muteButton.transform.localScale.x)) + (_muteButton.transform.forward * (_controller.name.StartsWith("Basement") ? -1 : 1) * (-0.93676814988f * _muteButton.transform.localScale.x) * Mathf.Clamp01(Mathf.Abs(_muteButton.transform.forward.y * 2f) + 0.38f));
                _headphones.transform.rotation = new Quaternion(0f, _muteButton.transform.rotation.y, 0f, _muteButton.transform.rotation.w);
                _headphones.transform.localScale = Vector3.one * (_muteButton.transform.localScale.x / 0.0854f);

                Headphones _headphoneGrabbable = _headphones.AddComponent<Headphones>();
                _headphoneGrabbable.Distance = 0.11f * (_muteButton.transform.localScale.x / 0.0854f);
                _headphoneGrabbable._muteButton = _muteButton;

                _headphoneGrabbable._onDock = delegate (object isLeftObj, EventArgs args)
                {
                    bool isLeft = (bool)isLeftObj;
                    AudioSource refSource = isLeft ? GorillaTagger.Instance.offlineVRRig.leftHandPlayer : GorillaTagger.Instance.offlineVRRig.rightHandPlayer;
                    refSource.PlayOneShot(_jackIn, 1f);
                };
                _headphoneGrabbable._onDockRemove = delegate (object isLeftObj, EventArgs args)
                {
                    bool isLeft = (bool)isLeftObj;
                    AudioSource refSource = isLeft ? GorillaTagger.Instance.offlineVRRig.leftHandPlayer : GorillaTagger.Instance.offlineVRRig.rightHandPlayer;
                    refSource.PlayOneShot(_jackOut, 1f);
                };

                _registeredDict.Add(_muteButton, _headphoneGrabbable);
                HeadphoneHandler.Instance.Register(_controller, _headphoneGrabbable);
            }
        }
    }
}
