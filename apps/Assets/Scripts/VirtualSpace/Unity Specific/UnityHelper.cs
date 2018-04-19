using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VirtualSpace.Shared;

namespace VirtualSpace.Unity.Helper
{
    public static class UnityHelper
    {
        private static void DrawArea(Polygon area, Color color, float duration)
        {
            for (int j = 0; j < area.Points.Count - 1; j++)
            {
                Debug.DrawLine(area.Points[j].ToVector3(), area.Points[j + 1].ToVector3(), color, duration);
            }
            Debug.DrawLine(area.Points[0].ToVector3(), area.Points[area.Points.Count - 1].ToVector3(), color, duration);
        }

        public static void DrawDebugArea(this Polygon me, float duration=1f)
        {
            DrawArea(me, Color.white, duration);
        }

        public static void DrawDebugArea(this Polygon me, Color color, float duration = 1f)
        {
            DrawArea(me, color, duration);
        }

        public static IEnumerator WaitAndDo(float time, Action action)
        {
            yield return new WaitForSeconds(time);
            action();
        }

        public static IEnumerator DoAndWaitRepeat(float waitInBetween, Action action)
        {
            while (true)
            {
                action();
                yield return new WaitForSeconds(waitInBetween);
            }
        }
    }
}