using System.Collections.Generic;
using System.Linq;

namespace X975.Radar.Packets.Handlers
{
    public class BruteForceKeySyncEvent : BaseEvent
    {
        public BruteForceKeySyncEvent(Dictionary<byte, object> parameters) : base(parameters)
        {
            // 掃描所有參數，尋找8-byte陣列
            Code = parameters.Values
                .OfType<byte[]>()
                .FirstOrDefault(arr => arr.Length == 8);
        }

        public byte[] Code { get; }
    }
}
