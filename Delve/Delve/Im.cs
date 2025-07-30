using DelveLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiCLI;
using Microsoft.Xna.Framework;

namespace Delve
{
    public class ImModal
    {
        Action okay;
        Action cancel;
        Action drawAction;
        string title;
        string text;
        bool rendered = false;
        public ImModal(string title, string text, Action okay, Action cancel)
        {
            this.text = text;
            this.title = title;
            this.okay = okay;
            this.cancel = cancel;
        }

        public ImModal(string title, string text, Action okay, Action cancel, Action drawAction)
        {
            this.text = text;
            this.title = title;
            this.okay = okay;
            this.cancel = cancel;
            this.drawAction = drawAction;
        }

        public bool Draw()
        {
            if (!rendered)
                ImGuiCli.OpenPopup(title);
            rendered = true;
            bool retVal = false;
            ImGuiCli.SetNextWindowSize(new Vector2(400, 280), ImGuiCond_.Appearing);
            if (ImGuiCli.BeginPopupModal(title, ImGuiWindowFlags_.Modal))
            {
                if (drawAction != null)
                    drawAction();
                else
                    ImGuiCli.TextWrapped(text);
                ImGuiCli.Spacing();
                ImGuiCli.Separator();
                if (okay != null && cancel != null)
                {
                    if (ImGuiCli.Button("Okay"))
                    {
                        okay();
                        retVal = true;
                    }
                    ImGuiCli.SameLine();
                    if (ImGuiCli.Button("Cancel"))
                    {
                        cancel();
                        retVal = true;
                    }
                }
                else if (okay != null)
                {
                    if (ImGuiCli.Button("Okay"))
                    {
                        okay();
                        retVal = true;
                    }
                }
                else if (cancel != null)
                {
                    if (ImGuiCli.Button("Cancel"))
                    {
                        cancel();
                        retVal = true;
                    }
                }
                ImGuiCli.EndPopup();
            }
            return retVal;
        }
    }

    public static class ImAids
    {
        static List<ImModal> modals_ = new List<ImModal>();
        static string[] CurveNames = Enum.GetNames(typeof(CurveType));

        public static bool DrawResponseCurve(ref ResponseCurve curve)
        {
            float[] values = curve.Cook(64);

            ImGuiCli.PushItemWidth(-1);
            ImGuiCli.PlotLines("##curve", values, 0, 0.0f, 1.0f, new Vector2(ImGuiCli.GetContentRegionAvailWidth(), 100));
            ImGuiCli.PopItemWidth();

            float x = curve.XIntercept;
            float y = curve.YIntercept;
            float sl = curve.SlopeIntercept;
            float ex = curve.Exponent;
            bool flipx = curve.FlipX;
            bool flipy = curve.FlipY;
            int idx = (int)curve.CurveShape;

            bool changed = false;

            if (ImGuiCli.TreeNode("Configuration"))
            {
                ImGuiCli.PopItemWidth();
                if (ImGuiCli.Combo("Type", ref idx, CurveNames))
                {
                    curve.CurveShape = (CurveType)idx;
                    changed = true;
                }
                if (ImGuiCli.DragFloat("X", ref x, 0.01f, 0.0f, 0.0f))
                {
                    curve.XIntercept = x;
                    changed = true;
                }
                if (ImGuiCli.DragFloat("Y", ref y, 0.01f, 0.0f, 0.0f))
                {
                    curve.YIntercept = y;
                    changed = true;
                }
                if (ImGuiCli.DragFloat("Slope", ref sl, 0.01f, 0.0f, 0.0f))
                {
                    curve.SlopeIntercept = sl;
                    changed = true;
                }
                if (ImGuiCli.DragFloat("Exponent", ref ex, 0.01f, 0.0f, 0.0f))
                {
                    curve.Exponent = ex;
                    changed = true;
                }

                ImGuiCli.PushItemWidth(-1);
                if (ImGuiCli.Checkbox("Flip X", ref flipx))
                {
                    curve.FlipX = flipx;
                    changed = true;
                }
                ImGuiCli.SameLine();
                if (ImGuiCli.Checkbox("Flip Y", ref flipy))
                {
                    curve.FlipY = flipy;
                    changed = true;
                }
                float w = (ImGuiCli.GetContentRegionAvailWidth() - ImGuiCli.CalcTextSize("Reset").X) / 2;
                ImGuiCli.SetCursorPosX(ImGuiCli.GetCursorPosX() + w);
                if (ImGuiCli.Button("Reset"))
                {
                    curve.XIntercept = curve.YIntercept = 0;
                    curve.SlopeIntercept = curve.Exponent = 1;
                    curve.FlipX = curve.FlipY = false;
                    changed = true;
                }
                ImGuiCli.TreePop();
            }

            return changed;
        }

        public static bool DrawGradient(ref Curve curve)
        {
            bool changed = false;

            var cursorPos = ImGuiCli.GetCursorPos();
            float width = ImGuiCli.GetContentRegionAvailWidth();
            //ImGuiCli.Dummy(new Vector2(width, 64));

            float[] values = new float[64];
            for (int i = 0; i < 64; ++i)
                values[i] = curve.Evaluate(i / 63.0f);

            ImGuiCli.PlotLines("##curve", values, 0, 0.0f, 1.0f, new Vector2(width, 100));

            var newCursorPos = ImGuiCli.GetCursorPos();
            var padding = ImGuiStyle.FramePadding;
            float adjustedHeight = 100 - padding.Y * 2;
            float adjustedWidth = width - padding.X * 2;
            for (int i = 0; i < curve.Keys.Count; ++i)
            {
                ImGuiCli.SetCursorPos(cursorPos + new Vector2(padding.X + curve.Keys[i].Position * adjustedWidth, padding.Y + (1.0f - curve.Keys[i].Value) * adjustedHeight - 3));
                ImGuiCli.PushID(i + 1);
                ImGuiCli.Button("##A", new Vector2(12,12));
                if (ImGuiCli.IsItemHovered() && ImGuiCli.IsItemClicked(0))
                {
                    curve.Keys[i].Value += ImGuiIO.MouseDelta.Y;// / 100;
                    curve.Keys[i].Value = Mathf.Clamp01(curve.Keys[i].Value);
                    curve.ComputeTangents(CurveTangent.Smooth);
                    changed = true;
                }
                ImGuiCli.PopID();
            }

            for (int i = 0; i < curve.Keys.Count; ++i)
            {
                var key = curve.Keys[i];
            }
            ImGuiCli.SetCursorPos(newCursorPos);
            return changed;
        }

        static Dictionary<Type, List<int>> EnumValues = new Dictionary<Type, List<int>>();
        static List<int> GetEnumValues<T>()
        {
            if (EnumValues.ContainsKey(typeof(T)))
                return EnumValues[typeof(T)];

            Array arr = Enum.GetValues(typeof(T));
            List<int> enumValues = new List<int>();
            for (int i = 0; i < arr.GetLength(0); ++i)
                enumValues.Add((int)arr.GetValue(i));
            EnumValues.Add(typeof(T), enumValues);
            return enumValues;
        }
        static Dictionary<Type, string[]> EnumNames = new Dictionary<Type, string[]>();
        static string[] GetEnumNames<T>()
        {
            if (EnumNames.ContainsKey(typeof(T)))
                return EnumNames[typeof(T)];

            string[] enumValues = Enum.GetNames(typeof(T));
            EnumNames.Add(typeof(T), enumValues);
            return enumValues;
        }

        public static bool EnumCombo<T>(string label, ref T value)
        {
            string[] names = GetEnumNames<T>();
            var values = GetEnumValues<T>();

            int idx = (int)Convert.ChangeType(value, typeof(int));
            int trueIdx = values.IndexOf(idx);
            if (ImGuiCli.Combo(label, ref trueIdx, names))
            {
                value = (T)Convert.ChangeType(values[trueIdx], typeof(T));
                return true;
            }
            return false;
        }

        public static void PushModal(ImModal mod)
        {
            modals_.Add(mod);
        }

        public static bool ImGuiHasInput
        {
            get
            {
                return ImGuiIO.WantCaptureKeyboard || ImGuiIO.WantCaptureMouse || ImGuiIO.WantTextInput ||
                    modals_.Count > 0 || ImGuiCli.IsAnyItemActive();
            }
        }

        public static void ProcessModals()
        {
            for (int i = 0; i < modals_.Count; ++i)
            {
                if (modals_[i].Draw())
                {
                    modals_.RemoveAt(i);
                    --i;
                    continue;
                }
                return;
            }
        }
    }
}
