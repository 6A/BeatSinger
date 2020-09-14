using BeatSaberMarkupLanguage.TypeHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace BeatSinger.Components
{
    public class SimpleLyricSpawner : MonoBehaviour, ILyricSpawner
    {
        TextMeshProUGUI FloatingText;
        public Vector3 Position;
        private DateTime EndTime;
        private Vector3 Offset = new Vector3(0, 0, 3);
        private int FontSizeOffset = 15;
        private Vector3 FacingPosition = new Vector3(0, 1.7f, 0);

        public void Awake()
        {
            FloatingText = CreateText();
            FloatingText.transform.position = Plugin.config.Position + Offset;
        }

        public void Update()
        {
            if (DateTime.UtcNow >= EndTime)
            {
                FloatingText.text = "";
            }
        }

        public void OnDisable()
        {
            FloatingText.text = "";
        }


        public void SpawnText(string text, float duration) => SpawnText(text, duration, false, null, 4f);

        public void SpawnText(string text, float duration, bool enableShake, Color? color, float fontSize)
        {
            EndTime = DateTime.UtcNow + TimeSpan.FromSeconds(duration);
            if (color.HasValue)
                FloatingText.color = color.Value;
            FloatingText.fontSize = fontSize + FontSizeOffset;
            FloatingText.text = text;
            Vector3 position = Plugin.config.Position + Offset;
            position.y = position.y * 0.4f;
            FloatingText.transform.position = position;
            FacePosition(FloatingText.transform, FacingPosition);
        }
        public TextMeshProUGUI CreateText(string text = null)
        {
            Canvas _canvas = new GameObject("BailOutFailText").AddComponent<Canvas>();
            _canvas.gameObject.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            _canvas.renderMode = RenderMode.WorldSpace;
            (_canvas.transform as RectTransform).sizeDelta = new Vector2(0f, 0f);
            return CreateText(_canvas, new Vector2(0f, 0f), (_canvas.transform as RectTransform).sizeDelta, text);
        }

        private TextMeshProUGUI CreateText(Canvas parent, Vector2 anchoredPosition, Vector2 sizeDelta, string text = null)
        {
            GameObject gameObj = parent.gameObject;
            gameObj.SetActive(false);
            TextMeshProUGUI textMesh = gameObj.AddComponent<TextMeshProUGUI>();
            /*
            Teko-Medium SDF No Glow
            Teko-Medium SDF
            Teko-Medium SDF No Glow Fading
            */
            var font = Instantiate(Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First(t => t.name == "Teko-Medium SDF No Glow"));
            if (font == null)
            {
                Plugin.log?.Error("Could not locate font asset, unable to display text");
                return null;
            }
            textMesh.font = font;
            textMesh.fontSize = Plugin.config.TextSize + FontSizeOffset;
            textMesh.rectTransform.SetParent(parent.transform as RectTransform, false);
            textMesh.text = text;
            textMesh.color = Plugin.config.TextColor;
            textMesh.rectTransform.anchorMin = new Vector2(0f, 0f);
            textMesh.rectTransform.anchorMax = new Vector2(0f, 0f);
            textMesh.rectTransform.sizeDelta = sizeDelta;
            textMesh.rectTransform.anchoredPosition = anchoredPosition;
            textMesh.alignment = TextAlignmentOptions.Center;
            FacePosition(textMesh.gameObject.transform, FacingPosition);
            gameObj.SetActive(true);
            return textMesh;
        }

        public static void FacePosition(Transform obj, Vector3 targetPos)
        {
            var rotAngle = Quaternion.LookRotation(obj.position - targetPos);
            obj.rotation = rotAngle;
        }
    }
}
