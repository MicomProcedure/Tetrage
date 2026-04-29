using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// MPPMの別プロセス間でVirtualTransportイベントを共有するためのファイルストア。
    /// </summary>
    public sealed class MppmSharedVirtualTransportStore
    {
        #region Serialized Event

        [Serializable]
        public sealed class StoredEvent
        {
            public long sequence;
            public int senderActorNumber;
            public int eventCode;
            public string payloadBase64;
            public string targetActorNumbersCsv;
        }

        #endregion

        #region Fields

        private static readonly object ProcessLock = new();
        private static readonly HashSet<string> ClearedPaths = new();

        private readonly string _eventsPath;
        private long _nextSequence;

        #endregion

        #region Properties

        public string EventsPath => _eventsPath;

        #endregion

        #region Constructor

        /// <summary>
        /// 指定されたイベントログパスを使うストアを作成する。
        /// </summary>
        public MppmSharedVirtualTransportStore(string eventsPath)
        {
            if (string.IsNullOrWhiteSpace(eventsPath))
            {
                throw new ArgumentException("eventsPath is null or empty", nameof(eventsPath));
            }

            _eventsPath = eventsPath;
            EnsureDirectory();
            _nextSequence = ReadLatestSequence();
        }

        #endregion

        #region Public API

        /// <summary>
        /// HostプロセスのPlay開始時に古いイベントログを削除する。
        /// </summary>
        public void ClearOnceForHost()
        {
            lock (ProcessLock)
            {
                if (ClearedPaths.Contains(_eventsPath))
                {
                    return;
                }

                if (File.Exists(_eventsPath))
                {
                    File.Delete(_eventsPath);
                }

                _nextSequence = 0;
                ClearedPaths.Add(_eventsPath);
                Debug.Log($"MppmSharedVirtualTransportStore: イベントログを初期化しました ({_eventsPath})");
            }
        }

        /// <summary>
        /// イベントをログ末尾へ追記する。
        /// </summary>
        public void Append(int senderActorNumber, EventCode code, byte[] payload, int[] targetActorNumbers)
        {
            var storedEvent = new StoredEvent
            {
                sequence = NextSequence(),
                senderActorNumber = senderActorNumber,
                eventCode = (int)code,
                payloadBase64 = Convert.ToBase64String(payload ?? Array.Empty<byte>()),
                targetActorNumbersCsv = BuildTargetCsv(targetActorNumbers)
            };

            string line = JsonUtility.ToJson(storedEvent);
            lock (ProcessLock)
            {
                EnsureDirectory();
                using var stream = new FileStream(_eventsPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream);
                writer.WriteLine(line);
            }
        }

        /// <summary>
        /// 指定sequenceより後のイベントを読み込む。
        /// </summary>
        public IReadOnlyList<StoredEvent> ReadAfter(long lastSequence)
        {
            var result = new List<StoredEvent>();
            if (!File.Exists(_eventsPath))
            {
                return result;
            }

            string[] lines;
            lock (ProcessLock)
            {
                using var stream = new FileStream(_eventsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                lines = reader.ReadToEnd().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            }

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                StoredEvent storedEvent;
                try
                {
                    storedEvent = JsonUtility.FromJson<StoredEvent>(line);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"MppmSharedVirtualTransportStore: イベント行の読み込みに失敗しました: {ex.Message}");
                    continue;
                }

                if (storedEvent != null && storedEvent.sequence > lastSequence)
                {
                    result.Add(storedEvent);
                }
            }

            return result;
        }

        /// <summary>
        /// イベントの送信先に指定Actorが含まれるかを判定する。空ならbroadcast。
        /// </summary>
        public static bool IsTargetForActor(StoredEvent storedEvent, int actorNumber)
        {
            if (storedEvent == null || string.IsNullOrWhiteSpace(storedEvent.targetActorNumbersCsv))
            {
                return true;
            }

            var parts = storedEvent.targetActorNumbersCsv.Split(',');
            foreach (var part in parts)
            {
                if (int.TryParse(part, out var targetActorNumber) && targetActorNumber == actorNumber)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 既定のイベントログパスを作成する。
        /// </summary>
        public static string CreateDefaultEventsPath()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            string root = string.IsNullOrEmpty(projectRoot) ? Application.temporaryCachePath : projectRoot;
            string roomKey = ResolveRoomKey();
            return Path.Combine(root, "Library", "TetrageMppmVirtualTransport", $"{roomKey}.events");
        }

        /// <summary>
        /// MPPMのPlayer名からActorNumberを解決する。
        /// </summary>
        public static bool TryResolveMppmActorNumber(out int actorNumber)
        {
            actorNumber = 1;
            if (!TryGetMultiplayerPlayModePlayerName(out var playerName))
            {
                return false;
            }

            const string prefix = "Player";
            if (!playerName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!int.TryParse(playerName.Substring(prefix.Length), out var playerNumber) || playerNumber < 1)
            {
                return false;
            }

            actorNumber = playerNumber;
            return true;
        }

        /// <summary>
        /// 現在の実行環境がMPPMかを判定する。
        /// </summary>
        public static bool IsMppmProcess()
        {
            if (TryGetMultiplayerPlayModePlayerName(out _))
            {
                return true;
            }

            return TryHasNonEmptyMppmReadOnlyTags();
        }

        /// <summary>
        /// 現在のMPPMプロセスがHost相当かを判定する。
        /// </summary>
        public static bool IsMppmHostProcess()
        {
            return !TryResolveMppmActorNumber(out var actorNumber) || actorNumber == 1;
        }

        #endregion

        #region Private Methods

        private void EnsureDirectory()
        {
            string directory = Path.GetDirectoryName(_eventsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private long NextSequence()
        {
            lock (ProcessLock)
            {
                _nextSequence++;
                return _nextSequence;
            }
        }

        private long ReadLatestSequence()
        {
            long latest = 0;
            foreach (var storedEvent in ReadAfter(0))
            {
                latest = Math.Max(latest, storedEvent.sequence);
            }

            return latest;
        }

        private static string BuildTargetCsv(int[] targetActorNumbers)
        {
            return targetActorNumbers == null || targetActorNumbers.Length == 0
                ? string.Empty
                : string.Join(",", targetActorNumbers);
        }

        private static string ResolveRoomKey()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Math.Abs(projectRoot.GetHashCode()).ToString();
        }

        private static Type ResolveMultiplayerPlaymodeCurrentPlayerType()
        {
            return Type.GetType("Unity.Multiplayer.Playmode.CurrentPlayer, Unity.Multiplayer.Playmode")
                ?? Type.GetType("Unity.Multiplayer.PlayMode.CurrentPlayer, Unity.Multiplayer.PlayMode");
        }

        private static bool TryGetMultiplayerPlayModeMainEditor(out bool isMainEditor)
        {
            isMainEditor = true;
            try
            {
                var currentPlayerType = ResolveMultiplayerPlaymodeCurrentPlayerType();
                var property = currentPlayerType?.GetProperty(
                    "IsMainEditor",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (property?.GetValue(null) is bool value)
                {
                    isMainEditor = value;
                    return true;
                }
            }
            catch
            {
                // MPPMパッケージ未導入時やAPI変更時は通常VirtualTransportとして扱う。
            }

            return false;
        }

        private static bool TryHasNonEmptyMppmReadOnlyTags()
        {
            try
            {
                var currentPlayerType = ResolveMultiplayerPlaymodeCurrentPlayerType();
                var method = currentPlayerType?.GetMethod(
                    "ReadOnlyTags",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method?.Invoke(null, null) is string[] tags)
                {
                    return tags.Length > 0;
                }
            }
            catch
            {
                // MPPMパッケージ未導入時やAPI変更時は通常VirtualTransportとして扱う。
            }

            return false;
        }

        private static bool TryGetMultiplayerPlayModePlayerName(out string playerName)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "-name", StringComparison.OrdinalIgnoreCase))
                {
                    playerName = args[i + 1];
                    return !string.IsNullOrEmpty(playerName);
                }
            }

            playerName = null;
            return false;
        }

        #endregion
    }
}
