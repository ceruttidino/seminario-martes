using UnityEngine;

/// <summary>
/// Sprites del jefe Slime (el antiguo Sapo Asqueroso). Mismo esquema que CrowArt: si existe un sprite en
/// Resources/Bosses/Slime con el mismo nombre se usa ese (asi el equipo de arte puede reemplazarlos sin tocar codigo);
/// si no existe, se dibuja uno por codigo.
///
/// Nombres: SlimeBodyIdleA, SlimeBodyIdleB, SlimeBodyBlink, SlimeBodyOpen, SlimeBodyAir, SlimePupil, SlimeGlob,
///          SlimePuddle, SlimeTongueTip.
///
/// El cuerpo se dibuja con el pivot ABAJO (los pies): al aplastarse / estirarse queda apoyado en el piso. Los ojos se
/// dibujan sin pupilas: las pupilas son sprites aparte (SlimePupil) que siguen al jugador (ver SlimeBossVisual).
/// </summary>
public static class SlimeArt
{
    public enum BodyPose { IdleA, IdleB, Blink, Open, Air }

    private const string ResourceFolder = "Bosses/Slime/";

    public const float BodyPixelsPerUnit = 48f;
    public const int BodyWidth = 112;
    public const int BodyHeight = 96;

    // Colores de la baba (tambien los usan los efectos).
    public static readonly Color32 Outline = new Color32(18, 50, 28, 255);
    public static readonly Color32 Dark = new Color32(44, 128, 46, 255);
    public static readonly Color32 Base = new Color32(92, 196, 60, 255);
    public static readonly Color32 Light = new Color32(150, 232, 88, 255);
    public static readonly Color32 Bright = new Color32(214, 252, 150, 255);
    public static readonly Color32 Core = new Color32(58, 156, 56, 255);
    public static readonly Color32 Sclera = new Color32(240, 244, 196, 255);
    public static readonly Color32 ScleraShade = new Color32(206, 214, 150, 255);
    public static readonly Color32 MouthDark = new Color32(58, 14, 38, 255);
    public static readonly Color32 Tongue = new Color32(196, 84, 112, 255);
    public static readonly Color32 Tooth = new Color32(246, 240, 208, 255);

    private const float BaseY = 7f;

    private static Sprite idleA;
    private static Sprite idleB;
    private static Sprite blink;
    private static Sprite open;
    private static Sprite air;
    private static Sprite pupil;
    private static Sprite glob;
    private static Sprite puddle;
    private static Sprite tongueTip;

    public static Sprite Body(BodyPose pose)
    {
        switch (pose)
        {
            case BodyPose.IdleB:
                if (idleB == null) idleB = Load("SlimeBodyIdleB") ?? DrawBody(pose);
                return idleB;
            case BodyPose.Blink:
                if (blink == null) blink = Load("SlimeBodyBlink") ?? DrawBody(pose);
                return blink;
            case BodyPose.Open:
                if (open == null) open = Load("SlimeBodyOpen") ?? DrawBody(pose);
                return open;
            case BodyPose.Air:
                if (air == null) air = Load("SlimeBodyAir") ?? DrawBody(pose);
                return air;
            default:
                if (idleA == null) idleA = Load("SlimeBodyIdleA") ?? DrawBody(pose);
                return idleA;
        }
    }

    /// <summary>Forma de cada pose: cuanto se estira, cuanto se ensancha y si lleva el charquito de abajo.</summary>
    public static void PoseShape(BodyPose pose, out float stretch, out float widen, out bool pool)
    {
        stretch = 1f;
        widen = 1f;
        pool = true;

        switch (pose)
        {
            case BodyPose.IdleB: stretch = 0.94f; widen = 1.035f; break;
            case BodyPose.Open: stretch = 1.02f; break;
            case BodyPose.Air: stretch = 1.08f; widen = 0.93f; pool = false; break;
        }
    }

    /// <summary>Centro de un ojo, en unidades de mundo medidas desde el pivot del cuerpo (los pies), para esa pose.</summary>
    public static Vector2 EyeCenter(BodyPose pose, bool right)
    {
        PoseShape(pose, out float stretch, out float widen, out _);
        float x = (right ? 17f : -17f) * widen / BodyPixelsPerUnit;
        float y = (BaseY + 40f * stretch) / BodyPixelsPerUnit;
        return new Vector2(x, y);
    }

    /// <summary>Centro de la boca, en unidades de mundo desde el pivot del cuerpo (los pies), para esa pose.</summary>
    public static Vector2 MouthCenter(BodyPose pose)
    {
        PoseShape(pose, out float stretch, out _, out _);
        return new Vector2(0f, (BaseY + 17f * stretch) / BodyPixelsPerUnit);
    }

    public static Sprite Pupil()
    {
        if (pupil == null)
            pupil = Load("SlimePupil") ?? DrawPupil();
        return pupil;
    }

    public static Sprite Glob()
    {
        if (glob == null)
            glob = Load("SlimeGlob") ?? DrawGlob();
        return glob;
    }

    public static Sprite Puddle()
    {
        if (puddle == null)
            puddle = Load("SlimePuddle") ?? DrawPuddle();
        return puddle;
    }

    public static Sprite TongueTip()
    {
        if (tongueTip == null)
            tongueTip = Load("SlimeTongueTip") ?? DrawTongueTip();
        return tongueTip;
    }

    private static Sprite Load(string name)
    {
        return Resources.Load<Sprite>(ResourceFolder + name);
    }

    // ================================================================= cuerpo

    private static Sprite DrawBody(BodyPose pose)
    {
        PoseShape(pose, out float stretch, out float widen, out bool pool);

        PixelCanvas c = new PixelCanvas(BodyWidth, BodyHeight);
        const float cx = 56f;

        // --- silueta ---------------------------------------------------------------------
        bool[,] mask = new bool[BodyWidth, BodyHeight];
        for (int y = 0; y < BodyHeight; y++)
        {
            for (int x = 0; x < BodyWidth; x++)
            {
                float px = x + 0.5f;
                float yl = (y + 0.5f - BaseY) / stretch;
                if (yl < 0f)
                    continue;

                // Borde de abajo irregular: la baba se va "desparramando".
                float wobble = 1.2f + 1.3f * Mathf.Sin(px * 0.31f) + 0.7f * Mathf.Sin(px * 0.83f + 1f);
                if (yl < wobble)
                    continue;

                bool inside =
                    InEllipse(px, yl, cx, 25f, 46f * widen, 30f) ||
                    InEllipse(px, yl, cx, 13f, 51f * widen, 15f) ||
                    InEllipse(px, yl, cx - 20f * widen, 43f, 20f * widen, 17f) ||
                    InEllipse(px, yl, cx + 22f * widen, 40f, 18f * widen, 15f) ||
                    InEllipse(px, yl, cx + 5f * widen, 55f, 24f * widen, 20f);

                mask[x, y] = inside;
            }
        }

        // --- charco debajo (la baba que se desparrama en el piso) ---------------------------
        if (pool)
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < BodyWidth; x++)
                {
                    float dx = (x + 0.5f - cx) / (53f * widen);
                    float dy = (y + 0.5f - 3.6f) / 4.4f;
                    if (dx * dx + dy * dy <= 1f && !mask[x, y])
                        c.Set(x, y, y < 2 ? Dark : Core);
                }
            }
        }

        // --- distancia al borde (para rim / brillo) ------------------------------------------
        int[,] edge = new int[BodyWidth, BodyHeight];
        const int maxEdge = 9;
        for (int y = 0; y < BodyHeight; y++)
        {
            for (int x = 0; x < BodyWidth; x++)
            {
                if (!mask[x, y])
                    continue;

                int best = maxEdge;
                for (int dy = -maxEdge; dy <= maxEdge && best > 1; dy++)
                {
                    for (int dx = -maxEdge; dx <= maxEdge; dx++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        bool inside = nx >= 0 && ny >= 0 && nx < BodyWidth && ny < BodyHeight && mask[nx, ny];
                        if (inside)
                            continue;

                        int d = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                        if (d < best)
                            best = d;
                    }
                }

                edge[x, y] = best;
            }
        }

        // --- sombreado -------------------------------------------------------------------------
        Vector3 light = new Vector3(-0.55f, 0.65f, 0.52f).normalized;
        for (int y = 0; y < BodyHeight; y++)
        {
            for (int x = 0; x < BodyWidth; x++)
            {
                if (!mask[x, y])
                    continue;

                float nx = (x + 0.5f - cx) / (52f * widen);
                float ny = ((y + 0.5f - BaseY) / stretch - 32f) / 46f;
                float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - nx * nx - ny * ny));
                float dot = nx * light.x + ny * light.y + nz * light.z;
                float lambert = Mathf.Max(0f, dot);

                // Luz "envolvente" (el lado de la sombra no queda negro): se siente translucido, como gelatina.
                float wrapped = Mathf.Clamp01((dot + 0.45f) / 1.45f);
                float value = 0.30f + 0.70f * wrapped;

                int tone = value < 0.40f ? 0 : value < 0.62f ? 1 : value < 0.84f ? 2 : 3;
                int e = edge[x, y];

                // Borde de abajo oscuro; borde de arriba a la izquierda brillante (aspecto gelatinoso).
                if (e <= 2 && ny < 0.05f)
                    tone = 0;
                else if (e >= 3 && e <= 5 && lambert > 0.62f && ny > 0.1f)
                    tone = Mathf.Max(tone, 3);
                else if (e == 3 && nx > 0.55f && ny < 0.1f)
                    tone = Mathf.Max(tone, 2); // luz que rebota en el borde de abajo a la derecha

                // "Nucleo" un poco mas oscuro adentro: da profundidad.
                float cdx = (x + 0.5f - (cx + 6f)) / 22f;
                float cdy = ((y + 0.5f - BaseY) / stretch - 21f) / 11f;
                if (cdx * cdx + cdy * cdy <= 1f && tone >= 2 && e > 4)
                    tone -= 1;

                c.Set(x, y, ToneColor(tone));
            }
        }

        // --- burbujas dentro del cuerpo -----------------------------------------------------------
        Bubble(c, mask, 28f, Y(24f, stretch), 5.5f);
        Bubble(c, mask, 85f, Y(34f, stretch), 4.2f);
        Bubble(c, mask, 66f, Y(13f, stretch), 3.2f);
        Bubble(c, mask, 40f, Y(11f, stretch), 2.4f);

        // --- brillos -------------------------------------------------------------------------------
        FillRotatedEllipse(c, mask, cx - 25f * widen, Y(54f, stretch), 11f, 5.2f, -38f, new Color32(232, 255, 206, 255));
        c.FillEllipseMasked(mask, cx - 11f * widen, Y(62f, stretch), 2.3f, 2.3f, new Color32(252, 255, 240, 255));

        // --- cara -----------------------------------------------------------------------------------
        bool closed = pose == BodyPose.Blink;
        DrawEye(c, cx - 17f * widen, Y(40f, stretch), false, closed, pose == BodyPose.Air);
        DrawEye(c, cx + 17f * widen, Y(40f, stretch), true, closed, pose == BodyPose.Air);
        DrawMouth(c, cx, Y(17f, stretch), pose);

        if (pose == BodyPose.Air)
        {
            // Chorreones colgando abajo mientras esta en el aire.
            Drip(c, 30f, 8f, 9);
            Drip(c, 58f, 8f, 13);
            Drip(c, 86f, 8f, 7);
        }

        c.Outline(Outline);
        return c.ToSprite(BodyPixelsPerUnit, new Vector2(0.5f, 0f));
    }

    private static float Y(float yl, float stretch)
    {
        return BaseY + yl * stretch;
    }

    private static Color32 ToneColor(int tone)
    {
        switch (tone)
        {
            case 0: return Dark;
            case 1: return Base;
            case 2: return Light;
            default: return Bright;
        }
    }

    private static void Bubble(PixelCanvas c, bool[,] mask, float cx, float cy, float r)
    {
        // Solo se dibuja si queda entera adentro del cuerpo.
        int x0 = Mathf.RoundToInt(cx);
        int y0 = Mathf.RoundToInt(cy);
        int ir = Mathf.CeilToInt(r) + 1;
        for (int dy = -ir; dy <= ir; dy++)
        {
            for (int dx = -ir; dx <= ir; dx++)
            {
                int x = x0 + dx;
                int y = y0 + dy;
                if (x < 0 || y < 0 || x >= BodyWidth || y >= BodyHeight || !mask[x, y])
                    return;
            }
        }

        for (int dy = -ir; dy <= ir; dy++)
        {
            for (int dx = -ir; dx <= ir; dx++)
            {
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > r)
                    continue;

                Color32 color = d > r - 1.1f ? Dark : Light;
                c.Set(x0 + dx, y0 + dy, color);
            }
        }

        // Reflejo de la burbuja.
        c.Set(x0 - Mathf.RoundToInt(r * 0.35f), y0 + Mathf.RoundToInt(r * 0.35f), new Color32(250, 255, 236, 255));
    }

    private static void DrawEye(PixelCanvas c, float ex, float ey, bool right, bool closed, bool wide)
    {
        float rx = wide ? 11.5f : 10.5f;
        float ry = wide ? 12.5f : 11.5f;
        float side = right ? 1f : -1f;

        if (closed)
        {
            // Ojo cerrado: una curva oscura con una pestaña.
            for (int x = Mathf.RoundToInt(ex - rx); x <= Mathf.RoundToInt(ex + rx); x++)
            {
                float t = (x - ex) / rx;
                float y = ey - 1f - 3.2f * Mathf.Sqrt(Mathf.Max(0f, 1f - t * t)) + t * side * 1.2f;
                c.Set(x, Mathf.RoundToInt(y), Outline);
                c.Set(x, Mathf.RoundToInt(y) + 1, Outline);
            }

            return;
        }

        // Esclerotica (blanco amarillento) con sombra abajo.
        for (int y = Mathf.FloorToInt(ey - ry) - 1; y <= Mathf.CeilToInt(ey + ry) + 1; y++)
        {
            for (int x = Mathf.FloorToInt(ex - rx) - 1; x <= Mathf.CeilToInt(ex + rx) + 1; x++)
            {
                float dx = (x + 0.5f - ex) / rx;
                float dy = (y + 0.5f - ey) / ry;
                float d = dx * dx + dy * dy;
                if (d > 1f)
                    continue;

                Color32 color = dy < -0.35f ? ScleraShade : Sclera;
                if (d > 0.78f)
                    color = Outline;
                c.Set(x, y, color);
            }
        }

        // Parpado pesado: tapa la parte de arriba del ojo con una inclinacion enojada (mas bajo hacia el centro).
        for (int y = Mathf.FloorToInt(ey - ry) - 1; y <= Mathf.CeilToInt(ey + ry) + 2; y++)
        {
            for (int x = Mathf.FloorToInt(ex - rx) - 2; x <= Mathf.CeilToInt(ex + rx) + 2; x++)
            {
                float dx = (x + 0.5f - ex) / (rx + 0.6f);
                float dy = (y + 0.5f - ey) / (ry + 0.6f);
                if (dx * dx + dy * dy > 1f)
                    continue;

                // 'inward' va de 0 (borde de afuera del ojo) a 1 (borde hacia el centro de la cara).
                float inward = (x + 0.5f - ex) * -side / rx * 0.5f + 0.5f;
                float lidY = ey + 5.5f - inward * 9.5f;
                if (y + 0.5f > lidY)
                    c.Set(x, y, Mathf.Abs(y + 0.5f - lidY) < 1.2f ? Outline : Dark);
            }
        }

        // Ceja gruesa encima del parpado.
        for (int i = -12; i <= 12; i++)
        {
            float inward = (i * -side / 12f) * 0.5f + 0.5f;
            int x = Mathf.RoundToInt(ex + i);
            int y = Mathf.RoundToInt(ey + 8.5f - inward * 9.5f);
            c.Set(x, y, Outline);
            c.Set(x, y + 1, Outline);
            c.Set(x, y + 2, Dark);
        }
    }

    private static void DrawMouth(PixelCanvas c, float cx, float cy, BodyPose pose)
    {
        if (pose == BodyPose.Open)
        {
            // Boca abierta de par en par: interior oscuro, dientes arriba, lengua abajo y baba colgando.
            for (int y = Mathf.FloorToInt(cy - 12f); y <= Mathf.CeilToInt(cy + 10f); y++)
            {
                for (int x = Mathf.FloorToInt(cx - 20f); x <= Mathf.CeilToInt(cx + 20f); x++)
                {
                    float dx = (x + 0.5f - cx) / 19f;
                    float dy = (y + 0.5f - (cy - 1f)) / 10.5f;
                    float d = dx * dx + dy * dy;
                    if (d > 1f)
                        continue;

                    c.Set(x, y, d > 0.8f ? Outline : MouthDark);
                }
            }

            // Lengua.
            for (int y = Mathf.FloorToInt(cy - 10f); y <= Mathf.CeilToInt(cy - 3f); y++)
            {
                for (int x = Mathf.FloorToInt(cx - 11f); x <= Mathf.CeilToInt(cx + 13f); x++)
                {
                    float dx = (x + 0.5f - (cx + 2f)) / 11.5f;
                    float dy = (y + 0.5f - (cy - 9.5f)) / 5.5f;
                    if (dx * dx + dy * dy <= 1f)
                        c.Set(x, y, Tongue);
                }
            }

            // Dientes de arriba (triangulitos).
            for (int i = -3; i <= 3; i++)
            {
                int tx = Mathf.RoundToInt(cx + i * 5.2f);
                int topY = Mathf.RoundToInt(cy + 8.5f - Mathf.Abs(i) * 0.9f);
                for (int h = 0; h < 4; h++)
                {
                    int half = Mathf.Max(0, 2 - h / 2);
                    for (int dx = -half; dx <= half; dx++)
                        c.Set(tx + dx, topY - h, Tooth);
                }
            }

            // Baba que cuelga del labio de abajo.
            DrawStrand(c, cx - 9f, cy - 11f, 9);
            DrawStrand(c, cx + 11f, cy - 11f, 6);
            return;
        }

        // Boca cerrada: linea ondulada de "asco" con dos colmillos y un hilo de baba.
        for (int x = Mathf.RoundToInt(cx - 19f); x <= Mathf.RoundToInt(cx + 19f); x++)
        {
            float t = (x - cx) / 19f;
            float y = cy + 1.8f * Mathf.Sin((x - cx) * 0.55f) - 2.2f * (1f - t * t);
            int yi = Mathf.RoundToInt(y);
            c.Set(x, yi + 2, Dark);
            c.Set(x, yi + 1, Outline);
            c.Set(x, yi, Outline);
            c.Set(x, yi - 1, Light); // labio de abajo con un reflejo: separa la boca del cuerpo
        }

        for (int side = -1; side <= 1; side += 2)
        {
            int fx = Mathf.RoundToInt(cx + side * 13f);
            int fy = Mathf.RoundToInt(cy - 3f);
            for (int h = 0; h < 4; h++)
            {
                c.Set(fx, fy - h, Tooth);
                if (h < 2)
                    c.Set(fx + side, fy - h, Tooth);
            }
        }

        DrawStrand(c, cx + 5f, cy - 4f, 8);
    }

    private static void DrawStrand(PixelCanvas c, float x, float y, int length)
    {
        int xi = Mathf.RoundToInt(x);
        int yi = Mathf.RoundToInt(y);
        for (int i = 0; i < length; i++)
        {
            c.Set(xi, yi - i, Light);
            c.Set(xi + 1, yi - i, Base);
        }

        // Gota al final del hilo.
        c.Set(xi, yi - length, Bright);
        c.Set(xi + 1, yi - length, Light);
        c.Set(xi, yi - length - 1, Base);
        c.Set(xi + 1, yi - length - 1, Dark);
    }

    private static void Drip(PixelCanvas c, float x, float y, int length)
    {
        int xi = Mathf.RoundToInt(x);
        int yi = Mathf.RoundToInt(y);
        for (int i = 0; i < length; i++)
        {
            int width = i < length - 3 ? 1 : 2;
            for (int dx = -width; dx <= width; dx++)
                c.Set(xi + dx, yi - 1 - i, dx == -width ? Base : (dx == width ? Dark : Light));
        }

        c.Set(xi, yi - length, Dark);
    }

    // ================================================================= pupila

    private static Sprite DrawPupil()
    {
        const int size = 9;
        PixelCanvas c = new PixelCanvas(size, size);
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (d <= 4.2f)
                    c.Set(x, y, d > 3.4f ? new Color32(8, 8, 14, 255) : new Color32(20, 20, 32, 255));
            }
        }

        c.Set(2, 6, new Color32(250, 250, 250, 255));
        c.Set(3, 6, new Color32(250, 250, 250, 255));
        c.Set(2, 5, new Color32(210, 210, 220, 255));
        return c.ToSprite(BodyPixelsPerUnit, new Vector2(0.5f, 0.5f));
    }

    // ================================================================= moco (proyectil)

    private static Sprite DrawGlob()
    {
        const int size = 36;
        PixelCanvas c = new PixelCanvas(size, size);
        float center = (size - 1) * 0.5f;

        // Gotitas alrededor (se ven girar junto con el moco).
        SmallBlob(c, 4f, 18f, 2.6f);
        SmallBlob(c, 31f, 23f, 2.4f);
        SmallBlob(c, 21f, 3.5f, 2.2f);
        SmallBlob(c, 9f, 31f, 1.8f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - (center + 0.5f);
                float dy = y + 0.5f - (center + 0.5f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > 11.5f)
                    continue;

                float nx = dx / 11.5f;
                float ny = dy / 11.5f;
                float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - nx * nx - ny * ny));
                float lambert = Mathf.Max(0f, nx * -0.5f + ny * 0.6f + nz * 0.62f);
                int tone = lambert < 0.35f ? 0 : lambert < 0.62f ? 1 : lambert < 0.84f ? 2 : 3;
                c.Set(x, y, ToneColor(tone));
            }
        }

        c.FillCircle(center - 4f, center + 5f, 2.6f, new Color32(240, 255, 220, 255));
        c.Outline(Outline);
        return c.ToSprite(32f, new Vector2(0.5f, 0.5f));
    }

    private static void SmallBlob(PixelCanvas c, float x, float y, float r)
    {
        c.FillCircle(x, y, r, Base);
        c.FillCircle(x - r * 0.25f, y + r * 0.3f, r * 0.55f, Light);
    }

    // ================================================================= charco

    private static Sprite DrawPuddle()
    {
        const int size = 72;
        PixelCanvas c = new PixelCanvas(size, size);
        float center = (size - 1) * 0.5f;

        bool[,] mask = new bool[size, size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - center - 0.5f) / center;
                float dy = (y + 0.5f - center - 0.5f) / center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx);
                float radius = 0.8f + 0.07f * Mathf.Sin(angle * 3f + 1f) + 0.05f * Mathf.Sin(angle * 5f + 2f)
                               + 0.03f * Mathf.Sin(angle * 9f);
                if (d <= radius)
                {
                    mask[x, y] = true;
                    float k = d / radius;

                    // Manchas claras irregulares (ruido suave) en vez de un patron parejo.
                    float noise = ValueNoise(x * 0.11f, y * 0.11f) * 0.65f + ValueNoise(x * 0.23f + 7f, y * 0.23f + 3f) * 0.35f;
                    int tone = k > 0.84f ? 0 : (noise > 0.62f ? 2 : (noise < 0.30f ? 0 : 1));
                    if (k <= 0.84f && noise > 0.82f)
                        tone = 3;
                    c.Set(x, y, ToneColor(tone));
                }
            }
        }

        // Burbujas y destellos.
        Bubble(c, mask, 24f, 40f, 4.5f);
        Bubble(c, mask, 46f, 30f, 3.6f);
        Bubble(c, mask, 36f, 52f, 2.8f);
        Bubble(c, mask, 50f, 46f, 2.2f);
        for (int i = 0; i < 6; i++)
        {
            int gx = 18 + i * 7;
            int gy = 20 + (i * 11) % 30;
            if (gx < size && gy < size && mask[gx, gy])
            {
                c.Set(gx, gy, Bright);
                c.Set(gx + 1, gy, Bright);
            }
        }

        c.Outline(Outline);
        return c.ToSprite(32f, new Vector2(0.5f, 0.5f));
    }

    // ================================================================= punta de la lengua (tentaculo)

    private static Sprite DrawTongueTip()
    {
        const int size = 22;
        PixelCanvas c = new PixelCanvas(size, size);
        float center = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - (center + 0.5f);
                float dy = y + 0.5f - (center + 0.5f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > 8.6f)
                    continue;

                float nx = dx / 8.6f;
                float ny = dy / 8.6f;
                float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - nx * nx - ny * ny));
                float lambert = Mathf.Max(0f, nx * -0.45f + ny * 0.65f + nz * 0.6f);
                int tone = lambert < 0.35f ? 0 : lambert < 0.62f ? 1 : lambert < 0.84f ? 2 : 3;
                c.Set(x, y, ToneColor(tone));
            }
        }

        c.Set(7, 14, new Color32(240, 255, 220, 255));
        c.Set(8, 14, new Color32(240, 255, 220, 255));
        c.Outline(Outline);
        return c.ToSprite(32f, new Vector2(0.5f, 0.5f));
    }

    // ================================================================= dibujo

    /// <summary>Ruido de valor suave (0..1) con una semilla fija: siempre dibuja el mismo charco.</summary>
    private static float ValueNoise(float x, float y)
    {
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        float fx = x - x0;
        float fy = y - y0;
        float sx = fx * fx * (3f - 2f * fx);
        float sy = fy * fy * (3f - 2f * fy);

        float a = Hash(x0, y0);
        float b = Hash(x0 + 1, y0);
        float c = Hash(x0, y0 + 1);
        float d = Hash(x0 + 1, y0 + 1);
        return Mathf.Lerp(Mathf.Lerp(a, b, sx), Mathf.Lerp(c, d, sx), sy);
    }

    private static float Hash(int x, int y)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0xFFFF) / 65535f;
        }
    }

    private static bool InEllipse(float x, float y, float cx, float cy, float rx, float ry)
    {
        float dx = (x - cx) / rx;
        float dy = (y - cy) / ry;
        return dx * dx + dy * dy <= 1f;
    }

    private static void FillRotatedEllipse(PixelCanvas c, bool[,] mask, float cx, float cy, float rx, float ry, float degrees, Color32 color)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        int extent = Mathf.CeilToInt(Mathf.Max(rx, ry)) + 1;

        for (int y = Mathf.FloorToInt(cy) - extent; y <= Mathf.CeilToInt(cy) + extent; y++)
        {
            for (int x = Mathf.FloorToInt(cx) - extent; x <= Mathf.CeilToInt(cx) + extent; x++)
            {
                if (x < 0 || y < 0 || x >= BodyWidth || y >= BodyHeight || !mask[x, y])
                    continue;

                float dx = x + 0.5f - cx;
                float dy = y + 0.5f - cy;
                float rxp = dx * cos + dy * sin;
                float ryp = -dx * sin + dy * cos;
                if ((rxp * rxp) / (rx * rx) + (ryp * ryp) / (ry * ry) <= 1f)
                    c.Set(x, y, color);
            }
        }
    }

    /// <summary>Mini canvas de pixeles (y hacia arriba).</summary>
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

        /// <summary>Como FillEllipse pero solo sobre los pixeles que estan dentro de 'mask'.</summary>
        public void FillEllipseMasked(bool[,] mask, float cx, float cy, float rx, float ry, Color32 color)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx));
            int x1 = Mathf.Min(width - 1, Mathf.CeilToInt(cx + rx));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry));
            int y1 = Mathf.Min(height - 1, Mathf.CeilToInt(cy + ry));

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (!mask[x, y])
                        continue;

                    float dx = (x + 0.5f - cx) / rx;
                    float dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f)
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
    }
}
