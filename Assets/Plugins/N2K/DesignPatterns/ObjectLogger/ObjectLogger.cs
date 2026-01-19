using UnityEngine;
using System.Collections.Generic;

namespace N2K
{
    [DisallowMultipleComponent]
    public class ObjectLogger : MonoBehaviour
    {
        [field: Header("Settings")]

        [SerializeField]
        private bool _showLogs = true;

        [field: Header("Data")]

        [SerializeField] 
        [ReadOnly]
        private string _objName = string.Empty;

        private List<string> _logs = new List<string>();

        private void Awake()
        {
            if (string.IsNullOrEmpty(_objName))
                _objName = name;
        }

        public void SetObjName(string name)
        {
            _objName = name;
        }

        #region ====================================== LOGS ===========================================
        public void Log(string msg)
        {
            string coloredName = $"<color=green>{_objName}</color>";
            string line = $"{coloredName} - {msg}";

            if (_showLogs)
                Debug.Log(line);

            _logs.Add(line);
        }

        public void LogWarning(string msg)
        {
            string line = $"<color=yellow>{_objName}</color> - {msg}";
            Debug.LogWarning(line);
            _logs.Add(line);
        }

        public void LogError(string msg)
        {
            string line = $"<color=red>{_objName}</color> - {msg}";
            Debug.LogError(line);
            _logs.Add(line);
        }
        #endregion -----------------------------------------------------------------------------------

        #region ==================================== UNITY EDITOR =====================================
#if UNITY_EDITOR
        public void ShowAllLogs()
        {
            if (_logs.Count == 0)
            {
                Debug.LogWarning($"[{_objName}] No logs recorded.");
                return;
            }

            foreach (string line in _logs)
                Debug.Log(line);
        }

        public void ClearAllLogs()
        {
            _logs.Clear();
            Debug.Log($"[{_objName}] Logs cleared.");
        }
#endif
        #endregion ------------------------------------------------------------------------------------
    }
}