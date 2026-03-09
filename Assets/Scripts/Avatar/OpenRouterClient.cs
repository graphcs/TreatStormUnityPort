using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SnackAttack.Avatar
{
    public class OpenRouterClient
    {
        private const string API_URL = "https://openrouter.ai/api/v1/chat/completions";
        private const string VISION_MODEL = "openai/gpt-4o";
        private const string IMAGE_MODEL = "google/gemini-3.1-flash-image-preview";
        private const int MAX_RETRIES = 2;

        private readonly string _apiKey;

        public OpenRouterClient(string apiKey)
        {
            _apiKey = apiKey;
        }

        /// <summary>
        /// Analyze an image using GPT-4o vision. Returns text description.
        /// </summary>
        public IEnumerator AnalyzeImage(string photoBase64, string prompt,
            Action<string> onResult, Action<string> onError)
        {
            var content = new List<object>
            {
                new Dictionary<string, object> { { "type", "text" }, { "text", prompt } },
                new Dictionary<string, object>
                {
                    { "type", "image_url" },
                    { "image_url", new Dictionary<string, object>
                        {
                            { "url", $"data:image/png;base64,{photoBase64}" },
                            { "detail", "high" }
                        }
                    }
                }
            };

            var payload = new Dictionary<string, object>
            {
                { "model", VISION_MODEL },
                { "messages", new List<object>
                    {
                        new Dictionary<string, object> { { "role", "user" }, { "content", content } }
                    }
                },
                { "max_tokens", 1000 }
            };

            string responseText = null;
            string error = null;
            yield return MakeRequest(payload, 120, r => responseText = r, e => error = e);

            if (error != null)
            {
                onError?.Invoke(error);
                yield break;
            }

            string text = ExtractText(responseText);
            if (string.IsNullOrEmpty(text))
                onError?.Invoke("Vision model returned empty response");
            else
                onResult?.Invoke(text);
        }

        /// <summary>
        /// Generate an image from a photo + prompt + optional reference images.
        /// Returns raw PNG bytes.
        /// </summary>
        public IEnumerator GenerateImageFromPhoto(string photoBase64, string prompt,
            List<string> referenceImagesBase64, string aspectRatio,
            Action<byte[]> onResult, Action<string> onError)
        {
            var content = new List<object>
            {
                new Dictionary<string, object> { { "type", "text" }, { "text", prompt } },
                new Dictionary<string, object>
                {
                    { "type", "image_url" },
                    { "image_url", new Dictionary<string, object>
                        {
                            { "url", $"data:image/png;base64,{photoBase64}" },
                            { "detail", "high" }
                        }
                    }
                }
            };

            if (referenceImagesBase64 != null)
            {
                foreach (string refB64 in referenceImagesBase64)
                {
                    content.Add(new Dictionary<string, object>
                    {
                        { "type", "image_url" },
                        { "image_url", new Dictionary<string, object>
                            {
                                { "url", $"data:image/png;base64,{refB64}" },
                                { "detail", "high" }
                            }
                        }
                    });
                }
            }

            var payload = new Dictionary<string, object>
            {
                { "model", IMAGE_MODEL },
                { "messages", new List<object>
                    {
                        new Dictionary<string, object> { { "role", "user" }, { "content", content } }
                    }
                },
                { "modalities", new List<string> { "image", "text" } },
                { "max_tokens", 4096 }
            };

            if (!string.IsNullOrEmpty(aspectRatio))
            {
                payload["image_config"] = new Dictionary<string, object>
                {
                    { "aspect_ratio", aspectRatio }
                };
            }

            string responseText = null;
            string error = null;
            yield return MakeRequest(payload, 180, r => responseText = r, e => error = e);

            if (error != null)
            {
                onError?.Invoke(error);
                yield break;
            }

            byte[] imageBytes = ExtractImage(responseText);
            if (imageBytes == null || imageBytes.Length == 0)
                onError?.Invoke("Image generation returned no image data");
            else
                onResult?.Invoke(imageBytes);
        }

        private IEnumerator MakeRequest(Dictionary<string, object> payload, int timeoutSeconds,
            Action<string> onResult, Action<string> onError)
        {
            string json = MiniJson.Serialize(payload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            for (int attempt = 0; attempt <= MAX_RETRIES; attempt++)
            {
                using var request = new UnityWebRequest(API_URL, "POST");
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = timeoutSeconds;
                request.SetRequestHeader("Authorization", $"Bearer {_apiKey}");
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("HTTP-Referer", "https://snackattack.game");
                request.SetRequestHeader("X-Title", "Jazzy's Treat Storm");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    onResult?.Invoke(request.downloadHandler.text);
                    yield break;
                }

                long code = request.responseCode;
                bool isRetryable = code == 429 || code == 500 || code == 502 || code == 503;

                if (isRetryable && attempt < MAX_RETRIES)
                {
                    float wait = (attempt + 1) * 5f;
                    Debug.Log($"[OpenRouter] HTTP {code}, retrying in {wait}s (attempt {attempt + 1}/{MAX_RETRIES})");
                    yield return new WaitForSecondsRealtime(wait);
                    continue;
                }

                string errorBody = request.downloadHandler?.text ?? request.error;
                string errorMsg = $"OpenRouter API error (HTTP {code}): {errorBody}";

                // Check for auth errors specifically
                if (code == 401 || code == 403)
                    errorMsg = "Invalid API key. Please check your OpenRouter API key and try again.";

                Debug.LogError($"[OpenRouter] {errorMsg}");
                onError?.Invoke(errorMsg);
                yield break;
            }
        }

        private string ExtractText(string responseJson)
        {
            try
            {
                var response = MiniJson.Deserialize(responseJson) as Dictionary<string, object>;
                if (response == null) return null;

                var choices = response["choices"] as List<object>;
                if (choices == null || choices.Count == 0) return null;

                var choice = choices[0] as Dictionary<string, object>;
                var message = choice["message"] as Dictionary<string, object>;
                object contentObj = message["content"];

                if (contentObj is string str) return str;

                // Content may be a list of parts
                if (contentObj is List<object> parts)
                {
                    var sb = new StringBuilder();
                    foreach (var part in parts)
                    {
                        if (part is Dictionary<string, object> partDict &&
                            partDict.TryGetValue("type", out object typeObj) &&
                            (string)typeObj == "text" &&
                            partDict.TryGetValue("text", out object textObj))
                        {
                            sb.Append((string)textObj);
                        }
                    }
                    return sb.ToString();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OpenRouter] Failed to extract text: {e.Message}");
            }
            return null;
        }

        private byte[] ExtractImage(string responseJson)
        {
            try
            {
                var response = MiniJson.Deserialize(responseJson) as Dictionary<string, object>;
                if (response == null) return null;

                var choices = response["choices"] as List<object>;
                if (choices == null || choices.Count == 0) return null;

                var choice = choices[0] as Dictionary<string, object>;
                var message = choice["message"] as Dictionary<string, object>;

                // Check images array (OpenRouter format)
                if (message.TryGetValue("images", out object imagesObj) &&
                    imagesObj is List<object> images && images.Count > 0)
                {
                    var img = images[0] as Dictionary<string, object>;
                    if (img != null && img.TryGetValue("image_url", out object urlObj))
                    {
                        var urlDict = urlObj as Dictionary<string, object>;
                        if (urlDict != null && urlDict.TryGetValue("url", out object url))
                        {
                            return DecodeDataUri((string)url);
                        }
                    }
                }

                // Fallback: check content parts for inline images
                if (message.TryGetValue("content", out object contentObj) &&
                    contentObj is List<object> parts)
                {
                    foreach (var part in parts)
                    {
                        if (part is Dictionary<string, object> partDict &&
                            partDict.TryGetValue("type", out object typeObj) &&
                            (string)typeObj == "image_url" &&
                            partDict.TryGetValue("image_url", out object imgUrlObj))
                        {
                            var imgUrlDict = imgUrlObj as Dictionary<string, object>;
                            if (imgUrlDict != null && imgUrlDict.TryGetValue("url", out object u))
                            {
                                return DecodeDataUri((string)u);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OpenRouter] Failed to extract image: {e.Message}");
            }
            return null;
        }

        private byte[] DecodeDataUri(string dataUri)
        {
            if (string.IsNullOrEmpty(dataUri)) return null;
            if (!dataUri.StartsWith("data:")) return null;

            int commaIdx = dataUri.IndexOf(',');
            if (commaIdx < 0) return null;

            string base64Data = dataUri.Substring(commaIdx + 1);
            return Convert.FromBase64String(base64Data);
        }
    }

    /// <summary>
    /// Minimal JSON serializer/deserializer for Unity (handles nested dicts and lists).
    /// Unity's JsonUtility doesn't handle Dictionary, so we use this lightweight approach.
    /// </summary>
    internal static class MiniJson
    {
        public static string Serialize(object obj)
        {
            if (obj == null) return "null";
            if (obj is string s) return "\"" + EscapeString(s) + "\"";
            if (obj is bool b) return b ? "true" : "false";
            if (obj is int i) return i.ToString();
            if (obj is long l) return l.ToString();
            if (obj is float f) return f.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (obj is double d) return d.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (obj is Dictionary<string, object> dict)
            {
                var sb = new StringBuilder("{");
                bool first = true;
                foreach (var kv in dict)
                {
                    if (!first) sb.Append(",");
                    sb.Append("\"").Append(EscapeString(kv.Key)).Append("\":");
                    sb.Append(Serialize(kv.Value));
                    first = false;
                }
                sb.Append("}");
                return sb.ToString();
            }

            if (obj is System.Collections.IList list)
            {
                var sb = new StringBuilder("[");
                bool first = true;
                foreach (var item in list)
                {
                    if (!first) sb.Append(",");
                    sb.Append(Serialize(item));
                    first = false;
                }
                sb.Append("]");
                return sb.ToString();
            }

            return "\"" + EscapeString(obj.ToString()) + "\"";
        }

        public static object Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            int index = 0;
            return ParseValue(json, ref index);
        }

        private static string EscapeString(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        private static void SkipWhitespace(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index])) index++;
        }

        private static object ParseValue(string json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length) return null;

            char c = json[index];
            if (c == '{') return ParseObject(json, ref index);
            if (c == '[') return ParseArray(json, ref index);
            if (c == '"') return ParseString(json, ref index);
            if (c == 't' || c == 'f') return ParseBool(json, ref index);
            if (c == 'n') { index += 4; return null; }
            return ParseNumber(json, ref index);
        }

        private static Dictionary<string, object> ParseObject(string json, ref int index)
        {
            var dict = new Dictionary<string, object>();
            index++; // skip {
            SkipWhitespace(json, ref index);
            if (index < json.Length && json[index] == '}') { index++; return dict; }

            while (index < json.Length)
            {
                SkipWhitespace(json, ref index);
                string key = ParseString(json, ref index);
                SkipWhitespace(json, ref index);
                index++; // skip :
                dict[key] = ParseValue(json, ref index);
                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ',') { index++; continue; }
                break;
            }
            if (index < json.Length && json[index] == '}') index++;
            return dict;
        }

        private static List<object> ParseArray(string json, ref int index)
        {
            var list = new List<object>();
            index++; // skip [
            SkipWhitespace(json, ref index);
            if (index < json.Length && json[index] == ']') { index++; return list; }

            while (index < json.Length)
            {
                list.Add(ParseValue(json, ref index));
                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ',') { index++; continue; }
                break;
            }
            if (index < json.Length && json[index] == ']') index++;
            return list;
        }

        private static string ParseString(string json, ref int index)
        {
            index++; // skip opening "
            var sb = new StringBuilder();
            while (index < json.Length)
            {
                char c = json[index++];
                if (c == '"') return sb.ToString();
                if (c == '\\' && index < json.Length)
                {
                    char next = json[index++];
                    switch (next)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case '/': sb.Append('/'); break;
                        case 'u':
                            if (index + 4 <= json.Length)
                            {
                                string hex = json.Substring(index, 4);
                                sb.Append((char)Convert.ToInt32(hex, 16));
                                index += 4;
                            }
                            break;
                        default: sb.Append(next); break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static object ParseNumber(string json, ref int index)
        {
            int start = index;
            bool isFloat = false;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && char.IsDigit(json[index])) index++;
            if (index < json.Length && json[index] == '.') { isFloat = true; index++; }
            while (index < json.Length && char.IsDigit(json[index])) index++;
            if (index < json.Length && (json[index] == 'e' || json[index] == 'E'))
            {
                isFloat = true;
                index++;
                if (index < json.Length && (json[index] == '+' || json[index] == '-')) index++;
                while (index < json.Length && char.IsDigit(json[index])) index++;
            }

            string numStr = json.Substring(start, index - start);
            if (isFloat)
            {
                double.TryParse(numStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double dv);
                return dv;
            }
            if (long.TryParse(numStr, out long lv))
            {
                if (lv >= int.MinValue && lv <= int.MaxValue) return (int)(long)lv;
                return lv;
            }
            return 0;
        }

        private static object ParseBool(string json, ref int index)
        {
            if (json.Substring(index, 4) == "true") { index += 4; return true; }
            index += 5; return false;
        }
    }
}
