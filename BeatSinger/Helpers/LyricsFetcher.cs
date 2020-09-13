using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SongCore;
using UnityEngine;
using UnityEngine.Networking;

namespace BeatSinger
{
    using BeatSinger.Helpers;
    using SimpleJSON;

    /// <summary>
    ///   Provides utilities for asynchronously fetching lyrics.
    /// </summary>
    public static class LyricsFetcher
    {
        private static void PopulateFromJson(string jsonString, List<Subtitle> subtitles)
        {
            JSONNode json = JSON.Parse(jsonString);
            JSONArray subtitlesArray;
            if (json.IsArray)
                subtitlesArray = json.AsArray;
            else
                subtitlesArray = json["subtitles"].AsArray;
            PopulateFromJson(subtitlesArray, subtitles);
        }
        private static void PopulateFromJson(JSONArray jAry, List<Subtitle> subtitles)
        {
            subtitles.Capacity = jAry.Count;

            foreach (JSONNode node in jAry)
            {
                subtitles.Add(new Subtitle(node));
            }
        }

        private static void PopulateFromJson(string jsonString, SubtitleContainer container)
        {
            JSONNode json = JSON.Parse(jsonString);
            JSONArray subtitlesArray;
            float timeScale = 1;
            if (json.IsArray)
                subtitlesArray = json.AsArray;
            else
            {
                container.TimeOffset = json["timeOffset"].AsFloat;
                timeScale = json["timeScale"].AsFloat;
                subtitlesArray = json["subtitles"].AsArray;
                if (timeScale > 0)
                    container.TimeScale = timeScale;
            }
            List<Subtitle> subtitles = new List<Subtitle>(subtitlesArray.Count);
            PopulateFromJson(subtitlesArray, subtitles);

            container.SetSubtitles(subtitles);
        }

        /// <summary>
        ///   Creates a <see cref="SubtitleContainer"/> from an SRT file. Return null if unsuccessful.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static void PopulateFromSrt(TextReader reader, List<Subtitle> subtitles)
        {
            // Parse using a simple state machine:
            //   0: Parsing number
            //   1: Parsing start / end time
            //   2: Parsing text
            byte state = 0;
            bool invalid = false;
            float startTime = 0f,
                  endTime = 0f;

            StringBuilder text = new StringBuilder();
            string line;
            int lineNumber = 0;
            while ((line = reader.ReadLine()) != null && !invalid)
            {
                lineNumber++;
                switch (state)
                {
                    case 0:
                        if (string.IsNullOrWhiteSpace(line))
                            // No number found; continue in same state.
                            continue;

                        if (!int.TryParse(line, out int _))
                        {
                            Plugin.log?.Warn($"Line {lineNumber}: {line} is not an integer.");
                            invalid = true;
                            break;
                        }

                        // Number found; continue to next state.
                        state = 1;
                        break;

                    case 1:
                        Match m = Regex.Match(line, @"(\d+):(\d+):(\d+,\d+) *--> *(\d+):(\d+):(\d+,\d+)");

                        if (!m.Success)
                        {
                            Plugin.log?.Error($"Invalid line in SRT file, line {lineNumber}: '{line}'");
                            invalid = true;
                            break;
                        }
                        startTime = int.Parse(m.Groups[1].Value) * 3600
                                  + int.Parse(m.Groups[2].Value) * 60
                                  + float.Parse(m.Groups[3].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture);

                        endTime = int.Parse(m.Groups[4].Value) * 3600
                                + int.Parse(m.Groups[5].Value) * 60
                                + float.Parse(m.Groups[6].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture);

                        // Subtitle start / end found; continue to next state.
                        state = 2;
                        break;

                    case 2:
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            // End of text; continue to next state.
                            subtitles.Add(new Subtitle(text.ToString(), startTime, endTime));
                            text.Length = 0;
                            state = 0;
                        }
                        else
                        {
                            // Continuation of text; continue in same state.
                            text.AppendLine(line);
                        }

                        break;

                    default:
                        // Shouldn't happen.
                        throw new Exception($"Invalid syntax in SRT file on line {lineNumber}: '{line}'");
                }
            }
            // From https://github.com/y0100100012/BeatSinger/blob/59b49b16b28d30a270d72df3e691d0d923dd2a14/BeatSinger/Helpers/LyricsFetcher.cs#L151
            //Add Last Subtitle if end is reached with text not empty
            if (text.Length != 0)
            {
                subtitles.Add(new Subtitle(text.ToString(), startTime, endTime));
                //             Debug.Log($"{text}   {startTime}   {endTime}");
                text.Length = 0;
                state = 0;
            }
            if (invalid)
            {
                Plugin.log?.Warn("Invalid subtiles file found, cancelling load...");
                subtitles.Clear();
            }
        }

        /// <summary>
        ///   Fetches the lyrics of the given song on the local file system and, if they're found,
        ///   populates the given list.
        /// </summary>
        public static bool TryGetLocalLyrics(string songDirectory, out SubtitleContainer container)
        {

            Plugin.log?.Debug($"Song directory: {songDirectory}.");
            container = SubtitleContainer.Empty;
            if (string.IsNullOrWhiteSpace(songDirectory) || !Directory.Exists(songDirectory))
                return false;

            // Find JSON lyrics
            string jsonFile = Path.Combine(songDirectory, "lyrics.json");

            if (File.Exists(jsonFile))
            {
                PopulateFromJson(File.ReadAllText(jsonFile), container);

                return container.Count > 0;
            }

            // Find SRT lyrics
            string srtFile = Path.Combine(songDirectory, "lyrics.srt");

            if (File.Exists(srtFile))
            {
                using (FileStream fs = File.OpenRead(srtFile))
                using (StreamReader reader = new StreamReader(fs))
                {
                    List<Subtitle> subtitles = new List<Subtitle>();
                    PopulateFromSrt(reader, subtitles);
                    if (subtitles.Count > 0)
                        container.SetSubtitles(subtitles);
                    return container.Count > 0;
                }

            }

            return false;
        }

        /// <summary>
        ///   Fetches the lyrics of the given song online asynchronously and, if they're found,
        ///   populates the given list.
        /// </summary>
        public static IEnumerator GetOnlineLyrics(IBeatmapLevel level, List<Subtitle> subtitles)
        {
            // Perform request
            UnityWebRequest req = UnityWebRequest.Get($"https://beatsinger.herokuapp.com/{level.GetLyricsHash()}");

            yield return req.SendWebRequest();

            if (req.isNetworkError || req.isHttpError)
            {
                Plugin.log?.Warn($"Error getting lyrics from 'beatsinger.herokuapp.com': { req.error}");
            }
            else if (req.responseCode == 200)
            {
                // Request done, process result
                try
                {
                    if (req.GetResponseHeader("Content-Type") == "application/json")
                    {
                        PopulateFromJson(req.downloadHandler.text, subtitles);
                    }
                    else
                    {
                        using (StringReader reader = new StringReader(req.downloadHandler.text))
                            PopulateFromSrt(reader, subtitles);
                    }
                }
                catch (Exception e)
                {
                    Plugin.log?.Error($"Error parsing lyrics response: {e.Message}");
                    Plugin.log?.Debug(e);
                }
            }

            req.Dispose();
        }

        /// <summary>
        ///   Fetches the lyrics of the given song online asynchronously using Musixmatch and, if they're found,
        ///   populates the given list.
        /// </summary>
        public static IEnumerator GetMusixmatchLyrics(string song, string artist, List<Subtitle> subtitles)
        {
            // Perform request
            string qTrack = UnityWebRequest.EscapeURL(song);
            string qArtist = UnityWebRequest.EscapeURL(artist);

            string url = "https://apic-desktop.musixmatch.com/ws/1.1/macro.subtitles.get"
                       + $"?format=json&q_track={qTrack}&q_artist={qArtist}&user_language=en"
                       + "&userblob_id=aG9va2VkIG9uIGEgZmVlbGluZ19ibHVlIHN3ZWRlXzE3Mg"
                       + "&subtitle_format=mxm&app_id=web-desktop-app-v1.0"
                       + "&usertoken=180220daeb2405592f296c4aea0f6d15e90e08222b559182bacf92";

            if (Settings.VerboseLogging)
                Plugin.log?.Debug($"Requesting lyrics from '{url}'");
            UnityWebRequest req = UnityWebRequest.Get(url);

            req.SetRequestHeader("Cookie", "x-mxm-token-guid=cd25ed55-85ea-445b-83cd-c4b173e20ce7");

            yield return req.SendWebRequest();

            if (req.isNetworkError || req.isHttpError)
            {
                Plugin.log?.Error($"Network error getting lyrics from MusixMatch: {req.error}");
            }
            else
            {
                // Request done, process result
                try
                {

                    JSONArray subtitlesArray = ParseMusixMatchResponse(req.downloadHandler.text);
                    if (subtitlesArray != null)
                    {
                        // No need to sort subtitles here, it should already be done.
                        subtitles.Capacity = subtitlesArray.Count;

                        foreach (JSONNode node in subtitlesArray)
                        {
                            subtitles.Add(new Subtitle(node));
                        }
                    }
                }
                catch (NullReferenceException e)
                {
                    // JSON key not found.
                    Plugin.log?.Error($"Error parsing MusixMatch lyrics json response. Response is missing an expected key.");
                    Plugin.log?.Debug(e);
                    if (Settings.VerboseLogging)
                    {
                        Plugin.log?.Debug(req.downloadHandler.text);
                    }
                }
                catch (Exception e)
                {
                    Plugin.log?.Error($"Error parsing MusixMatch lyrics json response: {e.Message}");
                    Plugin.log?.Debug(e);
                }
            }

            req.Dispose();
        }

        public static JSONArray ParseMusixMatchResponse(string jsonResponse)
        {
            JSONNode res = JSON.Parse(jsonResponse);
            MusixMatchStatus status = (MusixMatchStatus)res["message"]["header"]["status_code"].AsInt;
            if (status != MusixMatchStatus.Success)
            {
                Plugin.log?.Error($"Error getting lyrics from MusixMatch: {status.GetReasonString()}");
                return null;
            }
            JSONNode lyricResponse = res["message"]["body"]["macro_calls"]["track.subtitles.get"]["message"];
            MusixMatchStatus lyricResponseStatus = (MusixMatchStatus)lyricResponse["header"]["status_code"].AsInt;
            if (Settings.VerboseLogging)
                Plugin.log?.Debug($"MusixMatch lyric response status is '{lyricResponseStatus}'.");

            switch (lyricResponseStatus)
            {
                case MusixMatchStatus.NotFound:
                    Plugin.log?.Info($"Lyrics not found on MusixMatch.");
                    return null;
                default:
                    if (Settings.VerboseLogging)
                        Plugin.log?.Debug($"MusixMatch lyric response status is '{lyricResponseStatus}': {lyricResponseStatus.GetReasonString()}");
                    break;
            }
            JSONNode subtitleObject = lyricResponse["body"]["subtitle_list"]
                                         .AsArray[0]["subtitle"];

            JSONArray subtitlesArray = JSON.Parse(subtitleObject["subtitle_body"].Value).AsArray;
            if (subtitlesArray == null)
                Plugin.log?.Warn($"MusixMatch does not have lyrics for this song.");
            return subtitlesArray;
        }

        /// <summary>
        ///   Gets an ID that can be used to identify lyrics on the Beat Singer lyrics resolver.
        /// </summary>
        public static string GetLyricsHash(this IBeatmapLevel level)
        {
            string id = string.Join(", ", level.songName, level.songAuthorName, level.songSubName, level.beatsPerMinute, level.songDuration, level.songTimeOffset);

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(id));
        }
    }

    /// <summary>
    /// Status codes that MusixMatch may respond with in the header.
    /// https://developer.musixmatch.com/documentation/status-codes
    /// </summary>
    public enum MusixMatchStatus
    {
        /// <summary>
        /// No status
        /// </summary>
        None = 0,
        /// <summary>
        /// The request was successful.
        /// </summary>
        Success = 200,
        /// <summary>
        /// The request had bad syntax or was inherently impossible to be satisfied.
        /// </summary>
        BadRequest = 400,
        /// <summary>
        /// Authentication failed, probably because of invalid/missing API key.
        /// </summary>
        Unauthorized = 401,
        /// <summary>
        /// The usage limit has been reached, either you exceeded per day requests limits or your balance is insufficient.
        /// </summary>
        UsageLimitReached = 402,
        /// <summary>
        /// You are not authorized to perform this operation.
        /// </summary>
        Forbidden = 403,
        /// <summary>
        /// The requested resource was not found.
        /// </summary>
        NotFound = 404,
        /// <summary>
        /// The requested method was not found.
        /// </summary>
        MethodNotFound = 405,
        /// <summary>
        /// Ops. Something were wrong.
        /// </summary>
        GeneralFailure = 500,
        /// <summary>
        /// Our system is a bit busy at the moment and your request can’t be satisfied.
        /// </summary>
        ServiceUnavailable = 503
    }
}
