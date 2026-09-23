using System;
using System.IO;
using ProjectP.Data;
using UnityEngine;

namespace ProjectP.Core
{
    /// <summary>
    /// SaveData(게임 진행)와 SettingsData(설정)를 디스크에 읽고 쓴다. CLAUDE.md 2장, 4장
    ///
    /// - 저장 위치: Application.persistentDataPath
    /// - 임시 파일에 먼저 쓴 뒤 교체한다. 저장 도중 종료돼도 기존 파일이 깨지지 않는다.
    /// - 손상된 파일은 .bak으로 백업하고 새 데이터로 시작한다.
    ///   세이브 파일 하나 때문에 게임이 켜지지 않는 일을 막기 위함이다.
    /// - 디스크에 쓸 수 없는 경우는 예외를 올려 Boot를 멈춘다.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        private const string SaveFileName = "save.json";
        private const string SettingsFileName = "settings.json";

        private string savePath;
        private string settingsPath;

        /// <summary>현재 게임 진행 데이터. 저장 파일이 없으면 새 SaveData다.</summary>
        public SaveData Data { get; private set; }

        public SettingsData Settings { get; private set; }

        /// <summary>디스크에 저장 파일이 있는지 여부. 타이틀의 이어하기 버튼 활성 판정에 쓴다. 기획서 12.3</summary>
        public bool HasSaveFile => File.Exists(savePath);

        public void Initialize()
        {
            savePath = Path.Combine(Application.persistentDataPath, SaveFileName);
            settingsPath = Path.Combine(Application.persistentDataPath, SettingsFileName);

            Settings = Load<SettingsData>(settingsPath);
            if (Settings == null)
            {
                Settings = new SettingsData();
                SaveSettings(); // 디스크 쓰기 가능 여부 확인을 겸한다.
            }

            Data = Load<SaveData>(savePath) ?? new SaveData();
        }

        /// <summary>현재 게임 진행 데이터를 디스크에 쓴다.</summary>
        public void Save() => WriteAtomic(savePath, JsonUtility.ToJson(Data, true));

        public void SaveSettings() => WriteAtomic(settingsPath, JsonUtility.ToJson(Settings, true));

        /// <summary>진행 데이터를 새로 만들고 바로 저장한다. 설정은 유지한다.</summary>
        public void CreateNewSave()
        {
            Data = new SaveData();
            Save();
        }

        /// <summary>파일이 없으면 null. 손상됐으면 .bak으로 백업한 뒤 null.</summary>
        private static T Load<T>(string path) where T : class
        {
            if (!File.Exists(path)) return null;

            try
            {
                var data = JsonUtility.FromJson<T>(File.ReadAllText(path));
                if (data == null) throw new InvalidDataException("내용이 비어 있습니다.");
                return data;
            }
            catch (Exception e) when (e is ArgumentException || e is InvalidDataException)
            {
                var backupPath = path + ".bak";
                File.Copy(path, backupPath, true);
                File.Delete(path);
                Debug.LogWarning($"[SaveManager] 손상된 파일을 백업하고 새로 시작합니다: {backupPath}\n{e.Message}");
                return null;
            }
        }

        private static void WriteAtomic(string path, string json)
        {
            var tempPath = path + ".tmp";
            File.WriteAllText(tempPath, json);

            if (File.Exists(path)) File.Replace(tempPath, path, null);
            else File.Move(tempPath, path);
        }
    }
}
