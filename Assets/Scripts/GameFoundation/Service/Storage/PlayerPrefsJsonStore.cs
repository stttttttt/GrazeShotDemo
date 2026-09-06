using System;
using GameFoundation.Core;
using UnityEngine;

namespace GameFoundation.Service.Storage
{
    public sealed class PlayerPrefsJsonStore<T> : IDataStore<T> where T : class, new()
    {
        private readonly string _settingsKey;

        public PlayerPrefsJsonStore(string settingsKey)
        {
            if (string.IsNullOrWhiteSpace(settingsKey))
                throw new ArgumentException("设置存储键不能为空。", nameof(settingsKey));
            _settingsKey = settingsKey;
        }

        public T Load()
        {
            if (!PlayerPrefs.HasKey(_settingsKey)) return new T();

            var json = PlayerPrefs.GetString(_settingsKey);
            var data = JsonUtility.FromJson<T>(json);
            return data ?? new T();
        }

        public void Save(T data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            PlayerPrefs.SetString(_settingsKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}
