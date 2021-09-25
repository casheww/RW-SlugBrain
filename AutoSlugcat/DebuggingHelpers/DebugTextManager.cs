using System.Collections.Generic;
using UnityEngine;

namespace SlugBrain.DebuggingHelpers
{
    public class DebugTextManager
    {
        public DebugTextManager()
        {
            _texts = new List<TextEntry>();
        }

        public bool enabled = true;

        public void Update()
        {
            if (!enabled) return;

            if (Futile.stage == null) return;

            foreach (TextEntry t in _texts)
            {
                if (t.frames < int.MaxValue)
                {
                    t.frames--;
                }
                if (t.frames < 0)
                {
                    t.deleteMe = true;
                }
            }

            List<TextEntry> textsToRemove = new List<TextEntry>();

            float yOffset = 0;
            float leftMargin = margin;

            for (int i = 0; i < _texts.Count; i++)
            {
                TextEntry t = _texts[i];

                if (t.deleteMe) textsToRemove.Add(_texts[i]);
                else
                {
                    if (!t.addedToStage)
                    {
                        Futile.stage.AddChild(t.label);
                        t.addedToStage = true;
                    }

                    t.label.MoveToFront();

                    yOffset += t.label.textRect.height + margin;
                    t.label.SetPosition(leftMargin, Screen.height - yOffset);

                    if (yOffset <= margin)
                    {
                        yOffset = 0;
                        leftMargin += 250;
                    }
                }
            }

            foreach (TextEntry t in textsToRemove)
            {
                Futile.stage.RemoveChild(t.label);
                _texts.Remove(t);
            }
        }

        public void Write(string key, object obj, int frames = int.MaxValue) { Write(key, obj, new Color(0.9f, 0.9f, 1f), frames); }
        public void Write(string key, object obj, Color color, int frames = int.MaxValue)
        {
            if (!enabled) return;
            
            int existingIndex = -1;

            for (int i = 0; i < _texts.Count; i++)
            {
                if (_texts[i].key == key)
                {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex < 0)
            {
                _texts.Add(new TextEntry(key, color, frames));
                existingIndex = _texts.Count - 1;
            }

            string txt = obj == null ? "null" : obj.ToString();
            _texts[existingIndex].label.text = $"{key} : {txt}";
            _texts[existingIndex].label.color = color;
        }

        public void UnWrite(string key)
        {
            if (!enabled) return;
            
            int existingIndex = -1;

            for (int i = 0; i < _texts.Count; i++)
            {
                if (_texts[i].key == key)
                {
                    existingIndex = i;
                    break;
                }
            }

            _texts[existingIndex].deleteMe = true;
        }

        public void Clear()
        {
            foreach (TextEntry t in _texts)
            {
                t.deleteMe = true;
            }
        }

        private readonly List<TextEntry> _texts;

        private const float margin = 15;


        class TextEntry
        {
            public TextEntry(string key, Color color, int frames)
            {
                this.key = key;
                this.frames = frames;

                label = new FLabel("font", "-")
                {
                    alignment = FLabelAlignment.Left,
                    color = color,
                    scale = 1.2f
                };
                label.SetAnchor(Vector2.zero);

                addedToStage = false;
                deleteMe = false;
            }

            public string key;
            public FLabel label;
            public int frames;
            public bool addedToStage;
            public bool deleteMe;
        }

    }
}
