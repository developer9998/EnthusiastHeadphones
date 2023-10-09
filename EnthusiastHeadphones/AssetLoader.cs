﻿using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;


namespace EnthusiastHeadphones
{
    public class AssetLoader
    {
        private bool _bundleLoaded;
        private AssetBundle _storedBundle;

        private Task _loadingTask = null;
        private Dictionary<string, Object> _loadedObjects;

        private async Task LoadBundle()
        {
            var taskCompletionSource = new TaskCompletionSource<AssetBundle>();

            Stream str = Assembly.GetExecutingAssembly().GetManifestResourceStream("EnthusiastHeadphones.Resources.headphonebundle");
            var request = AssetBundle.LoadFromStreamAsync(str);

            request.completed += operation =>
            {
                var outRequest = operation as AssetBundleCreateRequest;
                taskCompletionSource.SetResult(outRequest.assetBundle);
            };

            _storedBundle = await taskCompletionSource.Task;
            _bundleLoaded = true;
        }

        public async Task<T> LoadAsset<T>(string name) where T : Object
        {
            if (!_bundleLoaded)
            {
                _loadingTask ??= LoadBundle();
                await _loadingTask;
            }

            if (_loadedObjects != null && _loadedObjects.TryGetValue(name, out var _loadedObject))
                return _loadedObject as T;
            _loadedObjects ??= new Dictionary<string, Object>();

            var taskCompletionSource = new TaskCompletionSource<T>();
            var request = _storedBundle.LoadAssetAsync<T>(name);

            request.completed += operation =>
            {
                var outRequest = operation as AssetBundleRequest;
                if (outRequest.asset == null)
                {
                    taskCompletionSource.SetResult(null);
                    return;
                }

                taskCompletionSource.SetResult(outRequest.asset as T);
            };

            var _finishedTask = await taskCompletionSource.Task;
            _loadedObjects.Add(name, _finishedTask);
            return _finishedTask;
        }
    }
}
