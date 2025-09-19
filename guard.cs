// ===============================================
// Util/Guard.cs
// Null checks, defensive helpers (show "?" instead of crash)
// ===============================================

using System;
using System.Collections.Generic;
using ExileCore2.PoEMemory;
using ExileCore2.Shared;

namespace WellCrafted.Util
{
    public static class Guard
    {
        public static string GetElementText(Element element)
        {
            try
            {
                if (element == null || !element.IsVisible) 
                    return null;

                if (!string.IsNullOrEmpty(element.Text))
                    return element.Text.Trim();

                // BFS search for first visible text
                var queue = new Queue<Element>();
                queue.Enqueue(element);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    if (current == null || !current.IsVisible) continue;
                    
                    if (!string.IsNullOrEmpty(current.Text))
                        return current.Text.Trim();

                    var children = current.Children;
                    if (children != null)
                    {
                        for (int i = 0; i < children.Count; i++)
                            queue.Enqueue(children[i]);
                    }
                }
            }
            catch
            {
                // Silent failure
            }
            return null;
        }

        public static ExileCore2.Shared.RectangleF? GetSafeRect(Element element)
        {
            try
            {
                if (element == null) return null;
                ExileCore2.Shared.RectangleF rect = element.GetClientRectCache;
                return rect.Width > 0 && rect.Height > 0 ? rect : (ExileCore2.Shared.RectangleF?)null;
            }
            catch
            {
                return null;
            }
        }

        public static T SafeGet<T>(Func<T> getter, T fallback = default(T))
        {
            try
            {
                return getter();
            }
            catch
            {
                return fallback;
            }
        }

        public static bool IsValidElement(Element element)
        {
            return element != null && element.IsVisible;
        }
    }
}
