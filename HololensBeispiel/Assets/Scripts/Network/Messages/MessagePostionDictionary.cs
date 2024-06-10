using System.Collections.Generic;
using Newtonsoft.Json;

namespace IMLD.MixedReality.Network
{
    public class MessagePositionDictionary
    {
        public Dictionary<string, Dictionary<string, float>> Data;

        public MessagePositionDictionary(Dictionary<string, Dictionary<string, float>> data)
        {
            Data = data;
        }

        public MessageContainer Pack()
        {
            string payload = JsonConvert.SerializeObject(Data);
            return new MessageContainer(MessageContainer.MessageType.POSITION_DICTIONARY, payload);
        }

        public static MessagePositionDictionary Unpack(MessageContainer container)
        {
            if (container.Type != MessageContainer.MessageType.POSITION_DICTIONARY)
                return null;

            string json = System.Text.Encoding.UTF8.GetString(container.Payload);
            var data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, float>>>(json);
            return new MessagePositionDictionary(data);
        }
    }
}
