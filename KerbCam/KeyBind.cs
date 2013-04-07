using KSP.IO;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KerbCam {
    public delegate void KeyEvent();
    public delegate void AnyKeyEvent(Event ev);
    public delegate void KeyBindingChangedEvent();

    public struct ExtEvent {
        private Event ev;

        /// <summary>
        /// Acts as the numeric field on ev, as we can't set that
        /// field.
        /// </summary>
        public bool numeric;

        public ExtEvent(KeyCode keyCode, bool numeric) {
            this.ev = Event.KeyboardEvent(keyCode.ToString());
            this.numeric = numeric;
        }

        public ExtEvent(Event ev) {
            if (ev != null) {
                this.ev = new Event(ev);
                this.numeric = ev.numeric;
            } else {
                this.ev = null;
                this.numeric = false;
            }
        }

        public ExtEvent(Event ev, bool numeric) {
            if (ev != null) {
                this.ev = new Event(ev);
            } else {
                this.ev = null;
            }
            this.numeric = numeric;
        }

        public bool IsBound() {
            return ev != null;
        }

        public void Clear() {
            ev = null;
            numeric = false;
        }

        public bool Matches(Event ev) {
            return (
                this.ev != null &&
                this.ev.keyCode == ev.keyCode &&
                this.ev.alt == ev.alt &&
                this.ev.control == ev.control &&
                this.ev.shift == ev.shift &&
                this.ev.command == ev.command);
        }

        /// <summary>
        /// Returns the event in a parseable form.
        /// </summary>
        /// <returns>The event as a string.</returns>
        public override string ToString() {
            if (ev == null) {
                return "";
            }

            StringBuilder s = new StringBuilder(10);

            if (ev.numeric) s.Append("*");
            var mods = ev.modifiers;
            if ((mods & EventModifiers.Alt) != 0) s.Append("&");
            if ((mods & EventModifiers.Control) != 0) s.Append("^");
            if ((mods & EventModifiers.Command) != 0) s.Append("%");
            if ((mods & EventModifiers.Shift) != 0) s.Append("#");
            s.Append(ev.keyCode.ToString());

            return s.ToString();
        }

        public static ExtEvent Parse(string evStr) {
            if (evStr == null || evStr == "") {
                return new ExtEvent(null, false);
            }

            bool numeric = evStr.StartsWith("*");
            if (numeric) {
                evStr = evStr.Substring(1);
            }
            Event ev = Event.KeyboardEvent(evStr);
            return new ExtEvent(ev, numeric);
        }

        /// <summary>
        /// Creates a readable string for the event.
        /// </summary>
        /// <returns>The description string.</returns>
        public string ToHumanString() {
            if (ev == null) {
                return "<unset>";
            }

            List<string> p = new List<string>(5);
            var mods = ev.modifiers;
            if ((mods & EventModifiers.Alt) != 0) p.Add("Alt");
            if ((mods & EventModifiers.Control) != 0) p.Add("Ctrl");
            if ((mods & EventModifiers.Command) != 0) p.Add("Cmd");
            if ((mods & EventModifiers.Shift) != 0) p.Add("Shift");
            string keyDesc;
            if (numeric) {
                keyDesc = "(numpad)" + ev.keyCode.ToString();
            } else {
                keyDesc = ev.keyCode.ToString();
            }
            p.Add(keyDesc);

            return string.Join("+", p.ToArray());
        }
    }

    public class KeyBind {
        private ExtEvent binding;
        private ExtEvent defaultBind;
        private string humanBinding;
        private bool requiredBound;
        public string description;
        public event KeyEvent keyUp;
        public event KeyEvent keyDown;
        public event KeyBindingChangedEvent changed;

        public KeyBind(string description, bool requiredBound, KeyCode defaultKeyCode) {
            this.description = description;
            this.defaultBind = new ExtEvent(defaultKeyCode, false);
            this.requiredBound = requiredBound;
            SetBinding(defaultBind);
        }

        public KeyBind(string description) {
            this.description = description;
            this.defaultBind = new ExtEvent();
            this.requiredBound = false;
            SetBinding(defaultBind);
        }

        public bool IsBound() {
            return binding.IsBound();
        }

        public bool IsRequiredBound() {
            return requiredBound;
        }

        public void SetBinding(ExtEvent ev) {
            binding = ev;
            humanBinding = ev.ToHumanString();
            if (changed != null) {
                changed();
            }
        }

        public void SetBinding(Event ev) {
            SetBinding(new ExtEvent(ev));
        }

        public string HumanBinding {
            get { return humanBinding; }
        }

        public bool MatchAndFireEvent(Event ev) {
            if (!binding.Matches(ev)) {
                return false;
            }

            KeyEvent destEvent;
            if (ev.type == EventType.KeyUp) {
                destEvent = this.keyUp;
            } else if (ev.type == EventType.KeyDown) {
                destEvent = this.keyDown;
            } else {
                return true;
            }
            if (destEvent != null) {
                destEvent();
            }
            ev.Use();
            return true;
        }

        public void SetFromConfig(string evStr) {
            SetBinding(ExtEvent.Parse(evStr));
        }

        public string GetForConfig() {
            return binding.ToString();
        }
    }

    public class KeyBindings<KeyT> : IConfigNode {
        // TODO: Maybe optimize this with a hash of the binding, but be
        // careful about hashes changing when the binding changes.
        private List<KeyBind> bindings =
            new List<KeyBind>();
        private Dictionary<KeyT, KeyBind> keyToBinding =
            new Dictionary<KeyT, KeyBind>();

        /// <summary>
        /// Captures *all* key events. Will block other key events while at
        /// least one delegate is set.
        /// </summary>
        public event AnyKeyEvent captureAnyKey;

        /// <summary>
        /// Any key binding was changed.
        /// </summary>
        public event KeyBindingChangedEvent anyChanged;

        public void AddBinding(KeyT key, KeyBind kb) {
            this.bindings.Add(kb);
            keyToBinding[key] = kb;
            kb.changed += HandleAnyChanged;
        }

        public void ListenKeyUp(KeyT key, KeyEvent del) {
            keyToBinding[key].keyUp += del;
        }

        public void UnlistenKeyUp(KeyT key, KeyEvent del) {
            keyToBinding[key].keyUp -= del;
        }

        public void ListenKeyDown(KeyT key, KeyEvent del) {
            keyToBinding[key].keyDown += del;
        }

        public void UnlistenKeyDown(KeyT key, KeyEvent del) {
            keyToBinding[key].keyDown -= del;
        }

        public void HandleEvent(Event ev) {
            if (ev.isKey && (ev.type == EventType.KeyUp || ev.type == EventType.KeyDown)) {
                if (captureAnyKey != null) {
                    if (ev.type == EventType.KeyUp) {
                        captureAnyKey(ev);
                        ev.Use();
                    }
                } else {
                    foreach (var kb in bindings) {
                        if (kb.MatchAndFireEvent(ev)) {
                            return;
                        }
                    }
                }
            }
        }

        public IEnumerable<KeyBind> Bindings() {
            return bindings;
        }

        public void Load(ConfigNode node) {
            foreach (var key in keyToBinding.Keys) {
                var kb = keyToBinding[key];
                kb.SetFromConfig(node == null ? null : node.GetValue(key.ToString()));
            }
        }

        public void Save(ConfigNode node) {
            foreach (var key in keyToBinding.Keys) {
                var kb = keyToBinding[key];
                node.AddValue(key.ToString(), kb.GetForConfig());
            }
        }

        private void HandleAnyChanged() {
            if (anyChanged != null) {
                anyChanged();
            }
        }
    }
}
