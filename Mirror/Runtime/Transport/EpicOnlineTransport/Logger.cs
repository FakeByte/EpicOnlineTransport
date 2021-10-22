using Epic.OnlineServices.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EpicTransport {
    public static class Logger {

        public static void EpicDebugLog(LogMessage message) {
            switch (message.Level) {
                case LogLevel.Info:
                    Debug.Log($"Epic Manager: Category - {message.Category} Message - {message.Message}");
                    break;
                case LogLevel.Error:
                    Debug.LogError($"Epic Manager: Category - {message.Category} Message - {message.Message}");
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning($"Epic Manager: Category - {message.Category} Message - {message.Message}");
                    break;
                case LogLevel.Fatal:
                    Debug.LogException(new Exception($"Epic Manager: Category - {message.Category} Message - {message.Message}"));
                    break;
                default:
                    Debug.Log($"Epic Manager: Unknown log processing. Category - {message.Category} Message - {message.Message}");
                    break;
            }
        }
    }
}