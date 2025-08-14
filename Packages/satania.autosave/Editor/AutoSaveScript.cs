using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace satania.runtime.autosave
{
    internal class AutoSaveSetting : ScriptableSingleton<AutoSaveSetting>
    {
        internal double nextTime = 0;
        internal bool isChangedHierarchy = false;
    }

    public class AutoSaveScript : EditorWindow
    {
        private static readonly string autosaveKey = "satania@autosave@toggle";
        private static readonly string intervalTimeKey = "satania@autosave@intervalTime";

        AutoSaveSetting setting => AutoSaveSetting.instance;

        #region methods
        /// <summary>
        /// 同じこと書くの面倒なのでメソッド化
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool GetConfigBool(string key)
        {
            string value = EditorUserSettings.GetConfigValue(key);
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return value.Equals("True");
        }

        public int GetConfigInt(string key)
        {
            string value = EditorUserSettings.GetConfigValue(key);
            if (string.IsNullOrEmpty(value))
            {
                return -1;
            }

            int number = 0;
            if (!int.TryParse(value, out number))
            {
                return -1;
            }

            return number;
        }

        public void SetConfigBool(string key, bool value)
        {
            EditorUserSettings.SetConfigValue(key, value.ToString());
        }

        public void SetConfigInt(string key, int value)
        {
            EditorUserSettings.SetConfigValue(key, value.ToString());
        }
        #endregion

        #region defines
        public bool isAutoSave
        {
            get
            {
                return GetConfigBool(autosaveKey);
            }
            set
            {
                SetConfigBool(autosaveKey, value);
            }
        }

        public int intervalTime
        {
            get
            {
                return GetConfigInt(intervalTimeKey);
            }
            set
            {
                SetConfigInt(intervalTimeKey, value);
            }
        }

        static string[] timeArrayText = { "1分", "5分", "15分", "30分", "60分" };
        static int[] timeArray = { 60 * 1, 60 * 5, 60 * 15, 60 * 30, 60 * 60 };
        #endregion

        #region editor functions
        /// <summary>
        /// エディタ上にテキストを表示します
        /// </summary>
        /// <param name="msg">表示する内容</param>
        /// <param name="size">表示するサイズ</param>
        /// <param name="isBold">太字にするか</param>
        private void DrawSizeLabel(string msg, int size, bool isBold = true, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            GUIStyle RichText = new GUIStyle(EditorStyles.label);
            RichText.richText = true;
            RichText.alignment = anchor;

            if (isBold)
                GUILayout.Label($"<size={size}><b>{msg}</b></size>", RichText);
            else
                GUILayout.Label($"<size={size}>{msg}</size>", RichText);
        }

        void DrawLine(int i_height = 1, int padding = 5)
        {
            GUILayout.Space(padding);
            Rect rect = EditorGUILayout.GetControlRect(false, i_height);
            rect.height = i_height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            GUILayout.Space(padding);
        }

        private void ColorDebugLog(string msg, Color color)
        {
            string colorStr = ColorUtility.ToHtmlStringRGBA(color);
            Debug.Log($"[<color=#{colorStr}>AuoSaveScript</color>]{msg}");
        }
        #endregion

        /// <summary>
        /// エディタのタイトル
        /// </summary>
        public static string EditorTitle = "Unity自動セーブ";

        [MenuItem("さたにあしょっぴんぐ/Unity自動セーブ", priority = 30)]
        private static void Init()
        {
            //ウィンドウのインスタンスを生成
            AutoSaveScript window = GetWindow<AutoSaveScript>();

            //ウィンドウサイズを固定
            window.maxSize = window.minSize = new Vector2(500, 300);

            //タイトルを変更
            window.titleContent = new GUIContent(EditorTitle);
        }

        private void ShowGUI()
        {
            DrawSizeLabel("Unity自動セーブ", 25, true, TextAnchor.MiddleCenter);

            DrawLine();

            isAutoSave = EditorGUILayout.ToggleLeft("自動セーブ", isAutoSave);

            GUILayout.Space(10);

            EditorGUI.BeginChangeCheck();
            GUILayout.Label("自動セーブの間隔");
            intervalTime = EditorGUILayout.Popup(intervalTime, timeArrayText);
            if (EditorGUI.EndChangeCheck())
            {
                setting.nextTime = EditorApplication.timeSinceStartup + timeArray[intervalTime];
                ColorDebugLog($"{timeArray[intervalTime] / 60}分起きに設定しました。", Color.yellow);
            }
        }

        /// <summary>
        /// GUI描画用
        /// </summary>
        public void OnGUI()
        {
            ShowGUI();
        }

        public void OnEnable()
        {
            bool logFlag = false;

            string autosaveValue = EditorUserSettings.GetConfigValue(autosaveKey);
            if (string.IsNullOrEmpty(autosaveValue))
            {
                SetConfigBool(autosaveKey, true);
                logFlag = true;
            }

            string intervalValue = EditorUserSettings.GetConfigValue(intervalTimeKey);
            if (string.IsNullOrEmpty(intervalValue))
            {
                SetConfigInt(intervalTimeKey, 1);
                logFlag = true;
            }

            if (logFlag)
                ColorDebugLog("設定を初期化しました。", Color.yellow);

            setting.nextTime = EditorApplication.timeSinceStartup + timeArray[intervalTime];
            UpdateLoop();
        }

        private void SaveScene()
        {
            UnityEngine.SceneManagement.Scene _scene = EditorSceneManager.GetActiveScene();
            bool saveOK = EditorSceneManager.SaveScene(_scene, _scene.path);

            if (saveOK)
                ColorDebugLog("シーンを保存しました " + System.DateTime.Now, Color.green);
            else
                ColorDebugLog("シーンの保存に失敗しました。 " + System.DateTime.Now, Color.red);
        }

        private void UpdatePlayModeState(PlayModeStateChange state)
        {
            if (isAutoSave && state == PlayModeStateChange.ExitingEditMode)
            {
                SaveScene();
            }
        }

        private void addUpdate()
        {
            if (setting.isChangedHierarchy && EditorApplication.timeSinceStartup > setting.nextTime)
            {
                setting.nextTime = EditorApplication.timeSinceStartup + timeArray[intervalTime];
                if (isAutoSave && !EditorApplication.isPlaying)
                {
                    SaveScene();
                }
                setting.isChangedHierarchy = false;
            }
        }

        private void UpdatehierarchyChanged()
        {
            if (!EditorApplication.isPlaying)
                setting.isChangedHierarchy = true;
        }

        private void UpdateLoop()
        {
            EditorApplication.playModeStateChanged += UpdatePlayModeState;

            EditorApplication.update += addUpdate;

            EditorApplication.hierarchyChanged += UpdatehierarchyChanged;
        }

    }
}
