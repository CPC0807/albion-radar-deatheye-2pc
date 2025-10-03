using System;
using System.Collections.Generic;
using System.Linq;

namespace X975.Radar.GameObjects.Players
{
    /// <summary>
    /// 嘗試從多個 XorCode 樣本反推 SessionKey
    /// </summary>
    public static class SessionKeyBruteForce
    {
        /// <summary>
        /// 測試簡單 XOR 算法：XorCode = SessionKey XOR TimeBlock
        /// </summary>
        public static void TestSimpleXor(List<byte[]> xorCodes)
        {
            Console.WriteLine("\n========== SessionKey Brute Force Test ==========");
            Console.WriteLine($"Testing {xorCodes.Count} XorCode samples...\n");

            // 測試不同的 TimeBlock 起始值
            for (long baseTime = 0; baseTime < 100000; baseTime++)
            {
                // 測試不同的 TimeBlock 編碼方式

                // 方式 1：TimeBlock 直接用整數
                if (TestTimeBlockEncoding_Integer(xorCodes, baseTime))
                    return;

                // 方式 2：TimeBlock 用 Unix 時間戳（秒）
                if (baseTime == 0)
                {
                    long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    long baseUnixBlock = unixTime / 10;  // 10 秒區塊

                    if (TestTimeBlockEncoding_Integer(xorCodes, baseUnixBlock))
                        return;
                }

                // 每 1000 次顯示進度
                if (baseTime % 1000 == 0)
                {
                    Console.Write($"\rTesting TimeBlock: {baseTime}...");
                }
            }

            Console.WriteLine("\n\n[RESULT] Simple XOR method failed - likely using HMAC/SHA256");
            Console.WriteLine("SessionKey is probably in JoinResponse or initialization event\n");
        }

        private static bool TestTimeBlockEncoding_Integer(List<byte[]> xorCodes, long baseTime)
        {
            // 假設 SessionKey = XorCode[0] XOR TimeBlock[0]
            byte[] timeBlock0 = BitConverter.GetBytes(baseTime);
            byte[] candidateKey = XorBytes(xorCodes[0], timeBlock0);

            // 驗證這個 candidateKey 是否能生成其他所有 XorCode
            bool allMatch = true;
            for (int i = 1; i < xorCodes.Count; i++)
            {
                byte[] timeBlockI = BitConverter.GetBytes(baseTime + i);
                byte[] expectedXorCode = XorBytes(candidateKey, timeBlockI);

                if (!expectedXorCode.SequenceEqual(xorCodes[i]))
                {
                    allMatch = false;
                    break;
                }
            }

            if (allMatch)
            {
                Console.WriteLine($"\n\n✅ ========== FOUND SESSION KEY! ==========");
                Console.WriteLine($"SessionKey: {BitConverter.ToString(candidateKey)}");
                Console.WriteLine($"Starting TimeBlock: {baseTime}");
                Console.WriteLine($"Encoding: Integer (8 bytes)");

                // 驗證
                Console.WriteLine("\nVerification:");
                for (int i = 0; i < xorCodes.Count; i++)
                {
                    byte[] timeBlock = BitConverter.GetBytes(baseTime + i);
                    byte[] generated = XorBytes(candidateKey, timeBlock);
                    string match = generated.SequenceEqual(xorCodes[i]) ? "✅" : "❌";
                    Console.WriteLine($"  {match} XorCode[{i}]: {BitConverter.ToString(xorCodes[i])} (TimeBlock: {baseTime + i})");
                }

                Console.WriteLine("\n============================================\n");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 測試 HMAC-SHA256 算法
        /// </summary>
        public static void TestHMAC(List<byte[]> xorCodes, byte[] candidateSessionKey)
        {
            Console.WriteLine("\n========== HMAC-SHA256 Test ==========");
            Console.WriteLine($"Testing SessionKey: {BitConverter.ToString(candidateSessionKey)}");

            using (var hmac = new System.Security.Cryptography.HMACSHA256(candidateSessionKey))
            {
                for (long timeBlock = 0; timeBlock < 1000; timeBlock++)
                {
                    byte[] message = BitConverter.GetBytes(timeBlock);
                    byte[] hash = hmac.ComputeHash(message);
                    byte[] xorCode = hash.Take(8).ToArray();  // 取前 8 bytes

                    if (xorCode.SequenceEqual(xorCodes[0]))
                    {
                        Console.WriteLine($"\n✅ Found matching TimeBlock: {timeBlock}");

                        // 驗證其他 XorCode
                        bool allMatch = true;
                        for (int i = 1; i < xorCodes.Count; i++)
                        {
                            byte[] msg = BitConverter.GetBytes(timeBlock + i);
                            byte[] h = hmac.ComputeHash(msg);
                            byte[] xc = h.Take(8).ToArray();

                            if (!xc.SequenceEqual(xorCodes[i]))
                            {
                                allMatch = false;
                                break;
                            }
                        }

                        if (allMatch)
                        {
                            Console.WriteLine("✅ All XorCodes match! SessionKey is correct!");
                            return;
                        }
                        else
                        {
                            Console.WriteLine("❌ First XorCode matched but others don't");
                        }
                    }
                }
            }

            Console.WriteLine("❌ No matching TimeBlock found");
        }

        /// <summary>
        /// 分析 XorCode 的統計特性
        /// </summary>
        public static void AnalyzeXorCodes(List<byte[]> xorCodes)
        {
            Console.WriteLine("\n========== XorCode Analysis ==========");

            // 計算相鄰 XorCode 的 XOR 差值
            Console.WriteLine("\nXOR differences between consecutive XorCodes:");
            for (int i = 0; i < xorCodes.Count - 1; i++)
            {
                byte[] diff = XorBytes(xorCodes[i], xorCodes[i + 1]);
                Console.WriteLine($"  XorCode[{i}] XOR XorCode[{i + 1}] = {BitConverter.ToString(diff)}");
            }

            // 熵值分析
            Console.WriteLine("\nByte frequency analysis:");
            int[] byteFreq = new int[256];
            foreach (var xorCode in xorCodes)
            {
                foreach (byte b in xorCode)
                {
                    byteFreq[b]++;
                }
            }

            int uniqueBytes = byteFreq.Count(f => f > 0);
            Console.WriteLine($"  Unique bytes: {uniqueBytes} / 256");
            Console.WriteLine($"  Total bytes: {xorCodes.Count * 8}");
            Console.WriteLine($"  Entropy: {(uniqueBytes / 256.0 * 100):F1}%");

            if (uniqueBytes > 200)
            {
                Console.WriteLine("  → High entropy, likely using strong crypto (HMAC/SHA256)");
            }
            else if (uniqueBytes < 100)
            {
                Console.WriteLine("  → Low entropy, might be simple XOR");
            }
        }

        private static byte[] XorBytes(byte[] a, byte[] b)
        {
            int len = Math.Min(a.Length, b.Length);
            byte[] result = new byte[len];
            for (int i = 0; i < len; i++)
            {
                result[i] = (byte)(a[i] ^ b[i]);
            }
            return result;
        }

        /// <summary>
        /// 從 exp1.txt 中提取的實際 XorCode 樣本
        /// </summary>
        public static List<byte[]> GetExp1Samples()
        {
            return new List<byte[]>
            {
                // 樣本 1 (07:03:37.646)
                new byte[] { 0x86, 0xBA, 0xA9, 0xC5, 0xD7, 0x77, 0xCD, 0x21 },
                // 樣本 2 (07:03:38.026) - 0.38s 後
                new byte[] { 0x62, 0x31, 0xDC, 0x45, 0x91, 0xA8, 0x6B, 0x97 },
                // 樣本 3 (07:03:48.009) - 9.88s 後
                new byte[] { 0xD6, 0x86, 0x0E, 0x0D, 0xA9, 0x38, 0x78, 0xEA },
                // 樣本 4 (07:03:58.007) - 9.99s 後
                new byte[] { 0xA8, 0x0B, 0x7F, 0x17, 0xF5, 0x51, 0x4B, 0x05 },
                // 樣本 5 (07:04:08.009) - 10.00s 後
                new byte[] { 0x5F, 0x8D, 0x24, 0x0B, 0xEC, 0x6E, 0x07, 0x41 },
                // 樣本 6 (07:04:17.998) - 9.99s 後
                new byte[] { 0xA1, 0xFE, 0xB7, 0xA6, 0x66, 0xCB, 0x0F, 0x1E },
                // 樣本 7 (07:04:28.058) - 10.01s 後
                new byte[] { 0xE9, 0x84, 0xE2, 0x58, 0xE4, 0x12, 0xC4, 0x66 },
                // 樣本 8 (07:04:38.052) - 9.99s 後
                new byte[] { 0x60, 0x48, 0x64, 0x1A, 0x4B, 0xB4, 0x73, 0xA5 },
            };
        }

        /// <summary>
        /// 執行完整分析
        /// </summary>
        public static void RunFullAnalysis()
        {
            var xorCodes = GetExp1Samples();

            Console.WriteLine("╔════════════════════════════════════════════════════════╗");
            Console.WriteLine("║   SessionKey Brute Force & XorCode Analysis Tool      ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════╝");

            // 1. 統計分析
            AnalyzeXorCodes(xorCodes);

            // 2. 測試簡單 XOR
            TestSimpleXor(xorCodes);

            Console.WriteLine("\n========== Recommendations ==========");
            Console.WriteLine("1. Capture JoinResponse (Response 2) on login");
            Console.WriteLine("2. Look for 8-byte or 16-byte arrays in initialization events");
            Console.WriteLine("3. Analyze packet patterns (后 4 bytes: 1F-F8, 54-92, E5-4C, etc.)");
            Console.WriteLine("4. Consider reverse engineering the game client\n");
        }
    }
}
