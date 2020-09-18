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
        public static JSONObject ToJson(this SubtitleContainer subtitles, bool convertSrt = false)
        {
            JSONObject jObject = new JSONObject();
            jObject["timeOffset"] = subtitles.TimeOffset;
            jObject["timeScale"] = subtitles.TimeScale;
            if (convertSrt || subtitles.SourceType != LyricSource.File_SRT)
            {
                JSONArray array = subtitles.Subtitles.ToJsonArray();
                jObject["subtitles"] = array;
            }
            return jObject;
        }

        public static void RaiseEventSafe(this EventHandler e, object sender, string eventName)
        {
            EventHandler[] handlers = e?.GetInvocationList().Select(d => (EventHandler)d).ToArray()
                ?? Array.Empty<EventHandler>();
            for (int i = 0; i < handlers.Length; i++)
            {
                try
                {
                    handlers[i].Invoke(sender, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Plugin.log?.Error($"Error in {eventName} handlers '{handlers[i]?.Method.Name}': {ex.Message}");
                    Plugin.log?.Debug(ex);
                }
            }
        }

        public static void RaiseEventSafe<TArgs>(this EventHandler<TArgs> e, object sender, TArgs args, string eventName)
        {
            EventHandler<TArgs>[] handlers = e?.GetInvocationList().Select(d => (EventHandler<TArgs>)d).ToArray()
                ?? Array.Empty<EventHandler<TArgs>>();
            for (int i = 0; i < handlers.Length; i++)
            {
                try
                {
                    handlers[i].Invoke(sender, args);
                }
                catch (Exception ex)
                {
                    Plugin.log?.Error($"Error in {eventName} handlers '{handlers[i]?.Method.Name}': {ex.Message}");
                    Plugin.log?.Debug(ex);
                }
            }
        }
    }
}
