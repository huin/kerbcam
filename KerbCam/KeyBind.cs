using KSP.IO;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KerbCam {
    public delegate void KeyEvent();
    public delegate void AnyKeyEvent(Event ev);
    public delegate void KeyBindingChangedEvent();

    public class KeyBind {
        private Event binding;
        private string humanBinding;
        private Event defaultBind;
        private bool requiredBound;
        public string description;
        public event KeyEvent ev;
        public event KeyBindingChangedEvent changed;

        public KeyBind(string description, bool requiredBound, KeyCode defaultKeyCode) {
            this.description = description;
            this.defaultBind = EventHelper.KeyboardUpEvent(defaultKeyCode.ToString());
            this.requiredBound = requiredBound;
            SetBinding(defaultBind);
        }

        public KeyBind(string description, bool requiredBound, Event defaultBind) {
            this.description = description;
            this.defaultBind = defaultBind;
            this.requiredBound = requiredBound;
            SetBinding(defaultBind);
        }

        public bool IsBound() {
            return binding != null;
        }

        public bool IsRequiredBound() {
            return requiredBound;
        }

        public void SetBinding(Event ev) {
            if (ev != null) {
                binding = new Event(ev);
                humanBinding = EventHelper.KeyboardEventHumanString(binding);
            } else {
                binding = null;
                humanBinding = "<unbound>";
            }
            if (changed != null) 
                changed();
        }

        public string HumanBinding {
            get { return humanBinding; }
        }

        public bool MatchAndFireEvent(Event ev) {
            if (this.binding != null && this.binding.Equals(ev)) {
                if (this.ev != null) {
                    this.ev();
                }
                ev.Use();
                return true;
            }
            return false;
        }

        public void SetFromConfig(string evStr) {
            if (evStr == null) {
                SetBinding(defaultBind);
            } else if (evStr == "") {
                // Explicitly unset.
                SetBinding(null);
            } else {
                // Configured.
                SetBinding(EventHelper.KeyboardUpEvent(evStr));
            }
        }

        public string GetForConfig() {
            if (binding == null) {
                return "";
            } else {
                return EventHelper.KeyboardEventString(binding);
            }
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

        public void Listen(KeyT key, KeyEvent del) {
            keyToBinding[key].ev += del;
        }

        public void Unlisten(KeyT key, KeyEvent del) {
            keyToBinding[key].ev -= del;
        }

        public void HandleEvent(Event ev) {
            if (ev.isKey && ev.type == EventType.KeyUp) {
                if (captureAnyKey != null) {
                    captureAnyKey(ev);
                    ev.Use();
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

    public class EventHelper {
        /// <summary>
        /// Reverse operation of Event.KeyboardEvent/KeyboardUpEvent.
        /// </summary>
        /// <param name="ev">The event to turn into a string.</param>
        /// <returns>The event as a string.</returns>
        public static string KeyboardEventString(Event ev) {
            if (!ev.isKey) {
                throw new Exception("Not a keyboard event: " + ev.ToString());
            }

            StringBuilder s = new StringBuilder(10);
            var mods = ev.modifiers;
            if ((mods & EventModifiers.Alt) != 0) s.Append("&");
            if ((mods & EventModifiers.Control) != 0) s.Append("^");
            if ((mods & EventModifiers.Command) != 0) s.Append("%");
            if ((mods & EventModifiers.Shift) != 0) s.Append("#");
            s.Append(ev.keyCode.ToString());

            return s.ToString();
        }

        public static Event KeyboardUpEvent(string evStr) {
            Event ev = Event.KeyboardEvent(evStr);
            ev.type = EventType.KeyUp;
            return ev;
        }

        /// <summary>
        /// Creates a readable string for the event.
        /// </summary>
        /// <param name="ev">The event to turn into a descriptive string.</param>
        /// <returns>The description string.</returns>
        public static string KeyboardEventHumanString(Event ev) {
            if (!ev.isKey) {
                throw new Exception("Not a keyboard event: " + ev.ToString());
            }

            List<string> p = new List<string>(5);
            var mods = ev.modifiers;
            if ((mods & EventModifiers.Alt) != 0) p.Add("Alt");
            if ((mods & EventModifiers.Control) != 0) p.Add("Ctrl");
            if ((mods & EventModifiers.Command) != 0) p.Add("Cmd");
            if ((mods & EventModifiers.Shift) != 0) p.Add("Shift");
            p.Add(ev.keyCode.ToString());

            return string.Join("+", p.ToArray());
        }
    }
}
