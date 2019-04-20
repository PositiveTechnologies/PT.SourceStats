using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PT.PM.Common;
using System;
using System.Linq;

namespace PT.SourceStats
{
    public class MessagesJsonConverter : JsonConverter
    {
        public MessagesJsonConverter()
        {
        }

        public override bool CanConvert(Type objectType)
        {
            var result = objectType == typeof(Message) || objectType == typeof(LanguageStatistics);
            return result;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.Null)
            {
                JObject jObject = JObject.Load(reader);
                object result;
                if (objectType == typeof(Message))
                {
                    JToken token = jObject[nameof(MessageType)];
                    var messageType = (MessageType)Enum.Parse(typeof(MessageType), token.ToString());
                    switch (messageType)
                    {
                        case MessageType.Progress:
                            result = new ProgressMessage();
                            break;
                        case MessageType.Error:
                            result = new ErrorMessage();
                            break;
                        case MessageType.Result:
                            result = new StatisticsMessage();
                            break;
                        default:
                            throw new NotImplementedException($"{token} message type is not supported");
                    }
                }
                else if (objectType == typeof(LanguageStatistics))
                {
                    JToken token = jObject[nameof(Language)];
                    Language language = LanguageUtils.ParseLanguages(token.ToString()).FirstOrDefault();
                    if (language == Language.CSharp)
                    {
                        result = new CSharpStatistics();
                    }
                    else if (language == Language.Java)
                    {
                        result = new JavaStatistics();
                    }
                    else if (language == Language.Php)
                    {
                        result = new PhpStatistics();
                    }
                    else
                    {
                        throw new NotImplementedException($"{token} language is not supported");
                    }
                }
                else
                {
                    throw new FormatException("Invalid JSON");
                }

                serializer.Populate(jObject.CreateReader(), result);
                return result;
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
