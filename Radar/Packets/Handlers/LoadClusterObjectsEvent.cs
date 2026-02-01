using Albion.Network;
using System.Collections.Generic;
using System.Numerics;
using System;
using VRise.Radar.GameObjects.LocalPlayer;
using VRise.Radar.Utility;
using System.Linq;

namespace VRise.Radar.Packets.Handlers
{
    public class LoadClusterObjectsEvent : BaseEvent
    {
        public LoadClusterObjectsEvent(Dictionary<byte, object> parameters) : base(parameters)
        {
            try
            {
                if (!parameters.ContainsKey(1) || !(parameters[1] is byte[][] byteArrays) || byteArrays.Length == 0)
                    return;

                ClusterObjectives = new Dictionary<int, ClusterObjective>();

                for (int i = 0; i < byteArrays.Length; i++)
                {
                    try
                    {
                        int id = ConvertId(parameters, i);

                        if (!parameters.ContainsKey(2) || !(parameters[2] is byte[] charges) || i >= charges.Length)
                            continue;
                        byte charge = charges[i];

                        Vector2 position = Vector2.Zero;
                        if (parameters.ContainsKey(5) && parameters[5] is float[] floatArray && floatArray.Length > i + 1)
                        {
                            position = Additions.fromValues(floatArray[i], floatArray[i + 1]);
                        }

                        if (!parameters.ContainsKey(8) || !(parameters[8] is string[] types) || i >= types.Length)
                            continue;
                        string type = types[i];

                        DateTime time = DateTime.Now;
                        if (type == "CHEST" && parameters.ContainsKey(6) && parameters[6] is long[] times6 && i < times6.Length)
                        {
                            time = new DateTime(times6[i]);
                        }
                        else if (parameters.ContainsKey(7) && parameters[7] is long[] times7 && i < times7.Length)
                        {
                            time = new DateTime(times7[i]);
                        }

                        if (type != "CHEST" && type != "WISPS")
                            continue;

                        ClusterObjectives.Add(id, new ClusterObjective()
                        {
                            Id = id,
                            Charge = charge,
                            Position = position,
                            Timer = time,
                            Type = type,
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[LoadClusterObjectsEvent] Error parsing object {i}: {ex.Message}");
                    }
                }

                if (ClusterObjectives.Count() == 0)
                    ClusterObjectives = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LoadClusterObjectsEvent] Error parsing packet: {ex.Message}");
            }
        }

        public Dictionary<int, ClusterObjective> ClusterObjectives { get; set; }

        private int ConvertId(Dictionary<byte, object> value, int i)
        {
            int id = 0;

            switch (value[4])
            {
                case byte[] byteArr:
                    id = byteArr[i];
                    break;

                case short[] shortArr:
                    id = shortArr[i];
                    break;

                default:
                    id = ((int[])value[4])[i];
                    break;
            }

            return id;
        }
    }
}
