// <copyright file="BLDebug.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#pragma warning disable CS0436

namespace BovineLabs.Core
{
    using System;
    using System.Diagnostics;
    using BovineLabs.Core.ConfigVars;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    [Configurable]
    public struct BLDebug : IComponentData
    {
        internal const string LogLevelName = "debug.loglevel";
        internal const int LogLevelDefaultValue = (int)LogLevel.Warning;

        [ConfigVar(LogLevelName, LogLevelDefaultValue, "The log level debugging for BovineLabs libraries.")]
        internal static readonly SharedStatic<int> CurrentLogLevel = SharedStatic<int>.GetOrCreate<BLDebug>();

        public static LogLevel Level => (LogLevel)CurrentLogLevel.Data;

        public bool IsValid => !this.World.IsEmpty;

        internal FixedString32Bytes World;

        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        public readonly void Verbose(in FixedString128Bytes msg)
        {
            if (Level >= LogLevel.Verbose)
            {
                UnityEngine.Debug.Log($"V | {this.World} | {msg}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        public readonly void VerboseString(string msg)
        {
            if (Level >= LogLevel.Verbose)
            {
                UnityEngine.Debug.Log($"V | {this.World} | {msg}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [HideInCallstack]
        public readonly void Debug(in FixedString128Bytes msg)
        {
            if (Level >= LogLevel.Debug)
            {
                UnityEngine.Debug.Log($"Debug | {this.World} | {msg}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [HideInCallstack]
        public readonly void DebugLong512(in FixedString512Bytes msg)
        {
            if (Level >= LogLevel.Debug)
            {
                UnityEngine.Debug.Log($"Debug | {this.World} | {msg}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [HideInCallstack]
        public readonly void DebugLong4096(in FixedString4096Bytes msg)
        {
            if (Level >= LogLevel.Debug)
            {
                UnityEngine.Debug.Log($"Debug | {this.World} | {msg}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [HideInCallstack]
        public readonly void DebugString(string msg)
        {
            if (Level >= LogLevel.Debug)
            {
                UnityEngine.Debug.Log($"Debug | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void Info(in FixedString128Bytes msg)
        {
            if (Level >= LogLevel.Info)
            {
                UnityEngine.Debug.Log($"Info  | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void Info512(in FixedString512Bytes msg)
        {
            if (Level >= LogLevel.Info)
            {
                UnityEngine.Debug.Log($"Info  | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void Info(in FixedString4096Bytes msg)
        {
            if (Level >= LogLevel.Info)
            {
                UnityEngine.Debug.Log($"Info  | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void InfoString(string msg)
        {
            if (Level >= LogLevel.Info)
            {
                UnityEngine.Debug.Log($"Info  | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void Warning(in FixedString128Bytes msg)
        {
            if (Level >= LogLevel.Warning)
            {
                UnityEngine.Debug.LogWarning($"Warn  | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void Warning512(in FixedString512Bytes msg)
        {
            if (Level >= LogLevel.Warning)
            {
                UnityEngine.Debug.LogWarning($"Warn  | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void Warning4096(in FixedString4096Bytes msg)
        {
            if (Level >= LogLevel.Warning)
            {
                UnityEngine.Debug.LogWarning($"Warn  | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void WarningString(string msg)
        {
            if (Level >= LogLevel.Warning)
            {
                UnityEngine.Debug.LogWarning($"Warn  | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void Error(in FixedString128Bytes msg)
        {
            if (Level >= LogLevel.Error)
            {
                UnityEngine.Debug.LogError($"Error | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void Error512(in FixedString512Bytes msg)
        {
            if (Level >= LogLevel.Error)
            {
                UnityEngine.Debug.LogError($"Error | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void Error4096(in FixedString4096Bytes msg)
        {
            if (Level >= LogLevel.Error)
            {
                UnityEngine.Debug.LogError($"Error | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void ErrorString(string msg)
        {
            if (Level >= LogLevel.Error)
            {
                UnityEngine.Debug.LogError($"Error | {this.World} | {msg}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        public static void LogVerbose(in FixedString128Bytes msg)
        {
            if (Level >= LogLevel.Verbose)
            {
                UnityEngine.Debug.Log($"V | {msg}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        public static void LogVerboseString(string msg)
        {
            if (Level >= LogLevel.Verbose)
            {
                UnityEngine.Debug.Log($"V | {msg}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [HideInCallstack]
        public static void LogDebug(in FixedString128Bytes msg)
        {
            if (Level >= LogLevel.Debug)
            {
                UnityEngine.Debug.Log($"Debug | {msg}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [HideInCallstack]
        public static void LogDebugLong512(in FixedString512Bytes msg)
        {
            if (Level >= LogLevel.Debug)
            {
                UnityEngine.Debug.Log($"Debug | {msg}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [HideInCallstack]
        public static void LogDebugLong4096(in FixedString4096Bytes msg)
        {
            if (Level >= LogLevel.Debug)
            {
                UnityEngine.Debug.Log($"Debug | {msg}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [HideInCallstack]
        public static void LogDebugString(string msg)
        {
            if (Level >= LogLevel.Debug)
            {
                UnityEngine.Debug.Log($"Debug | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogInfo(in FixedString128Bytes msg)
        {
            if (Level >= LogLevel.Info)
            {
                UnityEngine.Debug.Log($"Info  | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogInfo512(in FixedString512Bytes msg)
        {
            if (Level >= LogLevel.Info)
            {
                UnityEngine.Debug.Log($"Info  | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogInfo(in FixedString4096Bytes msg)
        {
            if (Level >= LogLevel.Info)
            {
                UnityEngine.Debug.Log($"Info  | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogInfoString(string msg)
        {
            if (Level >= LogLevel.Info)
            {
                UnityEngine.Debug.Log($"Info  | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogWarning(in FixedString128Bytes msg)
        {
            if (Level >= LogLevel.Warning)
            {
                UnityEngine.Debug.LogWarning($"Warn  | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogWarning512(in FixedString512Bytes msg)
        {
            if (Level >= LogLevel.Warning)
            {
                UnityEngine.Debug.LogWarning($"Warn  | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogWarning4096(in FixedString4096Bytes msg)
        {
            if (Level >= LogLevel.Warning)
            {
                UnityEngine.Debug.LogWarning($"Warn  | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogWarningString(string msg)
        {
            if (Level >= LogLevel.Warning)
            {
                UnityEngine.Debug.LogWarning($"Warn  | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogError(in FixedString128Bytes msg)
        {
            if (Level >= LogLevel.Error)
            {
                UnityEngine.Debug.LogError($"Error | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogError512(in FixedString512Bytes msg)
        {
            if (Level >= LogLevel.Error)
            {
                UnityEngine.Debug.LogError($"Error | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogError4096(in FixedString4096Bytes msg)
        {
            if (Level >= LogLevel.Error)
            {
                UnityEngine.Debug.LogError($"Error | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogErrorString(string msg)
        {
            if (Level >= LogLevel.Error)
            {
                UnityEngine.Debug.LogError($"Error | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogFatal(Exception ex)
        {
            if (Level >= LogLevel.Fatal)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }
    }
}
