using System;
using System.ComponentModel;

namespace KerbCam {
    class ConfigUtil {
        public static string GetValue(ConfigNode node, string name, string _default) {
            string valueStr = node.GetValue(name);
            if (valueStr == null) {
                return _default;
            } else {
                return valueStr;
            }
        }

        public static void Parse<Type>(ConfigNode node, string name,
            out Type value, Type _default) {

            string valueStr = node.GetValue(name);
            if (valueStr == null) {
                value = _default;
            } else {
                var converter = TypeDescriptor.GetConverter(typeof(Type));
                try {
                    value = (Type)converter.ConvertFromString(valueStr);
                } catch (NotSupportedException) {
                    value = _default;
                }
            }
        }

        public static void Write<Type>(ConfigNode node, string name,
            Type value) {
            var converter = TypeDescriptor.GetConverter(typeof(Type));
            string valueStr = converter.ConvertToString(value);
            node.AddValue(name, valueStr);
        }
    }
}
