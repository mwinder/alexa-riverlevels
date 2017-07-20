using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
        private static readonly Uri ApiUrl = new Uri("http://api.rainchasers.com/v1");

        public SkillResponse Handler(SkillRequest request, ILambdaContext context)
        {
            var log = context.Logger;

            log.LogLine($"Skill Request:");
            log.LogLine(JsonConvert.SerializeObject(request));

            var response = Response(request, log, Resources().FirstOrDefault());

            log.LogLine($"Skill Response:");
            log.LogLine(JsonConvert.SerializeObject(response));

            return response;
        }

        private static SkillResponse Response(SkillRequest input, ILambdaLogger log, RootResource resource)
        {
            if (input.GetRequestType() == typeof(LaunchRequest))
            {
                log.LogLine($"LaunchRequest");
                return Help(resource);
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
                case "LevelDeeIntent":
                    log.LogLine($"LevelDeeIntent");
                    return Level(resource.River("Dee"), log);
                case "LevelNorthTyneIntent":
                    log.LogLine($"LevelNorthTyneIntent");
                    return Level(resource.River("North Tyne"), log);
                default:
                    log.LogLine($"Unknown intent: " + intentRequest.Intent.Name);
                    return Help(resource);
            }
        }

        private static SkillResponse Stop(RootResource resource)
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

        private static SkillResponse Help(RootResource resource)
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

        private static SkillResponse Level(RiverResource resource, ILambdaLogger log)
        {
            var client = new HttpClient();
            var requestUri = new Uri($"{ApiUrl}/river/{resource.Uuid}");

            log.LogLine($"Requesting: {requestUri}");

            var result = client.GetAsync(requestUri).Result;
            var content = result.Content.ReadAsStringAsync().Result;

            log.LogLine(content);

            var river = (dynamic)JsonConvert.DeserializeObject(content);

            return new SkillResponse
            {
                Version = "1.0",
                Response = new ResponseBody
                {
                    ShouldEndSession = false,
                    OutputSpeech = new PlainTextOutputSpeech
                    {
                        Text = $"The river {river.data.river}, {river.data.section} is {river.data.state.text}, {river.data.state.value}",
                    },
                }
            };
        }

        private static IEnumerable<RootResource> Resources()
        {
            yield return new RootResource("en-GB")
            {
                Description = "UK river levels",
                HelpMessage = "Ask me for the level of your favourite river",
                StopMessage = "See you on the river!",
                Rivers = new List<RiverResource>
                {
                    new RiverResource { Name = "Clough", Uuid = "4b50bd9e-9c88-4795-93e0-b1e5c213e9ed" },
                    new RiverResource { Name = "Dee", Uuid = "75148ca0-ee5e-4344-8534-db9a59ed4cd0" },
                    new RiverResource { Name = "North Tyne", Uuid = "9a417b1b-464e-4f49-be17-bbb38241e500" }
                }
            };
        }
    }

    public class RootResource
    {
        public RootResource(string language)
        {
            Language = language;
        }

        public string Language { get; set; }
        public string Description { get; set; }
        public string HelpMessage { get; set; }
        public string StopMessage { get; set; }
        public IEnumerable<RiverResource> Rivers { get; set; }

        public RiverResource River(string river)
        {
            return Rivers.SingleOrDefault(x => x.Name == river) ?? RiverResource.Unknown;
        }
    }

    public class RiverResource
    {
        public static RiverResource Unknown => new RiverResource { Name = "Unknown" };

        public string Name { get; set; }
        public string Uuid { get; set; }
    }
}
