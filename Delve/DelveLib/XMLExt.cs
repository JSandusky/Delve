using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;

namespace DelveLib
{
    public static class XMLExt
    {
        public static bool GetBoolChild(this XmlElement elem, string name, bool defVal = false)
        {
            var nd = elem.SelectSingleNode(name);
            if (nd != null)
                return bool.Parse(nd.InnerText);
            return defVal;
        }

        public static XmlElement GetChild(this XmlElement elem, string name)
        {
            var nd = elem.SelectSingleNode(name);
            if (nd != null)
                return nd as XmlElement;
            return null;
        }

        public static float GetFloatChild(this XmlElement elem, string name, float defVal = 0.0f)
        {
            var nd = elem.SelectSingleNode(name);
            if (nd != null)
                return float.Parse(nd.InnerText);
            return defVal;
        }

        public static string GetStringChild(this XmlElement elem, string name)
        {
            var nd = elem.SelectSingleNode(name);
            if (nd != null)
                return nd.InnerText;
            return null;
        }

        public static bool GetBoolAttribute(this XmlElement elem, string name, bool defVal = false)
        {
            if (!elem.HasAttribute(name))
                return defVal;
            return bool.Parse(elem.GetAttribute(name));
        }

        public static Color GetColorAttribute(this XmlElement elem, string name, Color defVal)
        {
            if (!elem.HasAttribute(name))
                return defVal;
            //TODO
            return defVal;
        }

        public static int GetIntAttribute(this XmlElement elem, string name, int defVal = 0)
        {
            if (!elem.HasAttribute(name))
                return defVal;
            return int.Parse(elem.GetAttribute(name));
        }

        public static uint GetUIntAttribute(this XmlElement elem, string name, uint defVal = 0)
        {
            if (!elem.HasAttribute(name))
                return defVal;
            return uint.Parse(elem.GetAttribute(name));
        }

        public static float GetFloatAttribute(this XmlElement elem, string name, float defVal = 0)
        {
            if (!elem.HasAttribute(name))
                return defVal;
            return float.Parse(elem.GetAttribute(name));
        }
    }
}
