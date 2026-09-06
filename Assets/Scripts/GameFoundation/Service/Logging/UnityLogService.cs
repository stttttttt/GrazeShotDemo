using System;
using GameFoundation.Core;
using UnityEngine;

namespace GameFoundation.Service.Logging
{
    public sealed class UnityLogService : ILogService
    {
        public void Info(string message) => Debug.Log(message);
        public void Warning(string message) => Debug.LogWarning(message);
        public void Error(string message, Exception exception = null)
        {
            if (exception == null) Debug.LogError(message);
            else Debug.LogException(new Exception(message, exception));
        }
    }
}
