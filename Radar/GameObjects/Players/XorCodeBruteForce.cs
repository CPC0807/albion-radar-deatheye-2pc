using System;
using System.Collections.Generic;
using System.Numerics;

namespace X975.Radar.GameObjects.Players
{
    /// <summary>
    /// XorCode 暴力破解工具
    /// 假設：座標在 0-3000 範圍內，我們可以反推出 XorCode
    /// </summary>
    public static class XorCodeBruteForce
    {
        /// <summary>
        /// 嘗試反推 XorCode
        /// 假設玩家在某個合理的位置（不在原點）
        /// </summary>
        public static byte[] TryRecoverXorCode(byte[] encryptedBytes, float expectedX = -1, float expectedY = -1)
        {
            if (encryptedBytes == null || encryptedBytes.Length < 8)
                return null;

            // 如果沒有提供預期座標，嘗試一些常見的範圍
            List<Vector2> testPositions = new List<Vector2>();

            // 檢查是否提供了有效的預期座標
            bool hasExpectedPos = (expectedX != -1 && expectedY != -1) &&
                                  (Math.Abs(expectedX) < 10000f && Math.Abs(expectedY) < 10000f);

            if (hasExpectedPos)
            {
                testPositions.Add(new Vector2(expectedX, expectedY));
            }
            else
            {
                // 如果沒有提供座標，直接返回 null
                // 暴力破解範圍太大，容易找到錯誤的 XorCode
                return null;
            }

            foreach (var testPos in testPositions)
            {
                byte[] xorKey = TryExtractKey(encryptedBytes, testPos.X, testPos.Y);
                if (xorKey != null)
                {
                    Console.WriteLine($"[XorCodeRecover] Possible XorCode: {BitConverter.ToString(xorKey)} for position ({testPos.X:F0},{testPos.Y:F0})");
                    return xorKey;
                }
            }

            return null;
        }

        private static byte[] TryExtractKey(byte[] encryptedBytes, float targetX, float targetY)
        {
            // 驗證目標座標是否合理（避免極端值）
            if (Math.Abs(targetX) > 10000f || Math.Abs(targetY) > 10000f)
            {
                return null;
            }

            // 避免接近 0,0 的座標（玩家不太可能完全在原點）
            if (Math.Abs(targetX) < 0.5f && Math.Abs(targetY) < 0.5f)
            {
                return null;
            }

            byte[] targetXBytes = BitConverter.GetBytes(targetX);
            byte[] targetYBytes = BitConverter.GetBytes(targetY);

            byte[] xorKey = new byte[8];

            // 計算 XOR key
            for (int i = 0; i < 4; i++)
            {
                xorKey[i] = (byte)(encryptedBytes[i] ^ targetXBytes[i]);
                xorKey[i + 4] = (byte)(encryptedBytes[i + 4] ^ targetYBytes[i]);
            }

            // 驗證這個 key 是否合理
            // 1. 不應該全是 0
            bool allZero = true;
            for (int i = 0; i < 8; i++)
            {
                if (xorKey[i] != 0)
                {
                    allZero = false;
                    break;
                }
            }

            if (allZero)
                return null;

            // 2. XorCode 應該看起來像隨機數據（不應該和原始 bytes 相同）
            bool sameAsOriginal = true;
            for (int i = 0; i < 8; i++)
            {
                if (xorKey[i] != encryptedBytes[i])
                {
                    sameAsOriginal = false;
                    break;
                }
            }

            if (sameAsOriginal)
                return null;

            // 3. 測試這個 key 是否能產生合理的座標（使用正確的解密算法）
            byte[] testX = new byte[4];
            byte[] testY = new byte[4];
            Array.Copy(encryptedBytes, 0, testX, 0, 4);
            Array.Copy(encryptedBytes, 4, testY, 0, 4);

            DecryptBytes(testX, xorKey, 0);
            DecryptBytes(testY, xorKey, 4);

            float decryptedX = BitConverter.ToSingle(testX, 0);
            float decryptedY = BitConverter.ToSingle(testY, 0);

            // 必須匹配目標座標
            if (Math.Abs(decryptedX - targetX) < 0.01f && Math.Abs(decryptedY - targetY) < 0.01f)
            {
                return xorKey;
            }

            return null;
        }

        /// <summary>
        /// 使用已知的 XorCode 解密座標
        /// </summary>
        public static Vector2? DecryptPosition(byte[] encryptedBytes, byte[] xorKey)
        {
            if (encryptedBytes == null || xorKey == null ||
                encryptedBytes.Length < 8 || xorKey.Length < 8)
                return null;

            // 使用 PlayersHandler 的解密算法
            byte[] xBytes = new byte[4];
            byte[] yBytes = new byte[4];

            Array.Copy(encryptedBytes, 0, xBytes, 0, 4);
            Array.Copy(encryptedBytes, 4, yBytes, 0, 4);

            // 解密 X (使用 XorCode 的 bytes[0-3], saltPos=0)
            DecryptBytes(xBytes, xorKey, 0);

            // 解密 Y (使用 XorCode 的 bytes[4-7], saltPos=4)
            DecryptBytes(yBytes, xorKey, 4);

            float x = BitConverter.ToSingle(xBytes, 0);
            float y = BitConverter.ToSingle(yBytes, 0);

            if (!float.IsNaN(x) && !float.IsNaN(y) &&
                !float.IsInfinity(x) && !float.IsInfinity(y))
            {
                return new Vector2(x, y);
            }

            return null;
        }

        /// <summary>
        /// 解密 4 bytes（使用 PlayersHandler 的算法）
        /// </summary>
        private static void DecryptBytes(byte[] bytes4, byte[] saltBytes8, int saltPos)
        {
            for (int i = 0; i < bytes4.Length; i++)
            {
                int saltIndex = i % (saltBytes8.Length - saltPos) + saltPos;
                bytes4[i] ^= saltBytes8[saltIndex];
            }
        }
    }
}
