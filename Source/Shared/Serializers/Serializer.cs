using System;
using System.IO;
using Newtonsoft.Json;
using RimworldTogether.Shared.Network;
using Shared.Misc;

namespace RimworldTogether.Shared.Serializers
{
    public static class Serializer
    {
        //Packets

        public static string SerializePacketToString(Packet packet)
        {
            byte[] packetBytes = ObjectConverter.ConvertObjectToBytes(packet);
            packetBytes = GZip2.Compress(packetBytes);

            //Logger.WriteToConsole($"Outgoing > {packetBytes.Length}", Logger.LogMode.Warning);
            //Logger.WriteToConsole("");

            return Convert.ToBase64String(packetBytes);
        }

        public static Packet SerializeStringToPacket(string serializable)
        {
            byte[] packetBytes = Convert.FromBase64String(serializable);
            packetBytes = GZip2.Decompress(packetBytes);

            //Logger.WriteToConsole($"Incoming > {packetBytes.Length}", Logger.LogMode.Warning);

            return (Packet)ObjectConverter.ConvertBytesToObject(packetBytes);
        }

        //Data

        public static string SerializeToString(object serializable)
        {
            return JsonConvert.SerializeObject(serializable);
        }

        public static T SerializeFromString<T>(string serializable)
        {
            return JsonConvert.DeserializeObject<T>(serializable);
        }

        //Files

        public static void SerializeToFile(string path, object serializable)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(serializable, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            }));
        }

        public static T SerializeFromFile<T>(string path)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        }
    }
}