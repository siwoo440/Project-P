using System;

namespace ProjectP.EditorTools
{
    /// <summary>
    /// 코드로 그리는 임시 UI 도형. 부호 있는 거리 함수(SDF)로 계산해 가장자리가 매끄럽다.
    /// Unity에 의존하지 않는 순수 계산이라 에디터 밖 미리보기에서도 같은 코드를 쓴다.
    ///
    /// 반환값은 RGBA32 바이트 배열이며 행 0이 맨 아래다(Unity 텍스처 순서).
    /// 흰색 도형은 Image 색으로 물들여(tint) 쓴다.
    /// </summary>
    public static class SpriteShapes
    {
        // ---------------------------------------------------------------- UI 기본 도형

        public static byte[] RoundedRect(int size, float radius) =>
            Render(size, size, (x, y) => (1f, Coverage(RoundedBox(x, y, size / 2f, size / 2f, radius))));

        public static byte[] RoundedOutline(int size, float radius, float stroke) =>
            Render(size, size, (x, y) =>
            {
                var d = RoundedBox(x, y, size / 2f - stroke / 2f, size / 2f - stroke / 2f, radius - stroke / 2f);
                return (1f, Coverage(Math.Abs(d) - stroke / 2f));
            });

        public static byte[] Shadow(int size, float radius, float blur, float opacity) =>
            Render(size, size, (x, y) =>
            {
                var half = size / 2f - blur;
                var d = RoundedBox(x, y, half, half, radius);
                var t = Clamp01(d / blur);
                var falloff = 1f - t * t * (3f - 2f * t);
                return (0f, opacity * falloff);
            });

        public static byte[] Circle(int size) =>
            Render(size, size, (x, y) => (1f, Coverage(Length(x, y) - (size / 2f - 1f))));

        public static byte[] Glow(int size) =>
            Render(size, size, (x, y) =>
            {
                var t = Clamp01(Length(x, y) / (size / 2f));
                return (1f, (float)Math.Pow(1f - t, 2.2f));
            });

        /// <summary>세로 그라디언트. 위쪽 색 → 아래쪽 색. 색은 0~1 RGB.</summary>
        public static byte[] VerticalGradient(int width, int height, float[] top, float[] bottom)
        {
            var pixels = new byte[width * height * 4];
            for (var y = 0; y < height; y++)
            {
                var t = (y + 0.5f) / height;
                for (var x = 0; x < width; x++)
                {
                    var i = (y * width + x) * 4;
                    for (var c = 0; c < 3; c++) pixels[i + c] = ToByte(bottom[c] + (top[c] - bottom[c]) * t);
                    pixels[i + 3] = 255;
                }
            }

            return pixels;
        }

        // ---------------------------------------------------------------- 보석

        /// <summary>보석 타일. 위가 밝고 아래가 어두운 입체감과 윗부분 광택, 어두운 테두리를 넣는다.</summary>
        public static byte[] GemTile(int size)
        {
            var half = size / 2f - 2f;
            var radius = size * 0.24f;
            return Render(size, size, (x, y) =>
            {
                var d = RoundedBox(x, y, half, half, radius);
                var t = (y + size / 2f) / size;
                var value = 0.78f + 0.22f * t;

                var gloss = RoundedBox(x, y - size * 0.2f, half - 10f, half * 0.45f, radius - 8f);
                value += 0.12f * Coverage(gloss * 0.5f) * t;

                if (d > -4f) value *= 0.82f + 0.18f * Clamp01(-d / 4f);
                return (Clamp01(value), Coverage(d));
            });
        }

        /// <summary>물리 — 세로로 긴 다이아몬드.</summary>
        public static byte[] IconPhysical(int size)
        {
            var s = size / 128f;
            return Render(size, size, (x, y) => (1f, Coverage(Rhombus(x, y, 34f * s, 48f * s) - 3f * s)));
        }

        /// <summary>마법 — 다섯 갈래 별.</summary>
        public static byte[] IconMagic(int size)
        {
            var s = size / 128f;
            return Render(size, size, (x, y) => (1f, Coverage(Star(x, y + 3f * s, 44f * s, 5, 3f) - 3f * s)));
        }

        /// <summary>회복 — 둥근 십자.</summary>
        public static byte[] IconHeal(int size)
        {
            var s = size / 128f;
            return Render(size, size, (x, y) =>
            {
                var horizontal = RoundedBox(x, y, 44f * s, 15f * s, 7f * s);
                var vertical = RoundedBox(x, y, 15f * s, 44f * s, 7f * s);
                return (1f, Coverage(Math.Min(horizontal, vertical)));
            });
        }

        /// <summary>혼돈 — 여덟 갈래 가시 폭발.</summary>
        public static byte[] IconChaos(int size)
        {
            var s = size / 128f;
            return Render(size, size, (x, y) => (1f, Coverage(Star(x, y, 44f * s, 8, 3.4f) - 4f * s)));
        }

        /// <summary>균형 — 고리와 가운데 점.</summary>
        public static byte[] IconBalance(int size)
        {
            var s = size / 128f;
            return Render(size, size, (x, y) =>
            {
                var ring = Math.Abs(Length(x, y) - 34f * s) - 8f * s;
                var dot = Length(x, y) - 12f * s;
                return (1f, Coverage(Math.Min(ring, dot)));
            });
        }

        // ---------------------------------------------------------------- 계산 도우미

        /// <summary>픽셀 중심 좌표(가운데가 0, 위가 +)마다 (밝기, 투명도)를 계산해 RGBA 배열을 만든다.</summary>
        private static byte[] Render(int width, int height, Func<float, float, (float value, float alpha)> shade)
        {
            var pixels = new byte[width * height * 4];
            for (var py = 0; py < height; py++)
            {
                for (var px = 0; px < width; px++)
                {
                    var (value, alpha) = shade(px + 0.5f - width / 2f, py + 0.5f - height / 2f);
                    var i = (py * width + px) * 4;
                    var v = ToByte(value);
                    pixels[i] = v;
                    pixels[i + 1] = v;
                    pixels[i + 2] = v;
                    pixels[i + 3] = ToByte(alpha);
                }
            }

            return pixels;
        }

        private static float Coverage(float distance) => Clamp01(0.5f - distance);

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;

        private static byte ToByte(float value) => (byte)Math.Round(Clamp01(value) * 255f);

        private static float Length(float x, float y) => (float)Math.Sqrt(x * x + y * y);

        private static float RoundedBox(float x, float y, float halfWidth, float halfHeight, float radius)
        {
            var qx = Math.Abs(x) - halfWidth + radius;
            var qy = Math.Abs(y) - halfHeight + radius;
            return Length(Math.Max(qx, 0f), Math.Max(qy, 0f)) + Math.Min(Math.Max(qx, qy), 0f) - radius;
        }

        private static float Rhombus(float x, float y, float bx, float by)
        {
            x = Math.Abs(x);
            y = Math.Abs(y);
            var ndot = (bx - 2f * x) * bx - (by - 2f * y) * by;
            var h = Math.Max(-1f, Math.Min(1f, ndot / (bx * bx + by * by)));
            var d = Length(x - 0.5f * bx * (1f - h), y - 0.5f * by * (1f + h));
            return d * Math.Sign(x * by + y * bx - bx * by);
        }

        /// <summary>n갈래 별. m이 클수록 갈래가 뾰족하다(2 ≤ m ≤ n).</summary>
        private static float Star(float x, float y, float radius, int n, float m)
        {
            var an = (float)Math.PI / n;
            var en = (float)Math.PI / m;
            float acx = (float)Math.Cos(an), acy = (float)Math.Sin(an);
            float ecx = (float)Math.Cos(en), ecy = (float)Math.Sin(en);

            var angle = (float)Math.Atan2(x, y);
            var bn = angle - 2f * an * (float)Math.Floor(angle / (2f * an)) - an;
            var length = Length(x, y);
            var px = length * (float)Math.Cos(bn) - radius * acx;
            var py = length * Math.Abs((float)Math.Sin(bn)) - radius * acy;

            var k = Math.Max(0f, Math.Min(-(px * ecx + py * ecy), radius * acy / ecy));
            px += ecx * k;
            py += ecy * k;
            return Length(px, py) * Math.Sign(px);
        }
    }
}
