﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.Xna.Framework;
using ImGuiCLI;
using System.Collections;

namespace PropertyData
{
    public enum EditorType
    {
        Default,
        Color,
        Bitmask,
        Transform,
        ObjectRef
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true)]
    [Description("Marks the sort sequence of the property. \"propName\" parameter is only required when placed on a class to specify the target field.")]
    public class PropertyPriorityAttribute : System.Attribute
    {
        public PropertyPriorityAttribute(int level, string propName = "")
        {
            Level = level;
            PropertyName = propName;
        }

        public int Level { get; set; }
        public string PropertyName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = true)]
    [Description("Marks a field to be ignored in property reflection. \"propName\" parameter is only required when placed on a class to specify the target field.")]
    public class PropertyIgnoreAttribute : System.Attribute
    {
        public PropertyIgnoreAttribute(string propName = "")
        {
            PropName = propName;
        }

        public string PropName { get; set; }
        public string EditorSpecific { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    [Description("Indicates which set of custom flags is used for this property.")]
    public class PropertyFlagsAttribute : System.Attribute
    {
        public string BitNames { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    [Description("Allows a class to override the name of fields. Specifically for when the meaning of something changes during inheritance.")]
    public class OverrideNameAttribute : System.Attribute
    {
        public string Target { get; set; }
        public string NewName { get; set; }

        public OverrideNameAttribute(string target, string newName)
        {
            Target = target;
            NewName = newName;
        }
    }

    /// <summary>
    /// Interface for handling a cached property info.
    /// Allows plugging in handlers for different types of fields without diving into a massive if/else tree.
    /// </summary>
    public interface ImPropertyHandler
    {
        /// <summary>
        /// Execute ImGui calls and make data changes
        /// </summary>
        /// <param name="propertyInfo">Property/field info for which UI is emitted</param>
        /// <param name="targetObject">Object whose property/field is being accessed</param>
        void EmitUI(ImGuiControls.ReflectionCache.CachedPropertyInfo propertyInfo, object targetObject);

        /// Typically should return true, return false for something like a checkbox.
        bool RequiresLabel { get; }

        /// <summary>
        /// Generates code for handling the ImGui calls and data manipulation.
        /// </summary>
        /// <param name="targetName">Name of the object instance variable that code is accessing</param>
        /// <param name="accessorName">Name of the field/property having code emitted for</param>
        /// <param name="contextRequestsLabel">Output code should include a special labeling needs</param>
        /// <returns>A string of code handling the ImGui calls and data changes</returns>
        string GenerateCode(string targetName, string accessorName, bool contextRequestsLabel);
    }

    /// <summary>
    /// Handles a complete type.
    /// </summary>
    public interface ImPropertyObjectPage
    {
        /// <summary>
        /// Used for complete emission of a UI for a given object.
        /// </summary>
        /// <param name="forObject"></param>
        void EmitEditPage(object forObject);

        /// <summary>
        /// Outputs a list of column names for this object, used for datagrid column intersection.
        /// </summary>
        List<string> GetColumnFields();

        /// <summary>
        /// Emit UI for a column row.
        /// </summary>
        /// <param name="fieldList">List of fields to be displayed</param>
        /// <param name="forObject">Object the row is for</param>
        void EmitColumns(IList<string> fieldList, object forObject);
    }
}

namespace ImGuiControls
{
    /// <summary>
    /// Generic reflection based inspector.
    /// 
    /// Use the CategoryAttribute to cluster properties into groups.
    /// 
    /// Use the DescriptionAttribute to attach a tip.
    /// </summary>
    public class Inspector
    {
        protected ReflectionCache reflectCache_;
        ImGuiTextFilter filter_;

        /// Whether to use alphabetical display.
        public bool IsAlphabetical { get; set; } = true;

        public bool IsAdvanced { get; set; } = false;

        /// Object being inspected.
        public object Inspecting { get; set; }

        /// <summary>
        /// Construct.
        /// </summary>
        /// <param name="cacheMethod">Reflection cache to use for lookups.</param>
        public Inspector(ReflectionCache cacheMethod)
        {
            filter_ = new ImGuiTextFilter();
            reflectCache_ = cacheMethod;
        }

        /// Makes ImGui calls to display as a window.
        public virtual void DrawAsWindow(string windowName)
        {
            if (ImGuiCli.Begin(windowName, ImGuiWindowFlags_.ResizeFromAnySide | ImGuiWindowFlags_.MenuBar))
            {
                DrawMenuBar();
                Draw(Inspecting);
            }
            ImGuiCli.End();
        }

        public virtual void DrawAsDock(string title)
        {
            if (ImGuiDock.BeginDock(title, ImGuiWindowFlags_.MenuBar | ImGuiWindowFlags_.ResizeFromAnySide))
            {
                DrawMenuBar();
                Draw(Inspecting);
            }
            ImGuiDock.EndDock();
        }

        /// For use when in a custom window, draws the menu-bar.
        public void DrawMenuBar()
        {
            ImGuiCli.BeginMenuBar();
            ImGuiCli.PushItemWidth(ImGuiCli.GetContentRegionAvailWidth() * 0.65f);
            filter_.Draw(ICON_FA.FILTER + " Filter");
            ImGuiCli.SameLine();
            ImGuiCli.SetCursorPosX(ImGuiCli.GetWindowWidth() - ImGuiCli.CalcTextSize(ICON_FA.EYE + ICON_FA.SORT_ALPHA_DOWN + "  ").X - ImGuiStyle.FramePadding.X*3*ImGuiIO.CurrentDPI);
            if (ImGuiCli.Button(IsAlphabetical ? ICON_FA.OBJECT_GROUP : ICON_FA.SORT_ALPHA_DOWN))
                IsAlphabetical = !IsAlphabetical;
            if (ImGuiCli.IsItemHovered())
                ImGuiCli.SetTooltip(IsAlphabetical ? "Group fields" : "Order alphabetically");
            if (ImGuiCli.Button(IsAdvanced ? ICON_FA.EYE : ICON_FA.EYE_SLASH))
                IsAdvanced = !IsAdvanced;
            if (ImGuiCli.IsItemHovered())
                ImGuiCli.SetTooltip(IsAdvanced ? "Showing advanced fields" : "Hiding advanced fields");
            ImGuiCli.EndMenuBar();
        }

        /// For use when in a custom window, draws for the given target.
        public void Draw(object target)
        {
            if (target == null)
            {
                ImGuiCli.Text("< nothing to edit >");
                return;
            }
            
            // Check if there's a "complete" handler for the object
            PropertyData.ImPropertyObjectPage page = null;
            if (reflectCache_.DefinedPages.TryGetValue(target.GetType(), out page))
            {
                page.EmitEditPage(target);
                return;
            }

            if (IsAlphabetical)
            {
                List<ReflectionCache.CachedPropertyInfo> properties = reflectCache_.GetAlphabetical(target.GetType());
                if (!IsAdvanced)
                    properties = properties.Where(p => p.IsAdvanced == false).ToList();
                ImGuiCli.PushItemWidth(-1);
                for (int p = 0; p < properties.Count; ++p)
                {
                    ImGuiCli.PushID(p + 1);
                    DrawField(properties[p], target, true);
                    ImGuiCli.PopID();
                }
                ImGuiCli.PopItemWidth();
            }
            else
            {
                List<ReflectionCache.PropertyGrouping> groups = reflectCache_.GetGrouped(target.GetType());
                ImGuiCli.PushItemWidth(-1);
                for (int i = 0; i < groups.Count; ++i)
                {
                    var props = groups[i].Properties;
                    if (!IsAdvanced)
                        props = props.Where(p => p.IsAdvanced == false).ToList();
                    if (props.Count == 0)
                        continue;
                    if (groups.Count == 1 || ImGuiCli.CollapsingHeader(groups[i].GroupName))
                    {
                        ImGuiCli.Indent();
                        for (int p = 0; p < props.Count; ++p)
                        {
                            ImGuiCli.PushID(p + 1);
                            DrawField(props[p], target, true);
                            ImGuiCli.PopID();
                        }
                        ImGuiCli.Unindent();
                    }
                }
                ImGuiCli.PopItemWidth();
            }
        }

        /// Draws a field from cache info.
        protected void DrawField(ReflectionCache.CachedPropertyInfo info, object target, bool withLabel)
        {
            if (filter_.IsActive && !filter_.PassFilter(info.DisplayName))
                return;

            Type pType = info.Type;
            // bools, arrays, dictionaries, and List<T>s don't show a label
            if ((pType != typeof(bool) && !pType.IsArray && !(pType.IsGenericType && typeof(IList).IsAssignableFrom(pType))) && withLabel)
            {
                ImGuiCli.Text(info.DisplayName);
                if (!string.IsNullOrWhiteSpace(info.Tip))// && ImGuiCli.IsItemHovered())
                {
                    ImGuiCli.SameLine();
                    ImGuiCli.Text(ICON_FA.INFO_CIRCLE);
                    if (ImGuiCli.IsItemHovered())
                        ImGuiCli.SetTooltip(info.Tip);
                }
            }

            string labelLessName = "##" + info.DisplayName;

            if (pType.IsArray)
            {
                Array arr = (Array)info.GetValue(target);
                if (arr == null)
                {
                    ImGuiCli.Text(info.DisplayName + ", ^1NULL");
                    ImGuiCli.SameLine();
                    if (ImGuiCli.Button("Spawn"))
                    {
                        arr = (Array)Activator.CreateInstance(pType, new object[] { 1 });
                        info.SetValue(target, arr);
                    }
                    return;
                }

                bool treeOpen = ImGuiCli.TreeNode(labelLessName);
                ImGuiCli.SameLine();
                ImGuiCli.Text(string.Format(info.DisplayName + " {0}", arr.Length));
                ImGuiCli.SameLine();
                if (!string.IsNullOrWhiteSpace(info.Tip))
                {
                    ImGuiCli.Text(ICON_FA.INFO_CIRCLE);
                    if (ImGuiCli.IsItemHovered())
                        ImGuiCli.SetTooltip(info.Tip);
                    ImGuiCli.SameLine();
                }
                if (ImGuiCli.Button("Nullify"))
                {
                    info.SetValue(target, null);
                    if (treeOpen)
                        ImGuiCli.TreePop();
                    return;
                }
                if (treeOpen)
                {
                    int ct = arr.Length;
                    bool changed = false;
                    if (ImGuiCli.InputInt("##count", ref ct))
                    {
                        PropertyExtMethods.ResizeArray(ref arr, ct);
                        changed = true;
                    }
                    for (int i = 0; i < arr.Length; ++i)
                    {
                        ImGuiCli.PushID(i + 1);

                        object itemVal = arr.GetValue(i);
                        if (info.EditType == PropertyData.EditorType.ObjectRef)
                        {
                            ImGuiCli.Selectable(itemVal != null ? itemVal.ToString() : "< none >", false);
                            if (ImGuiCli.BeginDragDropTarget())
                            {
                                string data = "";
                                if (ImGuiCli.AcceptDragDropPayload("U_OBJREF", ref data))
                                {
                                    object dragObj = DragObjectHelper.Dragging;
                                    DragObjectHelper.Dragging = null;
                                    if (dragObj != null && pType.GetGenericArguments()[0].IsAssignableFrom(dragObj.GetType()))
                                        arr.SetValue(dragObj, i);
                                }
                                ImGuiCli.EndDragDropTarget();
                            }
                            ImGuiCli.SameLine();
                            if (ImGuiCli.Button("X"))
                                arr.SetValue(null, i);
                        }
                        else
                        {
                            if (itemVal == null)
                            {
                                itemVal = Activator.CreateInstance(pType.GetElementType());
                                arr.SetValue(itemVal, i);
                            }
                            bool itemOpen = ImGuiCli.TreeNode("##" + i.ToString());
                            ImGuiCli.SameLine();
                            ImGuiCli.Text(string.Format("{0} : {1}", i, itemVal.ToString()));
                            if (itemOpen)
                            {
                                ImGuiCli.Indent();
                                Draw(itemVal);
                                arr.SetValue(itemVal, i);
                                ImGuiCli.Unindent();
                                ImGuiCli.TreePop();
                            }
                        }
                        ImGuiCli.PopID();
                    }

                    if (changed)
                        info.SetValue(target, arr);
                    ImGuiCli.TreePop();
                }
            }
            else if (pType.IsGenericType && typeof(IList).IsAssignableFrom(pType))
            {
                IList list = (IList)info.GetValue(target);
                if (list == null)
                {
                    list = (IList)Activator.CreateInstance(pType);
                    info.SetValue(target, list);
                }

                bool treeOpen = ImGuiCli.TreeNode(labelLessName);
                ImGuiCli.SameLine();
                ImGuiCli.Text(string.Format(info.DisplayName + " {0}", list.Count));
                ImGuiCli.SameLine();
                if (!string.IsNullOrWhiteSpace(info.Tip))
                {
                    ImGuiCli.Text(ICON_FA.INFO_CIRCLE);
                    if (ImGuiCli.IsItemHovered())
                        ImGuiCli.SetTooltip(info.Tip);
                    ImGuiCli.SameLine();
                }
                if (ImGuiCli.Button("Clear"))
                    list.Clear();

                if (treeOpen)
                {
                    if (ImGuiCli.Button("Add Item"))
                    {
                        if (info.EditType == PropertyData.EditorType.ObjectRef)
                            list.Add(null);
                        else
                            list.Add(Activator.CreateInstance(pType.GetGenericArguments()[0]));
                    }

                    for (int i = 0; i < list.Count; ++i)
                    {
                        ImGuiCli.PushID(i + 1);
                        object itemVal = list[i];
                        if (info.EditType == PropertyData.EditorType.ObjectRef)
                        {
                            ImGuiCli.Text(itemVal != null ? itemVal.ToString() : "< none >");
                            if (ImGuiCli.BeginDragDropTarget())
                            {
                                string data = "";
                                if (ImGuiCli.AcceptDragDropPayload("U_OBJREF", ref data))
                                {
                                    object dragObj = DragObjectHelper.Dragging;
                                    DragObjectHelper.Dragging = null;
                                    if (dragObj != null && pType.GetGenericArguments()[0].IsAssignableFrom(dragObj.GetType()))
                                        list[i] = dragObj;
                                }
                                ImGuiCli.EndDragDropTarget();
                            }
                            ImGuiCli.SameLine();
                            if (ImGuiCli.Button("X"))
                            {
                                list.RemoveAt(i);
                                --i;
                            }
                        }
                        else
                        {
                            if (itemVal == null)
                            {
                                itemVal = Activator.CreateInstance(pType.GetGenericArguments()[0]);
                                list[i] = itemVal;
                            }
                            bool itemOpen = ImGuiCli.TreeNode("##" + i.ToString());
                            ImGuiCli.SameLine();
                            ImGuiCli.Text(string.Format("{0} : {1}", i, itemVal.ToString()));
                            ImGuiCli.SameLine();
                            if (ImGuiCli.Button("X"))
                            {
                                list.RemoveAt(i);
                                --i;
                                if (itemOpen)
                                    ImGuiCli.TreePop();
                                ImGuiCli.PopID();
                                continue;
                            }
                            if (itemOpen)
                            {
                                ImGuiCli.Indent();
                                Draw(itemVal);
                                ImGuiCli.Unindent();
                                ImGuiCli.TreePop();
                            }
                        }
                        ImGuiCli.PopID();
                    }
                    ImGuiCli.TreePop();
                }
            }
            else if (pType == typeof(bool))
            {
                bool value = (bool)info.GetValue(target);
                string lbl = withLabel ? info.DisplayName : "##" + info.DisplayName;
                if (ImGuiCli.Checkbox(lbl, ref value))
                    info.SetValue(target, value);
                if (!string.IsNullOrWhiteSpace(info.Tip) && ImGuiCli.IsItemHovered())
                    ImGuiCli.SetTooltip(info.Tip);
            }
            else if (pType == typeof(int))
            {
                int value = (int)info.GetValue(target);
                if (ImGuiCli.DragInt(labelLessName, ref value))
                    info.SetValue(target, value);
            }
            else if (pType == typeof(uint))
            {
                int value = (int)info.GetValue(target);
                if (ImGuiCli.DragInt(labelLessName, ref value, 1, 0, int.MaxValue))
                    info.SetValue(target, (int)value);
            }
            else if (pType == typeof(float))
            {
                float value = (float)info.GetValue(target);
                if (ImGuiCli.DragFloat(labelLessName, ref value))
                    info.SetValue(target, value);
            }
            else if (pType == typeof(double))
            {
                float value = (float)info.GetValue(target);
                if (ImGuiCli.DragFloat(labelLessName, ref value))
                    info.SetValue(target, (double)value);
            }
            else if (pType == typeof(string))
            {
                string value = (string)info.GetValue(target);
                if (value == null)
                    value = "";
                if (ImGuiCli.InputText(labelLessName, ref value))
                    info.SetValue(target, value);
            }
            else if (pType == typeof(Color))
            {
                Color value = (Color)info.GetValue(target);
                if (ImGuiCli.InputColor(labelLessName, ref value))
                    info.SetValue(target, value);
            }
            else if (pType == typeof(Vector2))
            {
                Vector2 value = (Vector2)info.GetValue(target);
                if (ImGuiEx.DragFloatN_Colored(labelLessName, ref value))
                    info.SetValue(target, value);
            }
            else if (pType == typeof(Vector3))
            {
                Vector3 value = (Vector3)info.GetValue(target);
                if (ImGuiEx.DragFloatN_Colored(labelLessName, ref value))
                    info.SetValue(target, value);
            }
            else if (pType == typeof(Vector4))
            {
                if (info.EditType == PropertyData.EditorType.Color)
                {
                    Vector4 value = (Vector4)info.GetValue(target);
                    if (ImGuiCli.InputColor(labelLessName, ref value))
                        info.SetValue(target, value);
                }
                else
                {
                    Vector4 value = (Vector4)info.GetValue(target);
                    if (ImGuiCli.DragFloat4(labelLessName, ref value))
                        info.SetValue(target, value);
                }
            }
            else if (pType == typeof(Matrix))
            {
                if (info.EditType == PropertyData.EditorType.Transform)
                {
                    Matrix value = (Matrix)info.GetValue(target);
                    if (ImGuiEx.MatrixTransform(ref value, true))
                        info.SetValue(target, value);
                }
                else
                {
                    Matrix value = (Matrix)info.GetValue(target);
                    if (ImGuiEx.DragMatrix(ref value))
                        info.SetValue(target, value);
                }
            }
            else if (pType.IsEnum)
            {
                int value = (int)info.GetValue(target);
                if (ImGuiCli.Combo(labelLessName, ref value, info.enumNames))
                    info.SetValue(target, Enum.GetValues(info.Type).GetValue(value));
            }
            else
            {
                PropertyData.ImPropertyHandler foundHandler = null;
                if (reflectCache_.TypeHandlers.TryGetValue(pType, out foundHandler))
                    foundHandler.EmitUI(info, target);
            }
        }

        /// Emits cooked code that can be used in place of reflection for performance.
        public string GenerateCode(object forObject)
        {
            return GenerateCode(forObject.GetType());
        }

        /// Emits cooked code that can be used in place of reflection for performance.
        public string GenerateCode(Type forType)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("(object target, bool alphabetical) => {");
            sb.AppendFormat("    {0} obj = target as {0};\r\n", forType.Name);
            sb.AppendLine("    if (alphabetical) {");

            var alphabetical = reflectCache_.GetAlphabetical(forType);
            for (int i = 0; i < alphabetical.Count; ++i)
            {
                if (alphabetical[i].Property.PropertyType != typeof(bool))
                    WriteFieldCode(sb, alphabetical[i], i);
            }

            sb.AppendLine("    } else {");

            var grouped = reflectCache_.GetGrouped(forType);
            for (int i = 0; i < grouped.Count; ++i)
            {
                sb.AppendFormat("            if (ImGuiCli.CollapsingHeader(\"{0}\")) {{\r\n", grouped[i].GroupName);
                sb.AppendLine("                ImGuiCli.Indent();");
                for (int p = 0; p < grouped[i].Properties.Count; ++p)
                {
                    WriteFieldCode(sb, grouped[i].Properties[p], p);
                }
                sb.AppendLine  ("                ImGuiCli.Unindent();");
                sb.AppendLine  ("            }");
            }

            sb.AppendLine("    }");
            sb.AppendLine("};");

            return sb.ToString();
        }

        /// Outputs part of the code generation.
        void WriteFieldCode(StringBuilder sb, ReflectionCache.CachedPropertyInfo info, int idx)
        {
            sb.AppendFormat("                ImGuiCli.Text(\"{0}\");\r\n", info.DisplayName);
            if (string.IsNullOrWhiteSpace(info.Tip))
                sb.AppendFormat("                if (ImGuiCli.IsItemHovered()) ImGuiCli.SetTooltip(\"{0}\");\r\n", info.Tip);

            sb.AppendFormat("                ImGuiCli.PushID({0}+1);\r\n", idx);

            Type pType = info.Property.PropertyType;
            if (pType == typeof(bool))
            {
                sb.AppendFormat("                bool b{0} = obj.{1};\r\n", idx, info.AccessName);
                sb.AppendFormat("                if (ImGuiCli.Checkbox(\"{1}\", ref b{0})) obj.{2} = b{0};\r\n", info.DisplayName, idx, info.AccessName);
                if (!string.IsNullOrEmpty(info.Tip))
                    sb.AppendFormat("                if (ImGuiCli.IsItemHovered()) ImGuiCli.SetTooltip(\"{0}\");\r\n", info.Tip);
            }
            else if (pType == typeof(int))
            {
                sb.AppendFormat("                int b{0} = obj.{1};\r\n", idx, info.AccessName);
                sb.AppendFormat("                if (ImGuiCli.DragInt(\"##{1}\", ref b{0})) obj.{2} = b{0};\r\n", info.DisplayName, idx, info.AccessName);
            }
            else if (pType == typeof(float))
            {
                sb.AppendFormat("                float b{0} = obj.{1};\r\n", idx, info.AccessName);
                sb.AppendFormat("                if (ImGuiCli.DragFloat(\"##{1}\", ref b{0})) obj.{2} = b{0};\r\n", info.DisplayName, idx, info.AccessName);
            }
            else if (pType == typeof(string))
            {
                sb.AppendFormat("                string b{0} = obj.{1};\r\n", idx, info.AccessName);
                sb.AppendFormat("                if (b{0} == null) b{0} = \"\";\r\n", idx);
                sb.AppendFormat("                if (ImGuiCli.InputText(\"##{1}\", ref b{0})) obj.{2} = b{0};\r\n", info.DisplayName, idx, info.AccessName);
            }
            else if (pType == typeof(Vector2))
            {
                sb.AppendFormat("                Vector2 b{0} = obj.{1};\r\n", idx, info.AccessName);
                sb.AppendFormat("                if (ImGuiCli.DragFloat2(\"##{1}\", ref b{0})) obj.{2} = b{0};\r\n", info.DisplayName, idx, info.AccessName);
            }
            else if (pType == typeof(Vector3))
            {
                sb.AppendFormat("                Vector3 b{0} = obj.{1};\r\n", idx, info.AccessName);
                sb.AppendFormat("                if (ImGuiCli.DragFloat3(\"##{1}\", ref b{0})) obj.{2} = b{0};\r\n", info.DisplayName, idx, info.AccessName);
            }
            else if (pType == typeof(Vector4))
            {
                sb.AppendFormat("                Vector4 b{0} = obj.{1};\r\n", idx, info.AccessName);
                sb.AppendFormat("                if (ImGuiCli.DragFloat4(\"##{1}\", ref b{0})) obj.{2} = b{0};\r\n", info.DisplayName, idx, info.AccessName);
            }
            else if (pType == typeof(Color))
            {
                sb.AppendFormat("                Color b{0} = obj.{1};\r\n", idx, info.AccessName);
                sb.AppendFormat("                if (ImGuiCli.InputColor(\"##{1}\", ref b{0})) obj.{2} = b{0};\r\n", info.DisplayName, idx, info.AccessName);
            }
            else if (pType == typeof(Matrix))
            {
                sb.AppendFormat("                Matrix b{0} = obj.{1};\r\n", idx, info.AccessName);
                sb.AppendFormat("                if (ImGuiEx.DragMatrix(ref b{0})) obj.{2} = b{0};\r\n", info.DisplayName, idx, info.AccessName);
            }

            sb.AppendLine("                ImGuiCli.PopID();");
        }
    }

    /// <summary>
    /// Specialization designed for showing a list of objects in a grid
    /// for comparing values across them.
    /// 
    /// Recommeded to use the same ReflectionCache as for any existing Inspector.
    /// </summary>
    public class InspectorGrid : Inspector
    {
        bool objectsDirty_ = true;
        IList<object> objects_;
        public IList<object> Objects { get { return objects_; }
            set
            {
                if (objects_ != value)
                {
                    objects_ = value;
                    objectsDirty_ = true;
                }
            }
        }

        /// Construct.
        public InspectorGrid(ReflectionCache cache) : base(cache) { }

        /// Outputs calls as for producing an ImGui window.
        public override void DrawAsWindow(string title)
        {
            if (ImGuiCli.Begin(title, ImGuiWindowFlags_.ResizeFromAnySide))
                Draw();
            ImGuiCli.End();
        }

        public override void DrawAsDock(string title)
        {
            if (ImGuiDock.BeginDock(title, ImGuiWindowFlags_.ResizeFromAnySide))
                Draw();
            ImGuiDock.EndDock();
        }

        /// For use when in a custom window. Produces ImGui calls.
        List<string> infos = null;
        List<ReflectionCache.CachedPropertyInfo>[] tables = null;
        public void Draw()
        {
            if (Objects == null || Objects.Count == 0)
            {
                ImGuiCli.Text("< no objects selected >");
                return;
            }

            if (objectsDirty_)
            {
                infos = null;
                tables = new List<ReflectionCache.CachedPropertyInfo>[Objects.Count];
                Type lastType = null;

                for (int i = 0; i < Objects.Count; ++i)
                {
                    if (Objects[i].GetType() == lastType)
                    {
                        tables[i] = tables[i - 1];
                        continue;
                    }
                    if (infos == null)
                    {
                        tables[i] = reflectCache_.GetAlphabetical(Objects[i].GetType()).ForEditor(Objects[i].GetType(), "DataGrid");
                        if (!IsAdvanced)
                            tables[i] = tables[i].Where(p => p.IsAdvanced == false).ToList();
                        infos = tables[i].Select(s => s.DisplayName).ToList();
                        lastType = Objects[i].GetType();
                    }
                    else
                    {
                        tables[i] = reflectCache_.GetAlphabetical(Objects[i].GetType()).ForEditor(Objects[i].GetType(), "DataGrid");
                        if (!IsAdvanced)
                            tables[i] = tables[i].Where(p => p.IsAdvanced == false).ToList();
                        infos = infos.Intersect(tables[i].Select(s => s.DisplayName).ToList()).ToList();
                        lastType = Objects[i].GetType();
                    }
                }
            }
            if (infos.Count == 0)
            {
                ImGuiCli.Text("< no common fields to edit >");
                return;
            }

            ImGuiCli.PushItemWidth(-1);
            bool useColumns = true;// infos.Count > 1;
            if (useColumns)
                ImGuiCli.Columns(infos.Count);

            for (int i = 0; i < infos.Count; ++i)
            {
                ImGuiCli.Text(infos[i]);
                float w = ImGuiCli.CalcTextSize(infos[i]).X;
                if (useColumns && ImGuiCli.GetColumnWidth(i) < (w + 10) && ImGuiCli.IsItemHovered())
                    ImGuiCli.SetTooltip(infos[i]);
                if (useColumns)
                    ImGuiCli.NextColumn();
            }
            ImGuiCli.Separator();

            ImGuiCli.PushStyleVar(ImGuiStyleVar_.ItemSpacing, 0);
            ImGuiCli.PushStyleVar(ImGuiStyleVar_.FramePadding, 0);
            for (int o = 0; o < Objects.Count; ++o)
            {
                for (int i = 0; i < infos.Count; ++i)
                {
                    var cacheInfo = tables[o].FirstOrDefault(c => c.DisplayName == infos[i]);
                    ImGuiCli.PushID(o * infos.Count + infos[i]);
                    ImGuiCli.PushItemWidth(-1);
                    DrawField(cacheInfo, Objects[o], false);
                    ImGuiCli.PopItemWidth();
                    ImGuiCli.PopID();
                    if (useColumns)
                        ImGuiCli.NextColumn();
                }
                ImGuiCli.Separator();
            }
            ImGuiCli.PopStyleVar(2);
            ImGuiCli.PopItemWidth();
        }
    }

    /// <summary>
    /// Reflection caching is independent from the inspectors so
    /// that reduced (or expansive) means of reflecting can be controlled.
    /// 
    /// Attributes:
    ///     DisplayNameAttribute: use for overriding text
    ///     DescriptionAttribute: use for adding a tip
    ///     EditorBrowsableAttribute: use to mark as invisible/advanced/visible
    ///     EditorAttribute: use to set EditorType enum value from string
    ///     PropertyPriorityAttribute: use to set the sort priority (does not apply to alphabetical)
    ///     PropertyIgnoreAttribute: use to exclude from inclusion
    ///         can be attached to a class, in which case specify the name - able to hide inherited fields
    ///     OverrideNameAttribute: place on a class to have it override a name, for coping with inheritence
    ///     </summary>
    public class ReflectionCache
    {
        public Dictionary<Type, PropertyData.ImPropertyObjectPage> DefinedPages { get; private set; } = new Dictionary<Type, PropertyData.ImPropertyObjectPage>();
        public Dictionary<Type, PropertyData.ImPropertyHandler> TypeHandlers { get; private set; } = new Dictionary<Type, PropertyData.ImPropertyHandler>();

        public class PropertyGrouping
        {
            public string GroupName { get; set; }
            public List<CachedPropertyInfo> Properties { get; private set; } = new List<CachedPropertyInfo>();
        }

        public class CachedPropertyInfo
        {
            public PropertyInfo Property;
            public FieldInfo Field;
            public PropertyData.EditorType EditType = PropertyData.EditorType.Default;
            public string AccessName;
            public string DisplayName;
            public string Tip;
            public Type Type;
            public string[] enumNames;
            public bool IsAdvanced = false;
            public HashSet<string> EditorKeys = new HashSet<string>();
            public HashSet<string> DisabledEditors = new HashSet<string>();

            public CachedPropertyInfo() {  }

            public object GetValue(object target)
            {
                if (Property != null)
                    return Property.GetValue(target);
                return Field.GetValue(target);
            }

            public void SetValue(object target, object value)
            {
                if (Property != null)
                    Property.SetValue(target, value);
                else
                    Field.SetValue(target, value);
            }

            public bool IsReadOnly { get { return Property != null && !Property.CanWrite; } }

            /// Allows more generic handling of property vs field differences.
            public T GetCustomAttribute<T>() where T : System.Attribute
            {
                if (Property != null)
                    return Property.GetCustomAttribute<T>();
                return Field.GetCustomAttribute<T>();
            }
            
            /// Generic handling of property vs field differences.
            public IEnumerable<T> GetCustomAttributes<T>() where T : System.Attribute
            {
                if (Property != null)
                    return Property.GetCustomAttributes<T>();
                return Field.GetCustomAttributes<T>();
            }
        }

        Dictionary<Type, List<PropertyGrouping>> GroupedCache = new Dictionary<Type, List<PropertyGrouping>>();
        Dictionary<Type, List<CachedPropertyInfo>> AlphabeticalCache = new Dictionary<Type, List<CachedPropertyInfo>>();
        Dictionary<Type, List<CachedPropertyInfo>> Cache = new Dictionary<Type, List<CachedPropertyInfo>>();
        bool scanFields_;
        bool scanProperties_;

        public ReflectionCache(bool includeProperties, bool includeFields)
        {
            scanFields_ = includeFields;
            scanProperties_ = includeProperties;
        }

        public List<PropertyGrouping> GetGrouped(Type type)
        {
            if (GroupedCache.ContainsKey(type))
                return GroupedCache[type];

            List<CachedPropertyInfo> properties = GetOrdered(type);
            HashSet<PropertyInfo> used = new HashSet<PropertyInfo>();

            List<PropertyGrouping> ret = new List<PropertyGrouping>();
            Dictionary<string, PropertyGrouping> createdGroupings = new Dictionary<string, PropertyGrouping>();

            foreach (var propInfo in properties)
            {
                CategoryAttribute grpAttr = null;
                if (propInfo.Property != null)
                    grpAttr = propInfo.Property.GetCustomAttribute<CategoryAttribute>();
                else
                    grpAttr = propInfo.Field.GetCustomAttribute<CategoryAttribute>();

                string catName = grpAttr != null ? grpAttr.Category : "Misc";

                PropertyGrouping target = null;
                if (createdGroupings.ContainsKey(catName))
                    target = createdGroupings[catName];
                else
                {
                    target = new PropertyGrouping { GroupName = catName };
                    createdGroupings[catName] = target;
                    ret.Add(target);
                }

                target.Properties.Add(propInfo);
            }

            // order by group name
            ret = ret.OrderBy((o) => o.GroupName).ToList();
            GroupedCache[type] = ret;
            return ret;
        }

        public List<CachedPropertyInfo> GetOrdered(Type type)
        {
            if (Cache.ContainsKey(type))
                return Cache[type];

            List<CachedPropertyInfo> ret = new List<CachedPropertyInfo>();
            if (scanProperties_)
            {
                PropertyInfo[] infos = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var val in infos)
                {
                    CachedPropertyInfo info = new CachedPropertyInfo { Property = val, AccessName = val.Name, Type = val.PropertyType };
                    ret.Add(info);
                }
            }
            if (scanFields_)
            {
                FieldInfo[] infos = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var val in infos)
                {
                    CachedPropertyInfo info = new CachedPropertyInfo { Field = val, Type = val.FieldType, AccessName = val.Name };
                    ret.Add(info);
                }
            }

            ret = Filter(type, ret);

            for (int i = 0; i < ret.Count; ++i)
                FillAttributes(type, ret[i]);

            ret.Sort((lhs, rhs) => {
                var lhsAttr = lhs.GetCustomAttribute<PropertyData.PropertyPriorityAttribute>();
                var rhsAttr = rhs.GetCustomAttribute<PropertyData.PropertyPriorityAttribute>();
                if (lhsAttr != null && rhsAttr == null)
                    return -1;
                else if (lhsAttr == null && rhsAttr != null)
                    return 1;
                else if (lhsAttr != null && rhsAttr != null)
                {
                    if (lhsAttr.Level == rhsAttr.Level)
                        return 0;
                    return lhsAttr.Level < rhsAttr.Level ? -1 : 1;
                }
                return 1;
            });

            Cache[type] = ret;
            return ret;
        }

        public List<CachedPropertyInfo> GetAlphabetical(Type type)
        {
            if (AlphabeticalCache.ContainsKey(type))
                return AlphabeticalCache[type];

            List<CachedPropertyInfo> ret = new List<CachedPropertyInfo>();

            if (scanProperties_)
            {
                PropertyInfo[] infos = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var val in infos)
                {
                    CachedPropertyInfo info = new CachedPropertyInfo { Property = val, AccessName = val.Name, Type = val.PropertyType };
                    ret.Add(info);
                }
            }
            if (scanFields_)
            {
                FieldInfo[] infos = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var val in infos)
                {
                    CachedPropertyInfo info = new CachedPropertyInfo { Field = val, Type = val.FieldType, AccessName = val.Name };
                    ret.Add(info);
                }
            }

            ret = Filter(type, ret);

            for (int i = 0; i < ret.Count; ++i)
                FillAttributes(type, ret[i]);

            var r = ret.OrderBy(o => o.DisplayName).ToList();
            AlphabeticalCache[type] = r;
            return r;
        }

        static List<CachedPropertyInfo> Filter(Type type, List<CachedPropertyInfo> infos)
        {
            List<CachedPropertyInfo> processing = new List<CachedPropertyInfo>();
            processing.AddRange(infos.Where((p) => {
                // Property ignore attribute
                PropertyData.PropertyIgnoreAttribute[] ignores = p.GetCustomAttributes<PropertyData.PropertyIgnoreAttribute>().ToArray();
                if (ignores != null)
                {
                    for (int i = 0; i < ignores.Length; ++i)
                        if (ignores[i].EditorSpecific == null)
                            return false;
                }

                // Browsable to remove
                var browsable = p.GetCustomAttribute<BrowsableAttribute>();
                if (browsable != null && browsable.Browsable == false)
                    return false;

                // EditorBrowsable to remove
                var editBrowse = p.GetCustomAttribute<EditorBrowsableAttribute>();
                if (editBrowse != null && editBrowse.State == EditorBrowsableState.Never)
                    return false;
                return true;
            }));

            var typeLevelIgnores = type.GetCustomAttributes<PropertyData.PropertyIgnoreAttribute>();
            if (typeLevelIgnores != null)
            {
                var ignoreNames = typeLevelIgnores.Select(p => p.PropName);
                processing = processing.Where((p) => { return !ignoreNames.Contains(p.AccessName); }).ToList();
            }

            return processing;
        }

        static void FillAttributes(Type type, CachedPropertyInfo info)
        {
            // Allow marking with a pretty name
            var lbl = info.GetCustomAttribute<DisplayNameAttribute>();
            if (lbl != null)
                info.DisplayName = lbl.DisplayName;
            else // or just split camel case
                info.DisplayName = info.AccessName.SplitCamelCase();

            PropertyData.PropertyIgnoreAttribute[] ignores = info.GetCustomAttributes<PropertyData.PropertyIgnoreAttribute>().ToArray();
            if (ignores != null)
            {
                for (int i = 0; i < ignores.Length; ++i)
                {
                    if (ignores[i].EditorSpecific != null)
                        info.DisabledEditors.Add(ignores[i].EditorSpecific);
                }
            }

            // Allow the type to override the name whenever its meaning may change
            PropertyData.OverrideNameAttribute[] nameOverrides = type.GetCustomAttributes<PropertyData.OverrideNameAttribute>().ToArray();
            if (nameOverrides != null)
            {
                for (int j = 0; j < nameOverrides.Length; ++j)
                    if (info.AccessName.Equals(nameOverrides[j].Target))
                        info.DisplayName = nameOverrides[j].NewName;
            }

            EditorAttribute[] editors = info.GetCustomAttributes<EditorAttribute>().ToArray();
            if (editors != null)
            {
                for (int j = 0; j < editors.Length; ++j)
                    info.EditType = (PropertyData.EditorType)Enum.Parse(typeof(PropertyData.EditorType),editors[j].EditorTypeName);
                    //info.EditorKeys.Add(editors[j].EditorTypeName);
            }

            // Mark out fields that are advanced
            EditorBrowsableAttribute browser = info.GetCustomAttribute<EditorBrowsableAttribute>();
            if (browser != null)
                info.IsAdvanced = browser.State == EditorBrowsableState.Advanced;

            // Grab enum names if necessary
            if (info.Type.IsEnum)
                info.enumNames = Enum.GetNames(info.Type);

            var tip = info.GetCustomAttribute<DescriptionAttribute>();
            if (tip != null)
                info.Tip = tip.Description;
        }
    }

    public static class PropertyExtMethods
    {
        public static string SplitCamelCase(this string input)
        {
            StringBuilder ret = new StringBuilder();

            bool lastWasLower = false;
            for (int i = 0; i < input.Length; ++i)
            {
                if (char.IsUpper(input[i]) || char.IsDigit(input[i]))
                {
                    if (lastWasLower)
                    {
                        ret.Append(' ');
                        ret.Append(input[i]);
                    }
                    else if (i > 0 && i < input.Length - 1 && char.IsLower(input[i + 1]))
                    {
                        ret.Append(' ');
                        ret.Append(input[i]);
                    }
                    else
                        ret.Append(input[i]);
                    lastWasLower = false;
                }
                else if (char.IsSymbol(input[i]))
                    ret.Append(input[i]);
                else
                {
                    ret.Append(input[i]);
                    lastWasLower = true;
                }
            }
            return ret.ToString();
            //return System.Text.RegularExpressions.Regex.Replace(input, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
        }

        internal struct EditorKey
        {
            internal Type type_;
            internal string editor_;
        }
        static Dictionary<EditorKey, List<ReflectionCache.CachedPropertyInfo>> forEditorCache_ = new Dictionary<EditorKey, List<ReflectionCache.CachedPropertyInfo>>();

        public static List<ReflectionCache.CachedPropertyInfo> ForEditor(this List<ReflectionCache.CachedPropertyInfo> src, Type t, string editor)
        {
            EditorKey k = new ImGuiControls.PropertyExtMethods.EditorKey { type_ = t, editor_ = editor };
            if (forEditorCache_.ContainsKey(k))
                return forEditorCache_[k];
            List<ReflectionCache.CachedPropertyInfo> ret = new List<ReflectionCache.CachedPropertyInfo>();
            ret.AddRange(src);
            for (int i = 0; i < ret.Count; ++i)
            {
                if (ret[i].EditorKeys.Count != 0 && !ret[i].EditorKeys.Contains(editor))
                {
                    ret.RemoveAt(i);
                    --i;
                }
                if (ret[i].DisabledEditors.Count != 0 && ret[i].DisabledEditors.Contains(editor))
                {
                    ret.RemoveAt(i);
                    --i;
                }
            }
            forEditorCache_[k] = ret;
            return ret;
        }

        public static void ResizeArray(ref Array array, int n)
        {
            var type = array.GetType();
            var elemType = type.GetElementType();
            var resizeMethod = typeof(Array).GetMethod("Resize", BindingFlags.Static | BindingFlags.Public);
            var properResizeMethod = resizeMethod.MakeGenericMethod(elemType);
            var parameters = new object[] { array, n };
            properResizeMethod.Invoke(null, parameters);
            array = (Array)parameters[0];
        }
    }
}
