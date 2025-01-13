using UnityEngine;

public static class SelectionGUI
{
    private static Texture2D _whiteTexture;

    private static Texture2D WhiteTexture
    {
        get
        {
            if (_whiteTexture == null)
            {
                _whiteTexture = new Texture2D(1, 1);
                _whiteTexture.SetPixel(0, 0, Color.white);
                _whiteTexture.Apply();
            }

            return _whiteTexture;
        }
    }

    public static Rect GetScreenRect(Vector3 pScreenPosition1, Vector3 pScreenPosition2)
    {
        // Move origin from bottom left to top left
        pScreenPosition1.y = Screen.height - pScreenPosition1.y;
        pScreenPosition2.y = Screen.height - pScreenPosition2.y;
        // Calculate corners
        var topLeft = Vector3.Min(pScreenPosition1, pScreenPosition2);
        var bottomRight = Vector3.Max(pScreenPosition1, pScreenPosition2);
        // Create Rect
        return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
    }

    public static void DrawScreenRect(Rect pRect, Color pColor)
    {
        GUI.color = pColor;
        GUI.DrawTexture(pRect, WhiteTexture);
    }

    public static void DrawScreenRectBorder(Rect pRect, float pThickness, Color pColor)
    {
        //Top
        DrawScreenRect(new Rect(pRect.xMin, pRect.yMin, pRect.width, pThickness), pColor);
        // Left
        DrawScreenRect(new Rect(pRect.xMin, pRect.yMin, pThickness, pRect.height), pColor);
        // Right
        DrawScreenRect(new Rect(pRect.xMax - pThickness, pRect.yMin, pThickness, pRect.height), pColor);
        // Bottom
        DrawScreenRect(new Rect(pRect.xMin, pRect.yMax - pThickness, pRect.width, pThickness), pColor);
    }    
}
