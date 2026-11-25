using UnityEngine;

namespace RhythmArchive
{
    /// <summary>이미지 파일 없이 코드로 만드는 기본 스프라이트들.</summary>
    public static class RuntimeSprites
    {
        static Sprite square, rounded, circle, vertical;

        /// <summary>1x1 유닛 흰 사각형.</summary>
        public static Sprite Square => square != null ? square : (square = MakeSquare());

        /// <summary>모서리가 둥근 9-slice 스프라이트(0.5 유닛). SpriteRenderer.drawMode = Sliced로 사용.</summary>
        public static Sprite Rounded => rounded != null ? rounded : (rounded = MakeRounded(64, 14));

        /// <summary>가장자리가 부드러운 원(1 유닛). 타격 이펙트용.</summary>
        public static Sprite Circle => circle != null ? circle : (circle = MakeCircle(128));

        /// <summary>아래에서 위로 투명해지는 그라데이션(1 유닛). 레인 빔용.</summary>
        public static Sprite VerticalFade => vertical != null ? vertical : (vertical = MakeVerticalFade(64));

        static Texture2D NewTex(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            return tex;
        }

        static Sprite MakeSquare()
        {
            var tex = NewTex(4, 4);
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        }

        static Sprite MakeRounded(int size, int radius)
        {
            var tex = NewTex(size, size);
            var px = new Color32[size * size];
            float r = radius;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, r, size - r);
                float cy = Mathf.Clamp(y + 0.5f, r, size - r);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                float a = Mathf.Clamp01(r - d + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            var border = new Vector4(radius, radius, radius, radius);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size * 2,
                0, SpriteMeshType.FullRect, border);
        }

        static Sprite MakeCircle(int size)
        {
            var tex = NewTex(size, size);
            var px = new Color32[size * size];
            float c = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)) / c;
                // 링 + 부드러운 안쪽 광원
                float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.85f) / 0.12f);
                float glow = Mathf.Clamp01(1f - d) * 0.35f;
                float a = Mathf.Clamp01(ring + glow);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static Sprite MakeVerticalFade(int size)
        {
            var tex = NewTex(4, size);
            var px = new Color32[4 * size];
            for (int y = 0; y < size; y++)
            {
                byte a = (byte)(255 * Mathf.Pow(1f - (float)y / (size - 1), 1.6f));
                for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply();
            // pivot을 아래쪽 중앙으로: 판정선에서 위로 뻗는다.
            return Sprite.Create(tex, new Rect(0, 0, 4, size), new Vector2(0.5f, 0f), size);
        }
    }
}
