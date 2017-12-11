using Newtonsoft.Json;
using NUnit.Framework;
using PT.PM.Common.Json;
using System.Collections.Generic;

namespace PT.SourceStats.Tests
{
    [TestFixture]
    public class SerializationTest
    {
        [Test]
        public void Deserialize_Messages()
        {
            var messages = new Message[]
            {
                new ErrorMessage("Error Message"),
                new ProgressMessage(1, 2) { LastFileName = "temp" },
                new StatisticsMessage
                {
                    ErrorCount = 0,
                    LanguageStatistics = new List<LanguageStatistics>()
                    {
                        new PhpStatistics(),
                        new JavaStatistics(),
                        new CSharpStatistics()
                    }
                }
            };
            string expectedJson = JsonConvert.SerializeObject(messages, Formatting.Indented, LanguageJsonConverter.Instance);
            var deserialized = JsonConvert.DeserializeObject<Message[]>(expectedJson, new MessagesJsonConverter(), LanguageJsonConverter.Instance);
            string actualJson = JsonConvert.SerializeObject(deserialized, Formatting.Indented, LanguageJsonConverter.Instance);

            Assert.AreEqual(expectedJson, actualJson);
        }
    }
}
