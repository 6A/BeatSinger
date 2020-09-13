using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSinger.SimpleJSON;

namespace BeatSinger.Helpers
{
    public static class Extensions
    {
        public static string GetReasonString(this MusixMatchStatus status)
        {
            switch (status)
            {
                case MusixMatchStatus.None:
                    return "Unknown";
                case MusixMatchStatus.Success:
                    return "The request was successful.";
                case MusixMatchStatus.BadRequest:
                    return "The request had bad syntax or was inherently impossible to be satisfied.";
                case MusixMatchStatus.Unauthorized:
                    return "Invalid or missing API key.";
                case MusixMatchStatus.UsageLimitReached:
                    return "The usage limit has been reached.";
                case MusixMatchStatus.Forbidden:
                    return "You are not authorized to perform this operation.";
                case MusixMatchStatus.NotFound:
                    return "The song was not found.";
                case MusixMatchStatus.MethodNotFound:
                    return "The requested method was not found.";
                case MusixMatchStatus.GeneralFailure:
                    return "A server error occurred.";
                case MusixMatchStatus.ServiceUnavailable:
                    return "The MusixMatch service is too busy.";
                default:
                    return "Unknown";
            }
        }

        public static JSONArray ToJsonArray(this IEnumerable<Subtitle> subtitles)
        {
            JSONArray array = new JSONArray();
            foreach (var subtitle in subtitles)
            {
                JSONNode node = new JSONObject();
                node["text"] = subtitle.Text;
                node["time"] = subtitle.Time;
                if (subtitle.EndTime != null && subtitle.EndTime > subtitle.Time)
                    node["end"] = subtitle.EndTime;
                array.Add(node);
            }
            return array;
        }
        public static JSONObject ToJson(this SubtitleContainer subtitles)
        {
            JSONObject jObject = new JSONObject();
            jObject["timeOffset"] = subtitles.TimeOffset;
            jObject["timeScale"] = subtitles.TimeScale;
            JSONArray array = subtitles.Subtitles.ToJsonArray();
            jObject["subtitles"] = array;
            return jObject;
        }
    }
}
