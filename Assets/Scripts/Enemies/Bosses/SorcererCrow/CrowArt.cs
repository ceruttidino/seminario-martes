using UnityEngine;

/// <summary>
/// Sprites del Cuervo Hechicero (jefe final).
///
/// PLACEHOLDERS: si existe un sprite en Resources/Bosses/Crow con el mismo nombre se usa ese (asi el
/// equipo de arte puede reemplazarlos sin tocar codigo). Si no existe, se dibuja uno simple por codigo.
/// Nombres: CrowBody, CrowBodyPossessed, CrowOrb, CrowOrbDeflectable, CrowPortal, CrowArm,
///          CrowRavenBlack, CrowRavenWhite.
/// </summary>
public static class CrowArt
{
    private const string ResourceFolder = "Bosses/Crow/";

    private static Sprite body;
    private static Sprite bodyPossessed;
    private static Sprite orb;
    private static Sprite orbDeflectable;
    private static Sprite portal;
    private static Sprite arm;
    private static Sprite ravenBlack;
    private static Sprite ravenWhite;
    private static Sprite pixel;

    public static Sprite Body(bool possessed)
    {
        if (possessed)
        {
            if (bodyPossessed == null)
                bodyPossessed = Load("CrowBodyPossessed") ?? DrawBody(true);
            return bodyPossessed;
        }

        if (body == null)
            body = Load("CrowBody") ?? DrawBody(false);
        return body;
    }

    public static Sprite Orb()
    {
        if (orb == null)
            orb = Load("CrowOrb") ?? DrawOrb(false);
        return orb;
    }

    public static Sprite OrbDeflectable()
    {
        if (orbDeflectable == null)
            orbDeflectable = Load("CrowOrbDeflectable") ?? DrawOrb(true);
        return orbDeflectable;
    }

    public static Sprite Portal()
    {
        if (portal == null)
            portal = Load("CrowPortal") ?? DrawPortal();
        return portal;
    }

    public static Sprite Arm()
    {
        if (arm == null)
            arm = Load("CrowArm") ?? DrawArm();
        return arm;
    }

    public static Sprite Raven(bool white)
    {
        if (white)
        {
            if (ravenWhite == null)
                ravenWhite = Load("CrowRavenWhite") ?? DrawRaven(true);
            return ravenWhite;
        }

        if (ravenBlack == null)
            ravenBlack = Load("CrowRavenBlack") ?? DrawRaven(false);
        return ravenBlack;
    }

    /// <summary>Cuadrado blanco de 1x1 unidad (se usa para avisos / barras).</summary>
    public static Sprite Pixel()
    {
        if (pixel == null)
        {
            PixelCanvas canvas = new PixelCanvas(4, 4);
            canvas.FillRect(0, 0, 3, 3, new Color32(255, 255, 255, 255));
            pixel = canvas.ToSprite(4f, new Vector2(0.5f, 0.5f));
        }

        return pixel;
    }

    private static Sprite Load(string name)
    {
        return Resources.Load<Sprite>(ResourceFolder + name);
    }

    // ---------------------------------------------------------------- dibujo (placeholders)

    private static Sprite DrawBody(bool possessed)
    {
        Color32 wing = possessed ? new Color32(34, 14, 54, 255) : new Color32(20, 20, 32, 255);
        Color32 dark = possessed ? new Color32(52, 24, 78, 255) : new Color32(34, 34, 50, 255);
        Color32 light = possessed ? new Color32(88, 42, 124, 255) : new Color32(58, 58, 82, 255);
        Color32 beak = new Color32(232, 168, 48, 255);
        Color32 eye = possessed ? new Color32(255, 70, 240, 255) : new Color32(250, 244, 214, 255);
        Color32 pupil = possessed ? new Color32(255, 255, 255, 255) : new Color32(208, 28, 28, 255);

        PixelCanvas c = new PixelCanvas(64, 64);

        // Alas / capa
        c.FillEllipse(14f, 28f, 14f, 22f, wing);
        c.FillEllipse(50f, 28f, 14f, 22f, wing);

        // Puntas de plumas (hacia abajo)
        for (int side = 0; side < 2; side++)
        {
            bool right = side == 1;
            c.FillTriangle(P(2f, 22f, right), P(6f, 2f, right), P(12f, 18f, right), wing);
            c.FillTriangle(P(10f, 18f, right), P(16f, 0f, right), P(21f, 16f, right), wing);
            c.FillTriangle(P(18f, 16f, right), P(23f, 2f, right), P(27f, 16f, right), wing);
        }

        // Cuerpo y pecho
        c.FillEllipse(32f, 26f, 15f, 22f, dark);
        c.FillEllipse(32f, 24f, 9f, 14f, light);

        // Cabeza y cresta
        c.FillCircle(32f, 46f, 12f, dark);
        c.FillTriangle(new Vector2(23f, 55f), new Vector2(26f, 63f), new Vector2(30f, 57f), dark);
        c.FillTriangle(new Vector2(29f, 57f), new Vector2(32f, 63f), new Vector2(35f, 57f), dark);
        c.FillTriangle(new Vector2(34f, 57f), new Vector2(38f, 63f), new Vector2(41f, 55f), dark);

        // Pico
        c.FillTriangle(new Vector2(28f, 44f), new Vector2(36f, 44f), new Vector2(32f, 31f), beak);

        // Ojos
        float eyeRadius = possessed ? 4.2f : 3.2f;
        c.FillCircle(26f, 49f, eyeRadius, eye);
        c.FillCircle(38f, 49f, eyeRadius, eye);
        c.FillCircle(26f, 48f, 1.6f, pupil);
        c.FillCircle(38f, 48f, 1.6f, pupil);

        // Garras
        c.FillRect(24, 0, 29, 4, beak);
        c.FillRect(35, 0, 40, 4, beak);

        c.Outline(new Color32(8, 7, 14, 255));
        return c.ToSprite(24f, new Vector2(0.5f, 0.5f));
    }

    private static Vector2 P(float x, float y, bool mirror)
    {
        return new Vector2(mirror ? 63f - x : x, y);
    }

    private static Sprite DrawOrb(bool deflectable)
    {
        const int size = 32;
        PixelCanvas c = new PixelCanvas(size, size);
        Color32 core = new Color32(245, 255, 255, 255);
        Color32 mid = deflectable ? new Color32(255, 232, 120, 255) : new Color32(90, 205, 255, 255);
        Color32 rim = deflectable ? new Color32(255, 140, 40, 235) : new Color32(130, 50, 235, 235);

        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / (size * 0.5f);
                if (d > 1f)
                    continue;

                Color32 color;
                if (d < 0.35f)
                    color = core;
                else if (d < 0.7f)
                    color = Color32.Lerp(core, mid, (d - 0.35f) / 0.35f);
                else
                    color = Color32.Lerp(mid, rim, (d - 0.7f) / 0.3f);

                if (d > 0.88f)
                    color.a = (byte)Mathf.Lerp(color.a, 120f, (d - 0.88f) / 0.12f);

                c.Set(x, y, color);
            }
        }

        return c.ToSprite(32f, new Vector2(0.5f, 0.5f));
    }

    private static Sprite DrawPortal()
    {
        PixelCanvas c = new PixelCanvas(48, 24);
        Color32 ring = new Color32(176, 96, 255, 255);
        Color32 ringDim = new Color32(104, 48, 190, 255);
        Color32 inner = new Color32(24, 8, 40, 240);
        Color32 hole = new Color32(4, 0, 10, 255);

        for (int y = 0; y < 24; y++)
        {
            for (int x = 0; x < 48; x++)
            {
                float dx = (x + 0.5f - 24f) / 24f;
                float dy = (y + 0.5f - 12f) / 12f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > 1f)
                    continue;

                Color32 color;
                if (d > 0.82f)
                    color = (x + y) % 6 < 3 ? ring : ringDim;
                else if (d > 0.45f)
                    color = inner;
                else
                    color = hole;

                c.Set(x, y, color);
            }
        }

        return c.ToSprite(24f, new Vector2(0.5f, 0.5f));
    }

    private static Sprite DrawArm()
    {
        PixelCanvas c = new PixelCanvas(24, 64);
        Color32 dark = new Color32(26, 18, 38, 255);
        Color32 light = new Color32(52, 38, 72, 255);
        Color32 bone = new Color32(232, 222, 190, 255);

        // Antebrazo emplumado
        c.FillRect(7, 0, 16, 40, dark);
        c.FillRect(10, 0, 12, 40, light);
        for (int y = 4; y < 40; y += 6)
            c.FillTriangle(new Vector2(7f, y + 5f), new Vector2(7f, y), new Vector2(3f, y), dark);

        // Mano
        c.FillEllipse(12f, 46f, 8f, 8f, dark);

        // Dedos con garras
        Finger(c, new Vector2(4f, 50f), new Vector2(9f, 52f), new Vector2(6f, 63f), dark, bone);
        Finger(c, new Vector2(9f, 52f), new Vector2(15f, 52f), new Vector2(12f, 63f), dark, bone);
        Finger(c, new Vector2(15f, 52f), new Vector2(20f, 50f), new Vector2(18f, 63f), dark, bone);

        c.Outline(new Color32(8, 6, 12, 255));
        return c.ToSprite(32f, new Vector2(0.5f, 0f));
    }

    private static void Finger(PixelCanvas c, Vector2 baseLeft, Vector2 baseRight, Vector2 tip, Color32 dark, Color32 bone)
    {
        c.FillTriangle(baseLeft, baseRight, tip, dark);
        c.FillTriangle(Vector2.Lerp(baseLeft, tip, 0.65f), Vector2.Lerp(baseRight, tip, 0.65f), tip, bone);
    }

    private static Sprite DrawRaven(bool white)
    {
        Color32 bodyColor = white ? new Color32(240, 240, 250, 255) : new Color32(16, 16, 24, 255);
        Color32 wingColor = white ? new Color32(196, 202, 226, 255) : new Color32(30, 30, 44, 255);
        Color32 beak = new Color32(232, 168, 48, 255);
        Color32 eye = white ? new Color32(210, 30, 30, 255) : new Color32(250, 244, 214, 255);

        PixelCanvas c = new PixelCanvas(28, 18);

        // Cola, cuerpo, cabeza (mira hacia la derecha)
        c.FillTriangle(new Vector2(4f, 9f), new Vector2(4f, 5f), new Vector2(0f, 7f), bodyColor);
        c.FillEllipse(12f, 8f, 9f, 5f, bodyColor);
        c.FillCircle(21f, 10f, 4f, bodyColor);
        c.FillTriangle(new Vector2(24f, 12f), new Vector2(24f, 8f), new Vector2(27.5f, 10f), beak);

        // Ala
        c.FillTriangle(new Vector2(7f, 10f), new Vector2(17f, 10f), new Vector2(10f, 17f), wingColor);

        // Ojo
        c.Set(22, 11, eye);
        c.Set(23, 11, eye);

        c.Outline(white ? new Color32(90, 94, 120, 255) : new Color32(4, 4, 8, 255));
        return c.ToSprite(24f, new Vector2(0.5f, 0.5f));
    }

    // ---------------------------------------------------------------- mini canvas de pixeles

    private sealed class PixelCanvas
    {
        private readonly int width;
        private readonly int height;
        private readonly Color32[] pixels;

        public PixelCanvas(int width, int height)
        {
            this.width = width;
            this.height = height;
            pixels = new Color32[width * height];
        }

        public void Set(int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                return;

            pixels[y * width + x] = color;
        }

        public void FillRect(int x0, int y0, int x1, int y1, Color32 color)
        {
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                    Set(x, y, color);
            }
        }

        public void FillCircle(float cx, float cy, float r, Color32 color)
        {
            FillEllipse(cx, cy, r, r, color);
        }

        public void FillEllipse(float cx, float cy, float rx, float ry, Color32 color)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx));
            int x1 = Mathf.Min(width - 1, Mathf.CeilToInt(cx + rx));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry));
            int y1 = Mathf.Min(height - 1, Mathf.CeilToInt(cy + ry));

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f - cx) / rx;
                    float dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f)
                        pixels[y * width + x] = color;
                }
            }
        }

        public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color32 color)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
            int x1 = Mathf.Min(width - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
            int y1 = Mathf.Min(height - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    if (PointInTriangle(p, a, b, c))
                        pixels[y * width + x] = color;
                }
            }
        }

        /// <summary>Contorno de 1 pixel alrededor de lo dibujado.</summary>
        public void Outline(Color32 color)
        {
            Color32[] copy = (Color32[])pixels.Clone();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (copy[y * width + x].a != 0)
                        continue;

                    if (HasOpaque(copy, x - 1, y) || HasOpaque(copy, x + 1, y) || HasOpaque(copy, x, y - 1) || HasOpaque(copy, x, y + 1))
                        pixels[y * width + x] = color;
                }
            }
        }

        private bool HasOpaque(Color32[] source, int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                return false;

            return source[y * width + x].a != 0;
        }

        public Sprite ToSprite(float pixelsPerUnit, Vector2 pivot)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, width, height), pivot, pixelsPerUnit);
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);

            bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNeg && hasPos);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }
    }
}
