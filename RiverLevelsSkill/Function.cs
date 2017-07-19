using System;
using System.Collections.Generic;
using System.Linq;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace RiverLevelsSkill
{
    public class Function
    {
        public SkillResponse Handler(SkillRequest request, ILambdaContext context)
        {
            var log = context.Logger;

            log.LogLine($"Skill Request:");
            log.LogLine(JsonConvert.SerializeObject(request));

            var response = Response(request, log, GetResources().FirstOrDefault());

            log.LogLine($"Skill Response:");
            log.LogLine(JsonConvert.SerializeObject(response));

            return response;
        }

        private static SkillResponse Response(SkillRequest input, ILambdaLogger log, FactResource resource)
        {
            if (input.GetRequestType() == typeof(LaunchRequest))
            {
                log.LogLine($"Default LaunchRequest made: 'Alexa, open Science Facts");
                return Fact(resource, true);
            }

            var intentRequest = (IntentRequest)input.Request;
            switch (intentRequest.Intent.Name)
            {
                case "AMAZON.CancelIntent":
                    log.LogLine($"AMAZON.CancelIntent: send StopMessage");
                    return Stop(resource);
                case "AMAZON.StopIntent":
                    log.LogLine($"AMAZON.StopIntent: send StopMessage");
                    return Stop(resource);
                case "AMAZON.HelpIntent":
                    log.LogLine($"AMAZON.HelpIntent: send HelpMessage");
                    return Help(resource);
                case "GetFactIntent":
                case "GetNewFactIntent":
                    log.LogLine($"GetFactIntent: send Fact");
                    return Fact(resource, true);
                default:
                    log.LogLine($"Unknown intent: " + intentRequest.Intent.Name);
                    return Help(resource);
            }
        }

        private static SkillResponse Stop(FactResource resource)
        {
            return new SkillResponse
            {
                Version = "1.0",
                Response = new ResponseBody
                {
                    ShouldEndSession = true,
                    OutputSpeech = new PlainTextOutputSpeech
                    {
                        Text = resource.StopMessage
                    }
                }
            };
        }

        private static SkillResponse Help(FactResource resource)
        {
            return new SkillResponse
            {
                Version = "1.0",
                Response = new ResponseBody
                {
                    ShouldEndSession = false,
                    OutputSpeech = new PlainTextOutputSpeech
                    {
                        Text = resource.HelpMessage
                    }
                }
            };
        }

        private static SkillResponse Fact(FactResource resource, bool withPreface)
        {
            var next = new Random().Next(resource.Facts.Count);

            return new SkillResponse
            {
                Version = "1.0",
                Response = new ResponseBody
                {
                    ShouldEndSession = false,
                    OutputSpeech = new PlainTextOutputSpeech
                    {
                        Text = withPreface
                            ? resource.GetFactMessage + resource.Facts[next]
                            : resource.Facts[next]
                    },
                }
            };
        }

        private static IEnumerable<FactResource> GetResources()
        {
            yield return new FactResource("en-US")
            {
                SkillName = "American Space Facts",
                GetFactMessage = "Here's your fact: ",
                HelpMessage = "You can say tell me a space fact, or, you can say exit... What can I help you with?",
                HelpReprompt = "What can I help you with?",
                StopMessage = "Goodbye!",
                Facts = new List<string>
                {
                    "A year on Mercury is just 88 days long.",
                    "Despite being farther from the Sun, Venus experiences higher temperatures than Mercury.",
                    "Venus rotates counter-clockwise, possibly because of a collision in the past with an asteroid.",
                    "On Mars, the Sun appears about half the size as it does on Earth.",
                    "Earth is the only planet not named after a god.",
                    "Jupiter has the shortest day of all the planets.",
                    "The Milky Way galaxy will collide with the Andromeda Galaxy in about 5 billion years.",
                    "The Sun contains 99.86% of the mass in the Solar System.",
                    "The Sun is an almost perfect sphere.",
                    "A total solar eclipse can happen once every 1 to 2 years. This makes them a rare event.",
                    "Saturn radiates two and a half times more energy into space than it receives from the sun.",
                    "The temperature inside the Sun can reach 15 million degrees Celsius.",
                    "The Moon is moving approximately 3.8 cm away from our planet every year.",
                },
            };
        }
    }

    public class FactResource
    {
        public FactResource(string language)
        {
            Language = language;
            Facts = new List<string>();
        }

        public string Language { get; set; }
        public string SkillName { get; set; }
        public List<string> Facts { get; set; }
        public string GetFactMessage { get; set; }
        public string HelpMessage { get; set; }
        public string HelpReprompt { get; set; }
        public string StopMessage { get; set; }
    }
}
