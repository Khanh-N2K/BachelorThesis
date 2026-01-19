using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace N2K
{
    public static class RaycastUtil
    {
        public static bool RaycastUI(Vector2 screenPos, out List<RaycastResult> results)
        {
            results = new List<RaycastResult>();

            PointerEventData data = new PointerEventData(EventSystem.current)
            {
                position = screenPos
            };

            EventSystem.current.RaycastAll(data, results);
            return results.Count > 0;
        }

        public static bool RaycastWorld(Camera cam, Vector2 screenPos, out RaycastHit hit, LayerMask mask)
        {
            Ray ray = cam.ScreenPointToRay(screenPos);
            return Physics.Raycast(ray, out hit, 100f, mask);
        }
    }
}
