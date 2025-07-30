using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using LightJson;

namespace DelveLib.Proc
{
    public static class VolExt
    {
        public static List<KeyValuePair<string,JsonValue>> GetValues(this JsonObject obj)
        {
            List<KeyValuePair<string, JsonValue>> ret = new List<KeyValuePair<string, JsonValue>>();
            var enumer = obj.GetEnumerator();
            while (enumer.MoveNext())
                ret.Add(enumer.Current);
            return ret;
        }

        public static string[] getStringArray(this JsonValue value)
        {
            List<string> ret = new List<string>();
            if (value.IsJsonArray)
            {
                var arr = value.AsJsonArray;
                foreach (var a in arr)
                    ret.Add(a.AsString);
            }
            else
                return new string[] { value.AsString };
            return ret.ToArray();
        }

        public static Dictionary<string,JsonValue> Clone(this Dictionary<string,JsonValue> val)
        {
            Dictionary<string, JsonValue> ret = new Dictionary<string, JsonValue>();
            foreach (var kvp in val)
                ret.Add(kvp.Key, kvp.Value);
            return ret;
        }

        public static List<string> JSON_GetStringArray(this JsonObject obj)
        {
            List<string> ret = new List<string>();
            
            return ret;
        }


        public static string getString(this JsonObject obj, string key, string defVal)
        {
            if (!obj.ContainsKey(key))
                return defVal;
            return obj[key].AsString;
        }
    }

    public class VolumePartitioner
    {
        public static Action<string> LogFunction;

        public VolumePartitioner parent;
        public Dictionary<string, JsonValue> methodTable = new Dictionary<string, JsonValue>();
        public Dictionary<string, JsonValue> tempMethodTable = new Dictionary<string, JsonValue>();
        public List<JsonValue> ruleStack = new List<JsonValue>();
        public List<OcclusionArea> occluders = new List<OcclusionArea>();
        public List<VolumePartitioner> children = new List<VolumePartitioner>();

        public Dictionary<string, string> defines = new Dictionary<string, string>();
        public Dictionary<string, double> variables = new Dictionary<string, double>();

        Vector3 min = Vector3.Zero;
        Vector3 max = Vector3.Zero;
        Vector3 parentPos = Vector3.Zero;
        Matrix parentTransform = Matrix.Identity;
        Matrix localTransform = Matrix.Identity;
        Matrix composedTransform = Matrix.Identity;
        Matrix invComposedTransform = Matrix.Identity;
        Vector3 snap = Vector3.Zero;

        VolumePartitioner(Vector3 min, Vector3 max, JsonObject rule, Dictionary<String, JsonObject> methodTable, VolumePartitioner parent, List<OcclusionArea> occluders)
        {
            this.min = min;
            this.max = max;
            this.occluders.AddRange(occluders);
            localTransform = parentTransform = invComposedTransform = Matrix.Identity;

            if (parent != null)
            {
                parentTransform = parent.composedTransform;
                parentPos = (parent.min + parent.max) * 0.5f;

                Vector3 mc = (min + max) * 0.5f;
                Vector3 diff = mc - parentPos;
                localTransform.Translation = diff;
                tempMethodTable = parent.tempMethodTable.Clone();
            }
        }

        /// Retrieves the topmost partitioner unit
        public VolumePartitioner TopMost
        {
            get
            {
                if (parent != null)
                    return parent.TopMost;
                return this;
            }
        }

        public void Transform(Matrix transform)
        {
            localTransform = localTransform * transform;
            composedTransform = parentTransform * localTransform;
            invComposedTransform = Matrix.Invert(composedTransform);
        }
        static void Swap<T>(ref T a, ref T b)
        {
            T temp = b;
            b = a;
            a = temp;
        }
        public void TransformVolume(Matrix transform)
        {
            localTransform = localTransform * transform;
            composedTransform = parentTransform * localTransform;
            invComposedTransform = Matrix.Invert(composedTransform);

            Vector3 center = (min + max) * 0.5f;
            min -= center;
            max -= center;

            min = Vector3.Transform(min, transform);
            max = Vector3.Transform(max, transform);

            if (max.X < min.X)
                Swap(ref max.X, ref min.X);
            if (max.Y < min.Y)
                Swap(ref max.Y, ref min.Y);
            if (max.Z < min.Z)
                Swap(ref max.Z, ref min.Z);

            min += center;
            max += center;
        }

        public void ApplyCoords(string coords)
        {
            int pos = 0;

            String nX = "";
            String nY = "";
            String nZ = "";
            String coord = "";

            for (int i = 0; i < coords.Length; i++)
            {
                String c = "" + coords[i];
                coord += c;
                var lowC = c.ToLowerInvariant();
                if (lowC == "x" || lowC == "y" || lowC == "z")
                {
                    if (pos == 0)
                        nX = coord;
                    else if (pos == 1)
                        nY = coord;
                    else if (pos == 2)
                        nZ = coord;

                    pos++;
                    coord = "";
                }
            }
            SetCoords(nX, nY, nZ);
        }
        public void SetCoords(string X, string Y, string Z)
        {
            Matrix rotation = Matrix.Identity;
            float x = X.Length > 1 ? parseEquation(X.Substring(0, X.Length - 1), 0, variables) : 0;
            float y = Y.Length > 1 ? parseEquation(Y.Substring(0, Y.Length - 1), 0, variables) : 0;
            float z = Z.Length > 1 ? parseEquation(Z.Substring(0, Z.Length - 1), 0, variables) : 0;

            if (x != 0) rotation = rotation * Matrix.CreateRotationX(MathHelper.ToRadians(x));
            if (y != 0) rotation = rotation * Matrix.CreateRotationY(MathHelper.ToRadians(y));
            if (z != 0) rotation = rotation * Matrix.CreateRotationZ(MathHelper.ToRadians(z));

            TransformVolume(rotation);
        }
        public void Evaluate(float x, float y, float z)
        {
            processResize("X", x);
            processResize("Y", y);
            processResize("Z", z);
            EvaluateInternal(this);
        }

        public float GetVal(string axis, Vector3 vals)
        {
            if (axis.Length > 2 || axis.Length == 0)
            {
                LogFunction("VolumePartitioner: Invalid axis, " + axis);
                return vals.X;
            }
            if (axis.Substring(axis.Length - 1, axis.Length).ToLower() == "x")
                return vals.X;
            else if (axis.Substring(axis.Length - 1, axis.Length).ToLower() == "y")
                return vals.Y;
            else if (axis.Substring(axis.Length - 1, axis.Length).ToLower() == "z")
                return vals.Z;
            else
                LogFunction("VolumePartitioner: Invalid axis, " + axis);
            return vals.X;
        }
        public void ModVal(string axis, ref Vector3 vals, float val)
        {
            if (axis.Substring(axis.Length - 1, axis.Length).ToLower() == "x")
                vals.X += val;
            else if (axis.Substring(axis.Length - 1, axis.Length).ToLower() == "y")
                vals.Y += val;
            else if (axis.Substring(axis.Length - 1, axis.Length).ToLower() == "z")
                vals.Z += val;
            else
                LogFunction("VolumePartitioner: Invalid ModVal axis: " + axis);
        }
        public void SetVal(string axis, ref Vector3 vals, float val)
        {
            if (axis.Substring(axis.Length - 1, axis.Length).ToLower() == "x")
                vals.X = val;
            else if (axis.Substring(axis.Length - 1, axis.Length).ToLower() == "y")
                vals.Y = val;
            else if (axis.Substring(axis.Length - 1, axis.Length).ToLower() == "z")
                vals.Z = val;
            else
                LogFunction("VolumePartitioner: Invalid SetVal axis: " + axis);
        }

        void repeat(string eqn, int repeats, float offset, JsonObject ruleOffset, string offsetCoord, JsonObject ruleSub, string ruleCoord, JsonObject ruleRemainder, string remainderCoord, JsonObject repeatRule, string axis)
        {
        }
        void processRepeat(JsonObject repeat, string axis) { }
        void processSplit(JsonObject split) { }
        void processDivide(JsonObject divide, string axis) { }
        void processSelect(JsonObject select) { }
        void processResize(string axis, float val)
        {
            Vector3 lastPos = (min + max) * 0.5f;

            float interval = GetVal(axis, max) - GetVal(axis, min);
            val /= 2.0f;
		
		    int snapVal = (int)Math.Round(GetVal(axis, snap));
		    if (snapVal == 1)
                SetVal(axis, ref min, GetVal(axis, max)-(val*2));
		    else if (snapVal == 0)
		    {
                SetVal(axis, ref min, interval / 2.0f + GetVal(axis, min));
                SetVal(axis, ref max, GetVal(axis, min));
                ModVal(axis, ref min, -val);
                ModVal(axis, ref max, val);
            }
		    else if (snapVal == -1)
                SetVal(axis, ref max, GetVal(axis, min)+(val*2));
		    else
                throw new Exception("Invalid snap val: "+snap+" for axis: "+axis);

            Vector3 newPos = (min + max) * 0.5f;
            Vector3 diff = newPos - lastPos;

            localTransform.Translation += diff;

            composedTransform = parentTransform * localTransform;
            invComposedTransform = Matrix.Invert(composedTransform);
        }
        void processMultiConditional(JsonObject conditional, bool canInterrupt)
        {
            var values = conditional.GetValues();
            foreach (var val in values)
            {
                List<string> conditions = parseCSV(val.Key);
                bool pass = true;

                if (!val.Key.Equals("else") && conditions.Count > 0)
                {
                    foreach (var cond in conditions)
                    {
                        if (!evaluateConditional(cond))
                        {
                            pass = false;
                            break;
                        }
                    }
                }

                if (pass)
                {
                    
                }
            }
        }
        void processOcclude(JsonObject occlude)
        {
            String xeqn = occlude.getString("X", "100%");
            String yeqn = occlude.getString("Y", "100%");
            String zeqn = occlude.getString("Z", "100%");

            String oxeqn = occlude.getString("OX", "0");
            String oyeqn = occlude.getString("OY", "0");
            String ozeqn = occlude.getString("OZ", "0");

            float dx = max.X - min.X;
            float dy = max.Y - min.Y;
            float dz = max.Z - min.Z;

            float x = parseEquation(xeqn, dx, variables) / 2.0f;
            float y = parseEquation(yeqn, dy, variables) / 2.0f;
            float z = parseEquation(zeqn, dz, variables) / 2.0f;

            float ox = parseEquation(oxeqn, dx, variables) / 2.0f;
            float oy = parseEquation(oyeqn, dy, variables) / 2.0f;
            float oz = parseEquation(ozeqn, dz, variables) / 2.0f;

		    Vector3 tmin = new Vector3();
		    Vector3 tmax = new Vector3();
		
		    dx /= 2;
		    dy /= 2;
		    dz /= 2;
		
		    if (snap.X == -1)
		    {
			    tmin.X = -dx+ox;
			    tmax.X = -dx+x*2+ox;
		    }
		    else if (snap.X == 1)
		    {
			    tmin.X = dx-x*2+ox;
			    tmax.X = dx+ox;
		    }
		    else
		    {
			    tmin.X = -x+ox;
			    tmax.X = x+ox;
		    }
		
		    if (snap.Y == -1)
		    {
			    tmin.Y = -dy+oy;
			    tmax.Y = -dy+y*2+oy;
		    }
		    else if (snap.Y == 1)
		    {
			    tmin.Y = dy-y*2+oy;
			    tmax.Y = dy+oy;
		    }
		    else
		    {
			    tmin.Y = -y+oy;
			    tmax.Y = y+oy;
		    }
		
		    if (snap.Z == -1)
		    {
			    tmin.Z = -dz+oz;
			    tmax.Z = -dz+z*2+oz;
		    }
		    else if (snap.Z == 1)
		    {
			    tmin.Z = dz-z*2+oz;
			    tmax.Z = dz+oz;
		    }
		    else
		    {
			    tmin.Z = -z+oz;
			    tmax.Z = z+oz;
		    }
				
		    BoundingBox bb = new BoundingBox(Vector3.Transform(tmin, composedTransform), Vector3.Transform(tmax, composedTransform));
		    
		    String name = occlude.getString("Name", "");

            occluders.Add(new OcclusionArea(bb, name, this));
		
        }

        bool processRuleStack() { return false; }
        void processRuleBlock(JsonObject rule) { }
        bool processRule(JsonObject current, bool canInterrupt) { return false; }

        bool evaluateConditional(string condition) { return false; }
        float parseEquationWithException(string equation, float interval, Dictionary<string, double> variables) { return 0; }
        float parseEquation(string equation, float interval, Dictionary<string, double> variables) { return 0; }

        static void EvaluateInternal(VolumePartitioner root) { }
        static void loadImportsAndBuildMethodTable(List<string> importedFiles, JsonObject root, Dictionary<string, JsonObject> methodTable, string fileName, Dictionary<string, string> renameTable, bool addMain) {
            var imports = root["Imports"];
            if (imports != JsonValue.Null)
            {
                string[] files = imports.getStringArray();
                foreach (var file in files)
                {
                    importedFiles.Add(file);

                    string fn = file.Substring(file.LastIndexOf('/') + 1);
                    fn = fn.Substring(0, fn.LastIndexOf('.') + 1);

                    string fileContent = System.IO.File.ReadAllText(file);
                    JsonObject nroot = LightJson.Serialization.JsonReader.Parse(fileContent);

                    loadImportsAndBuildMethodTable(importedFiles, nroot, methodTable, fn, renameTable, false);
                }
            }

            IEnumerator<KeyValuePair<string,JsonValue>> values = root.GetEnumerator();
            while (values.MoveNext())
            {
                var name = values.Current.Key;
                JsonValue value = values.Current.Value;
                if (name.Equals("Main", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (addMain)
                    {

                    }
                }
                else if (name.Equals("Imports", StringComparison.InvariantCultureIgnoreCase))
                {

                }
                else
                {
                    methodTable.Add(fileName + name, value);
                    renameTable.Add(name, fileName + name);
                }
            }

            correctRenames(root, renameTable);
            renameTable.Clear();
        }
        static void correctRenames(JsonValue current, Dictionary<string, string> renameTable)
        {
            if (current.IsString)
            {
                string cString = current.AsString;
                if (!string.IsNullOrEmpty(cString))
                {
                    if (renameTable.ContainsKey(cString))
                    {
                        current = renameTable[cString];
                    }
                    else
                    {
                        List<string> split = parseCSV(cString);
                        bool change = false;
                        for (int i = 0; i < split.Count; i++)
                        {
                            if (renameTable.ContainsKey(split[i]))
                            {
                                split[i] = renameTable[split[i]];
                                change = true;
                            }
                        }

                        if (change)
                        {
                            String combined = split[0];
                            for (int i = 1; i < split.Count; i++)
                                combined += "," + split[i];
                            current = combined;
                        }
                    }
                }
            }
            else if (current.IsJsonArray)
            {
                var arr = current.AsJsonArray;
                for (int i = 0; i < arr.Count; ++i)
                    correctRenames(current[i], renameTable);
            }
            else if (current.IsJsonObject)
            {
                var obj = current.AsJsonObject;
                foreach (var v in obj)
                    correctRenames(v.Value, renameTable);
            }
        }

        static char[] csvDelimiters = new char[] { '(', ')' };
        static List<String> parseCSV(string csv)
        {
            List<String> store = new List<string>();
            store.Capacity = 16;
            string builder = "";
            int delimiter = -1;

            for (int i = 0; i < csv.Length; i++)
            {
                char c = csv[i];
                if (delimiter == -1)
                {
                    if (c == ',')
                    {
                        store.Add(builder);
                        builder = "";
                    }
                    else
                    {
                        builder += c;

                        for (int d = 0; d< 2; d++)
                        {
                            if (csvDelimiters[d] == c)
                            {
                                delimiter = d;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    builder += c;

                    if (csvDelimiters[delimiter] == c)
                        delimiter = -1;
                }
            }

            if (builder.Length > 0)
                store.Add(builder);

            for (int i = 0; i < store.Count; i++)
                store[i] = store[i].Trim();
            return store;
        }
    }
}
