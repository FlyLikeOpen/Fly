using Fly.Framework.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace Fly.UI.Common
{
	public static class HtmlHelperExtensions
	{
		#region TagInput

		public static MvcHtmlString TagInput(this HtmlHelper htmlHelper, string name, object tagAttributes = null)
		{
			return TagInput(htmlHelper, name, tagAttributes, null, null);
		}

		public static MvcHtmlString TagInput(this HtmlHelper htmlHelper, string name, string[] values)
		{
			return TagInput(htmlHelper, name, null, null, values);
		}

		public static MvcHtmlString TagInput(this HtmlHelper htmlHelper, string name, object tagAttributes, string[] values)
		{
			return TagInput(htmlHelper, name, tagAttributes, null, values);
		}

		public static MvcHtmlString TagInput(this HtmlHelper htmlHelper, string name, object tagAttributes, object htmlAttributes, string[] values = null)
		{
			var dic = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
			string fullName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
			StringBuilder attr = new StringBuilder();
			if (tagAttributes != null)
			{
				foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(tagAttributes))
				{
					object v = descriptor.GetValue(tagAttributes);
					Type t = v.GetType();
					if (t.IsPrimitive && !t.IsClass && t != typeof(char))
					{
						if (attr.Length > 0)
						{
							attr.Append(", ");
						}
						attr.AppendFormat("{0}: {1}", descriptor.Name, v.ToString().ToLower());
					}
					else
					{
						if (attr.Length > 0)
						{
							attr.Append(", ");
						}
						attr.AppendFormat("{0}: '{1}'", descriptor.Name, v);
					}
				}
			}
			TagBuilder tagBuilder = new TagBuilder("input");
			tagBuilder.MergeAttributes<string, object>(dic);
			tagBuilder.MergeAttribute("name", fullName, true);
			tagBuilder.MergeAttribute("id", fullName, true);
			tagBuilder.MergeAttribute("type", "text", true);
			if (attr.Length > 0)
			{
				tagBuilder.MergeAttribute("data-tags", "{ " + attr + " }", true);
			}
			var c = "tags";
			if (dic.ContainsKey("class"))
			{
				c += " " + dic["class"];
			}
			tagBuilder.MergeAttribute("class", c, true);
			if (values != null && values.Length > 0)
			{
				tagBuilder.MergeAttribute("value", string.Join(",", values), true);
			}
			return MvcHtmlString.Create(tagBuilder.ToString(TagRenderMode.SelfClosing));
		}

		#endregion

        public static MvcHtmlString DateTimePicker(this HtmlHelper htmlHelper, string name, bool readonly1 = false, DateTime? initialTime = null,
            bool onlyPickDate = false, bool onlyPickTime = false, DateTime? minTime = null, DateTime? maxTime = null, string notBefore = null, string notAfter = null,
            object inputHtmlAttribute = null, string size = null, bool onlyPickMonth = false)
        {
            TagBuilder inputTag = new TagBuilder("input");
            var dic = HtmlHelper.AnonymousObjectToHtmlAttributes(inputHtmlAttribute);
            inputTag.MergeAttributes<string, object>(dic);
            inputTag.MergeAttribute("type", "text", true);
            inputTag.MergeAttribute("name", name, true);

            var cls = "form-control dtp-input";
            if (dic.ContainsKey("class"))
                cls += " " + dic["class"];
            inputTag.MergeAttribute("class", cls, true);

            if (readonly1)
                inputTag.MergeAttribute("readonly", "readonly", true);

            string defaultDate = "";
            if (initialTime.HasValue)
                defaultDate = initialTime.Value.ToString("yyyy-MM-dd HH:mm:ss");

            string min1 = "";
            if (minTime.HasValue)
                min1 = minTime.Value.ToString("yyyy-MM-dd HH:mm:ss");

            string max2 = "";
            if (maxTime.HasValue)
                max2 = maxTime.Value.ToString("yyyy-MM-dd HH:mm:ss");

            string format = "YYYY-MM-DD HH:mm:ss";
            if (onlyPickDate)
                format = "YYYY-MM-DD";
            else if (onlyPickTime)
                format = "HH:mm:ss";
            else if (onlyPickMonth)
                format = "YYYY-MM";

            string[] sizes = new string[] { "lg", "sm" };
            string controlSize = "";
            if (!string.IsNullOrWhiteSpace(size) && sizes.Contains(size))
                controlSize = "input-group-" + size;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("<div class=\"input-group {0} date dtp-date\" id=\"{1}-dtp-container\" data-format=\"{2}\" data-notbefore=\"{3}\" data-notafter=\"{4}\" data-defaultdate=\"{5}\" data-mindate=\"{6}\" data-maxdate=\"{7}\" >", controlSize, name, format, notBefore, notAfter, defaultDate, min1, max2));
            sb.AppendLine(inputTag.ToString(TagRenderMode.SelfClosing));
            sb.AppendLine("<span class=\"input-group-addon\"><i class=\"fa fa-calendar\"></i></span>");
            sb.AppendLine("</div>");

            return MvcHtmlString.Create(sb.ToString());
        }

		#region CheckBox
		public static MvcHtmlString CheckBoxGroup(this HtmlHelper htmlHelper, string name,
			IEnumerable options, string TextPropertyName, string ValuePropertyName, IEnumerable<object> selectedItems = null, object htmlAttributes = null)
		{
			List<SelectListItem> list = new List<SelectListItem>();
			if (options != null)
			{
				foreach (var obj in options)
				{
					if (obj.GetType().Name.StartsWith("<>f__AnonymousType"))
					{
						var collection = TypeDescriptor.GetProperties(obj);
						var p_text = collection.Find(TextPropertyName, false);
						if (p_text == null)
						{
							throw new ApplicationException("Not found the property '" + TextPropertyName + "'");
						}
						var p_value = collection.Find(ValuePropertyName, false);
						if (p_value == null)
						{
							throw new ApplicationException("Not found the property '" + ValuePropertyName + "'");
						}
						var s = p_value.GetValue(obj);
						var t = p_text.GetValue(obj);
						list.Add(new SelectListItem()
						{
							Text = t == null ? null : t.ToString(),
							Value = s == null ? null : s.ToString(),
							Selected = selectedItems != null && selectedItems.Count() > 0 && (selectedItems.Contains(obj) || selectedItems.Contains(s) || (selectedItems.First() is string && s is string && selectedItems.Select(c=>c.ToString()).Contains(s.ToString())))
						});
					}
					else
					{
						var s = Invoker.PropertyGet(obj, ValuePropertyName);
						var t = Invoker.PropertyGet(obj, TextPropertyName);
                        //bool tmp_selected = (selectedItems != null && selectedItems.FirstOrDefault() is string && s is string && selectedItems.Select(c => c.ToString()).Contains(s.ToString()));
						list.Add(new SelectListItem()
						{
							Text = t == null ? null : t.ToString(),
							Value = s == null ? null : s.ToString(),
                            Selected = selectedItems != null && selectedItems.Count() > 0 && (selectedItems.Contains(obj) || selectedItems.Contains(s) || (selectedItems.First() is string && s is string && selectedItems.Select(c => c.ToString()).Contains(s.ToString())))//selectedItems != null && (selectedItems.Contains(obj) || tmp_selected ||(selectedItems.Contains(s)))
						});
					}
				}
			}
			return CheckBoxGroup(htmlHelper, name, list, htmlAttributes);
		}

		public static MvcHtmlString CheckBoxGroup(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> options = null, object htmlAttributes = null)
		{
			StringBuilder itemsHtml = new StringBuilder();
			if (options != null)
			{
				foreach (var item in options)
				{
					TagBuilder checkboxBuilder = new TagBuilder("input");

					checkboxBuilder.Attributes["type"] = "checkbox";
					checkboxBuilder.Attributes["name"] = name;

					if (item.Value != null)
					{
						checkboxBuilder.Attributes["value"] = item.Value;
					}
					if (item.Selected)
					{
						checkboxBuilder.Attributes["checked"] = "checked";
					}

					TagBuilder builder = new TagBuilder("label")
					{
						InnerHtml = checkboxBuilder.ToString(TagRenderMode.Normal) + item.Text
					};

					itemsHtml.Append(builder.ToString(TagRenderMode.Normal));
				}
			}
			var dic = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
			string fullName = htmlHelper == null ? name : htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
			TagBuilder tagBuilder = new TagBuilder("div");
			tagBuilder.MergeAttributes<string, object>(dic);
			tagBuilder.Attributes["class"] = "checkbox";
			tagBuilder.InnerHtml = itemsHtml.ToString();
			return MvcHtmlString.Create(tagBuilder.ToString(TagRenderMode.Normal));
		}

		public static MvcHtmlString CheckBoxGroup(this HtmlHelper htmlHelper, string name,
		   IEnumerable options, bool useIndexAsValue = true, IEnumerable<object> selectedItems = null, object htmlAttributes = null)
		{
			List<SelectListItem> list = new List<SelectListItem>();
			if (options != null)
			{
				int index = 0;
				foreach (var obj in options)
				{
					string text;
					if (obj.GetType().IsEnum)
					{
						text = ((Enum)obj).ToDescription();
					}
					else
					{
						text = obj.ToString();
						if (text.StartsWith("[$empty_first$]"))
						{
							text = text.Replace("[$empty_first$]", "");
						}
					}
					string value;
					if (useIndexAsValue)
					{
						value = index.ToString();
					}
					else
					{
						if (obj.GetType().IsEnum)
						{
							value = ((int)obj).ToString();
						}
						else
						{
							value = obj.ToString();
							if (value.StartsWith("[$empty_first$]"))
							{
								value = string.Empty;
							}
						}
					}
					list.Add(new SelectListItem()
					{
						Text = text,
						Value = value,
						Selected = selectedItems != null && selectedItems.Count() > 0 && (selectedItems.Contains(obj)
                            || (selectedItems.First().GetType() == typeof(int) && selectedItems.Select(c => c.ToString()).Contains(index.ToString()))
							|| (obj.GetType().IsEnum && selectedItems.First().GetType().IsEnum && selectedItems.Select(c => (int)c).Contains((int)obj)))
					});
					index++;
				}
			}
			return CheckBoxGroup(htmlHelper, name, list, htmlAttributes);
		}

		public static MvcHtmlString EnumCheckBox<T>(this HtmlHelper htmlHelper, string name, bool userIndexAsValue = false, IEnumerable<T> selectedItems = null,
            object htmlAttributes = null, IEnumerable<T> enumOptions = null)
		   where T : struct
		{
			if (typeof(T).IsEnum == false)
			{
				return MvcHtmlString.Empty;
			}
			//var options = Enum.GetValues(typeof(T));

            var valueList = Enum.GetValues(typeof(T));
            ArrayList array = new ArrayList(valueList.Length);
            if (enumOptions != null && enumOptions.Count() > 0)
            {
                var l = enumOptions.Distinct().ToList();
                for (int j = 0; j < valueList.Length; j++)
                {
                    var obj = valueList.GetValue(j);
                    if (l.Contains((T)obj))
                    {
                        array.Add(obj);
                    }
                }
            }
            else
            {
                for (int j = 0; j < valueList.Length; j++)
                {
                    array.Add(valueList.GetValue(j));
                }
            }

			Object[] selectValues = null;
			if(selectedItems!=null)
			{ 
				selectValues = new object[selectedItems.Count()];
				selectedItems.ToArray().CopyTo(selectValues, 0);
			}

            return CheckBoxGroup(htmlHelper, name, array, userIndexAsValue, selectValues, htmlAttributes);
		}
		#endregion

        #region Radio
        public static MvcHtmlString RadioGroup(this HtmlHelper htmlHelper, string name,
            IEnumerable options, string TextPropertyName, string ValuePropertyName, object checkedValue = null, object htmlAttributes = null)
        {
            List<SelectListItem> list = new List<SelectListItem>();
            if (options != null)
            {
                foreach (var obj in options)
                {
                    if (obj.GetType().Name.StartsWith("<>f__AnonymousType"))
                    {
                        var collection = TypeDescriptor.GetProperties(obj);
                        var p_text = collection.Find(TextPropertyName, false);
                        if (p_text == null)
                        {
                            throw new ApplicationException("Not found the property '" + TextPropertyName + "'");
                        }
                        var p_value = collection.Find(ValuePropertyName, false);
                        if (p_value == null)
                        {
                            throw new ApplicationException("Not found the property '" + ValuePropertyName + "'");
                        }
                        var s = p_value.GetValue(obj);
                        var t = p_text.GetValue(obj);
                        list.Add(new SelectListItem()
                        {
                            Text = t == null ? null : t.ToString(),
                            Value = s == null ? null : s.ToString(),
                            Selected = checkedValue != null && (checkedValue == obj || checkedValue == s || (checkedValue is string && s is string && checkedValue.ToString() == s.ToString()))
                        });
                    }
                    else
                    {
                        var s = Invoker.PropertyGet(obj, ValuePropertyName);
                        var t = Invoker.PropertyGet(obj, TextPropertyName);
                        //bool tmp_selected = checkedValue != null && (checkedValue == obj || checkedValue == s || (checkedValue is string && s is string && checkedValue.ToString() == s.ToString()));
                        list.Add(new SelectListItem()
                        {
                            Text = t == null ? null : t.ToString(),
                            Value = s == null ? null : s.ToString(),
                            Selected = checkedValue != null && (checkedValue == obj || checkedValue == s || (checkedValue is string && s is string && checkedValue.ToString() == s.ToString()))
                        });
                    }
                }
            }
            return RadioGroup(htmlHelper, name, list, htmlAttributes);
        }

        public static MvcHtmlString RadioGroup(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> options = null, object htmlAttributes = null)
        {
            StringBuilder itemsHtml = new StringBuilder();
            if (options != null)
            {
                if (options.Count() > 0 && options.FirstOrDefault(x => x.Selected) == null)
                {
                    options.First().Selected = true;
                }
                foreach (var item in options)
                {
                    TagBuilder checkboxBuilder = new TagBuilder("input");

                    checkboxBuilder.Attributes["type"] = "radio";
                    checkboxBuilder.Attributes["name"] = name;

                    if (item.Value != null)
                    {
                        checkboxBuilder.Attributes["value"] = item.Value;
                    }
                    if (item.Selected)
                    {
                        checkboxBuilder.Attributes["checked"] = "checked";
                    }

                    TagBuilder builder = new TagBuilder("label")
                    {
                        InnerHtml = checkboxBuilder.ToString(TagRenderMode.Normal) + item.Text
                    };

                    itemsHtml.Append(builder.ToString(TagRenderMode.Normal));
                }
            }
            var dic = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            string fullName = htmlHelper == null ? name : htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
            TagBuilder tagBuilder = new TagBuilder("div");
            tagBuilder.MergeAttributes<string, object>(dic);
            tagBuilder.Attributes["class"] = "radio";
            tagBuilder.InnerHtml = itemsHtml.ToString();
            return MvcHtmlString.Create(tagBuilder.ToString(TagRenderMode.Normal));
        }

        public static MvcHtmlString RadioGroup(this HtmlHelper htmlHelper, string name,
           IEnumerable options, bool useIndexAsValue = true, object checkedValue = null, object htmlAttributes = null)
        {
            List<SelectListItem> list = new List<SelectListItem>();
            if (options != null)
            {
                int index = 0;
                foreach (var obj in options)
                {
                    string text;
                    if (obj.GetType().IsEnum)
                    {
                        text = ((Enum)obj).ToDescription();
                    }
                    else
                    {
                        text = obj.ToString();
                        if (text.StartsWith("[$empty_first$]"))
                        {
                            text = text.Replace("[$empty_first$]", "");
                        }
                    }
                    string value;
                    if (useIndexAsValue)
                    {
                        value = index.ToString();
                    }
                    else
                    {
                        if (obj.GetType().IsEnum)
                        {
                            value = ((int)obj).ToString();
                        }
                        else
                        {
                            value = obj.ToString();
                            if (value.StartsWith("[$empty_first$]"))
                            {
                                value = string.Empty;
                            }
                        }
                    }
                    list.Add(new SelectListItem()
                    {
                        Text = text,
                        Value = value,
                        Selected = checkedValue != null && (checkedValue == obj || (checkedValue.GetType() == typeof(int) && (int)checkedValue == index)
                            || (obj.GetType().IsEnum && checkedValue.GetType().IsEnum && (int)obj == (int)checkedValue))
                    });
                    index++;
                }
            }
            return RadioGroup(htmlHelper, name, list, htmlAttributes);
        }

        public static MvcHtmlString EnumRadio<T>(this HtmlHelper htmlHelper, string name, bool userIndexAsValue = false, T? checkedValue = null,
            object htmlAttributes = null, IEnumerable<T> enumOptions = null)
           where T : struct
        {
            if (typeof(T).IsEnum == false)
            {
                return MvcHtmlString.Empty;
            }
            //var options = Enum.GetValues(typeof(T));

            var valueList = Enum.GetValues(typeof(T));
            List<object> array = new List<object>(valueList.Length);
            if (enumOptions != null && enumOptions.Count() > 0)
            {
                var l = enumOptions.Distinct().ToList();
                for (int j = 0; j < valueList.Length; j++)
                {
                    var obj = valueList.GetValue(j);
                    if (l.Contains((T)obj))
                    {
                        array.Add(obj);
                    }
                }
            }
            else
            {
                for (int j = 0; j < valueList.Length; j++)
                {
                    array.Add(valueList.GetValue(j));
                }
            }

            return RadioGroup(htmlHelper, name, array, userIndexAsValue, checkedValue, htmlAttributes);
        }
        #endregion

		#region Selector

		public static MvcHtmlString Selector(this HtmlHelper htmlHelper, string name,
			IEnumerable<SelectListItem> options = null, object htmlAttributes = null)
		{
			StringBuilder itemsHtml = new StringBuilder();
			if (options != null)
			{
				foreach (var item in options)
				{
					TagBuilder builder = new TagBuilder("option")
					{
						InnerHtml = HttpUtility.HtmlEncode(item.Text)
					};
					if (item.Value != null)
					{
						builder.Attributes["value"] = item.Value;
					}
					if (item.Selected)
					{
						builder.Attributes["selected"] = "selected";
					}
					itemsHtml.Append(builder.ToString(TagRenderMode.Normal));
				}
			}
			var dic = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
			string fullName = htmlHelper == null ? name : htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
			TagBuilder tagBuilder = new TagBuilder("select");
			tagBuilder.MergeAttributes<string, object>(dic);
			tagBuilder.MergeAttribute("name", fullName, true);
			tagBuilder.MergeAttribute("id", fullName, true);
			tagBuilder.MergeAttribute("class", "selectpicker " + name);
			tagBuilder.InnerHtml = itemsHtml.ToString();
			return MvcHtmlString.Create(tagBuilder.ToString(TagRenderMode.Normal));
		}

        public static MvcHtmlString Selector(this HtmlHelper htmlHelper, string name,
            IEnumerable options, string TextPropertyName, string ValuePropertyName, object selectedValue = null, object htmlAttributes = null)
        {
			return SelectorWithEmpty(htmlHelper, name, options, TextPropertyName, ValuePropertyName, null, selectedValue, htmlAttributes);
        }

		public static MvcHtmlString SelectorWithEmpty(this HtmlHelper htmlHelper, string name,
			IEnumerable options, string TextPropertyName, string ValuePropertyName, string firstEmptyOptionText = null, object selectedValue = null, object htmlAttributes = null)
		{
			List<SelectListItem> list = new List<SelectListItem>();

			if (!string.IsNullOrWhiteSpace(firstEmptyOptionText))
			{
				list.Add(new SelectListItem()
				{
					Text = firstEmptyOptionText,
					Value = "",
					Selected = selectedValue == null || selectedValue.ToString() == ""
				});
			}

			if (options != null)
			{
				foreach (var obj in options)
				{
					if (obj.GetType().Name.StartsWith("<>f__AnonymousType"))
					{
						var collection = TypeDescriptor.GetProperties(obj);
						var p_text = collection.Find(TextPropertyName, false);
						if (p_text == null)
						{
							throw new ApplicationException("Not found the property '" + TextPropertyName + "'");
						}
						var p_value = collection.Find(ValuePropertyName, false);
						if (p_value == null)
						{
							throw new ApplicationException("Not found the property '" + ValuePropertyName + "'");
						}
						var s = p_value.GetValue(obj);
						var t = p_text.GetValue(obj);
						list.Add(new SelectListItem()
						{
							Text = t == null ? null : t.ToString(),
							Value = s == null ? null : s.ToString(),
							Selected = selectedValue != null && (selectedValue == obj || selectedValue == s || (selectedValue is string && s is string && selectedValue.ToString() == s.ToString()))
						});
					}
					else
					{
						var s = Invoker.PropertyGet(obj, ValuePropertyName);
						var t = Invoker.PropertyGet(obj, TextPropertyName);
						bool tmp_selected = s is string && selectedValue is string && s.ToString() == selectedValue.ToString();
						list.Add(new SelectListItem()
						{
							Text = t == null ? null : t.ToString(),
							Value = s == null ? null : s.ToString(),
							Selected = selectedValue != null && (selectedValue == obj || tmp_selected || object.Equals(selectedValue, s))
						});
					}
				}
			}
			return Selector(htmlHelper, name, list, htmlAttributes);
		}
		
        public static MvcHtmlString Selector(this HtmlHelper htmlHelper, string name,
            IEnumerable options, bool useIndexAsValue = true, object selected = null, object htmlAttributes = null)
        {
            List<SelectListItem> list = new List<SelectListItem>();
            if (options != null)
            {
                int index = 0;
                foreach (var obj in options)
                {
                    string text;
                    if (obj.GetType().IsEnum)
                    {
                        text = ((Enum)obj).ToDescription();
                    }
                    else
                    {
                        text = obj.ToString();
                        if (text.StartsWith("[$empty_first$]"))
                        {
                            text = text.Replace("[$empty_first$]", "");
                        }
                    }
                    string value;
                    if (useIndexAsValue)
                    {
                        value = index.ToString();
                    }
                    else
                    {
                        if (obj.GetType().IsEnum)
                        {
                            value = ((int)obj).ToString();
                        }
                        else
                        {
                            value = obj.ToString();
                            if (value.StartsWith("[$empty_first$]"))
                            {
                                value = string.Empty;
                            }
                        }
                    }
                    list.Add(new SelectListItem()
                    {
                        Text = text,
                        Value = value,
                        Selected = selected != null && (selected == obj || (selected.GetType() == typeof(int) && (int)selected == index)
                            || (obj.GetType().IsEnum && selected.GetType().IsEnum && (int)obj == (int)selected))
                    });
                    index++;
                }
            }
            return Selector(htmlHelper, name, list, htmlAttributes);
        }

		public static MvcHtmlString NumberSelector(this HtmlHelper htmlHelper, string name,
			int fromNumber, int toNumber, int? selectedNumber = null, int step = 1, object htmlAttributes = null)
		{
			List<SelectListItem> list;
			step = step == 0 ? 1 : (step < 0 ? step * -1 : step);
			Func<int, int, bool> func;
			if (toNumber < fromNumber)
			{
				step = -1 * step;
				func = (t1, t2) => t1 >= t2;
				list = new List<SelectListItem>(fromNumber - toNumber);
			}
			else
			{
				func = (t1, t2) => t1 <= t2;
				list = new List<SelectListItem>(toNumber - fromNumber);
			}
			bool hasToNumber = false;
			while (func(fromNumber, toNumber))
			{
				if (fromNumber == toNumber)
				{
					hasToNumber = true;
				}
				list.Add(new SelectListItem()
				{
					Text = fromNumber.ToString(),
					Value = fromNumber.ToString(),
					Selected = selectedNumber.HasValue && selectedNumber.Value == fromNumber
				});
				fromNumber = fromNumber + step;
			}
			if (hasToNumber == false)
			{
				list.Add(new SelectListItem()
				{
					Text = toNumber.ToString(),
					Value = toNumber.ToString(),
					Selected = selectedNumber.HasValue && selectedNumber.Value == toNumber
				});
			}
			return Selector(htmlHelper, name, list, htmlAttributes);
		}

		public static MvcHtmlString DayOfWeekSelector(this HtmlHelper htmlHelper, string name, DayOfWeek? selected = null, object htmlAttributes = null)
		{
			var array = Enum.GetValues(typeof(DayOfWeek));
			return Selector(htmlHelper, name, array, selected: (selected.HasValue ? (object)selected.Value : null), useIndexAsValue: true, htmlAttributes: htmlAttributes);
		}

        public static MvcHtmlString EnumSelector<T>(this HtmlHelper htmlHelper, string name, T? selected = null, object htmlAttributes = null, IEnumerable<T> enumOptions = null)
			where T : struct
		{
			//if (typeof(T).IsEnum == false)
			//{
			//    return MvcHtmlString.Empty;
			//}
			//var array = Enum.GetValues(typeof(T));
			//return Selector(htmlHelper, name, array, selected: (selected.HasValue ? (object)selected.Value : null), useIndexAsValue: false, htmlAttributes: htmlAttributes);
            return EnumSelector<T>(htmlHelper, name, null, selected, htmlAttributes, enumOptions);
		}

        public static MvcHtmlString EnumSelector<T>(this HtmlHelper htmlHelper, string name, string firstEmptyOptionText, T? selected = null, object htmlAttributes = null, IEnumerable<T> enumOptions = null)
			where T : struct
		{
			if (typeof(T).IsEnum == false)
			{
				return MvcHtmlString.Empty;
			}
            var valueList = Enum.GetValues(typeof(T));
            List<object> array = new List<object>(valueList.Length);
            if (enumOptions != null && enumOptions.Count() > 0)
            {
                var l = enumOptions.Distinct().ToList();
                for (int j = 0; j < valueList.Length; j++)
                {
                    var obj = valueList.GetValue(j);
                    if (l.Contains((T)obj))
                    {
                        array.Add(obj);
                    }
                }
            }
            else
            {
                for (int j = 0; j < valueList.Length; j++)
                {
                    array.Add(valueList.GetValue(j));
                }
            }

			IEnumerable options;
			if (firstEmptyOptionText != null)
			{
				List<object> t = new List<object>(array.Count);
				t.Add("[$empty_first$]" + firstEmptyOptionText);
				foreach (var item in array)
				{
					t.Add(item);
				}
				options = t;
			}
			else
			{
				options = array;
			}
			return Selector(htmlHelper, name, options, selected: (selected.HasValue ? (object)selected.Value : null), useIndexAsValue: false, htmlAttributes: htmlAttributes);
		}

        public static MvcHtmlString EnumSelector(this HtmlHelper htmlHelper, string name, Type enumType, object selected = null, object htmlAttributes = null, IEnumerable<object> enumOptions = null)
		{
			//if (enumType.IsEnum == false)
			//{
			//    return MvcHtmlString.Empty;
			//}
			//var array = Enum.GetValues(enumType);
			//if (selected != null && enumType.IsAssignableFrom(selected.GetType()) == false)
			//{
			//    throw new ArgumentException("Can't cast type '" + selected.GetType().FullName + "' to type '" + enumType.FullName + "'.", "selected");
			//}
			//var value = (int)selected;
			//int i = 0;
			//for (int j = 0; j < array.Length; j++)
			//{
			//    if ((int)array.GetValue(j) == value)
			//    {
			//        i = j;
			//        break;
			//    }
			//}
			//return Selector(htmlHelper, name, array, true, i, htmlAttributes: htmlAttributes);
            return EnumSelector(htmlHelper, name, null, enumType, selected, htmlAttributes, enumOptions);
		}

        public static MvcHtmlString EnumSelector(this HtmlHelper htmlHelper, string name, string firstEmptyOptionText, Type enumType, object selected = null, object htmlAttributes = null, IEnumerable<object> enumOptions = null)
		{
			if (enumType.IsEnum == false)
			{
				return MvcHtmlString.Empty;
			}
			if (selected != null && enumType.IsAssignableFrom(selected.GetType()) == false)
			{
				throw new ArgumentException("Can't cast type '" + selected.GetType().FullName + "' to type '" + enumType.FullName + "'.", "selected");
            }
            var valueList = Enum.GetValues(enumType);
            List<object> array = new List<object>(valueList.Length);
            if (enumOptions != null && enumOptions.Count() > 0)
            {
                enumOptions = enumOptions.Distinct();
                foreach (var op in enumOptions)
                {
                    if (op != null && enumType.IsAssignableFrom(op.GetType()) == false)
                    {
                        throw new ArgumentException("Can't cast type '" + op.GetType().FullName + "' to type '" + enumType.FullName + "'.", "selected");
                    }
                }
                for (int j = 0; j < valueList.Length; j++)
                {
                    var obj = valueList.GetValue(j);
                    if (enumOptions.FirstOrDefault(x => x != null && (int)x == (int)obj) != null)
                    {
                        array.Add(obj);
                    }
                }
            }
            else
            {
                for (int j = 0; j < valueList.Length; j++)
                {
                    array.Add(valueList.GetValue(j));
                }
            }
			var value = (int)selected;
			int i = 0;
			for (int j = 0; j < array.Count; j++)
			{
				if ((int)array[j] == value)
				{
					i = j;
					break;
				}
			}
			IEnumerable options;
			if (firstEmptyOptionText != null)
			{
				List<object> t = new List<object>(array.Count);
				t.Add("[$empty_first$]" + firstEmptyOptionText);
				foreach (var item in array)
				{
					t.Add(item);
				}
				options = t;
			}
			else
			{
				options = array;
			}
			return Selector(htmlHelper, name, options, false, i, htmlAttributes: htmlAttributes);
		}

		#endregion

		#region TextBox

		public static MvcHtmlString TextInput(this HtmlHelper htmlHelper, string name, object inputHtmlAttributes = null, object spanHtmlAttributes = null, string iconName = null, bool onLeft = true, bool password = false)
		{
			var dic = HtmlHelper.AnonymousObjectToHtmlAttributes(inputHtmlAttributes);
			string fullName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
			TagBuilder tagBuilder = new TagBuilder("input");
			tagBuilder.MergeAttributes<string, object>(dic);
			tagBuilder.MergeAttribute("name", fullName, true);
			tagBuilder.MergeAttribute("id", fullName, true);
			if (password)
			{
				tagBuilder.MergeAttribute("type", "password", true);
			}
			else
			{
				tagBuilder.MergeAttribute("type", "text", true);
			}
			var c = "form-control";
			if (dic.ContainsKey("class"))
			{
				c += " " + dic["class"];
			}
			tagBuilder.MergeAttribute("class", c, true);
			if (string.IsNullOrWhiteSpace(iconName))
			{
				return MvcHtmlString.Create(tagBuilder.ToString(TagRenderMode.SelfClosing));
			}
			TagBuilder spanTagBuilder = new TagBuilder("span");
			var dic1 = HtmlHelper.AnonymousObjectToHtmlAttributes(spanHtmlAttributes);
			spanTagBuilder.MergeAttributes<string, object>(dic1);
			var c1 = "input-icon input-icon-" + (onLeft ? "left" : "right");
			if (dic1.ContainsKey("class"))
			{
				c1 += " " + dic1["class"];
			}
			spanTagBuilder.MergeAttribute("class", c1, true);
			spanTagBuilder.InnerHtml = tagBuilder.ToString(TagRenderMode.SelfClosing) + "<i class=\"fa " + iconName.Trim() + "\"></i>";
			//return MvcHtmlString.Create("<span class=\"input-icon input-icon-" + (onLeft ? "left" : "right") + "\">" + tagBuilder.ToString(TagRenderMode.SelfClosing) + "<i class=\"fa " + iconName.Trim() + "\"></i></span>");
			return MvcHtmlString.Create(spanTagBuilder.ToString());
		}

		#endregion

		public static MvcHtmlString DateTimeRangePicker(this HtmlHelper htmlHelper, string name,
			DateTime? startDate = null, DateTime? endDate = null, DateTime? minDate = null, DateTime? maxDate = null,
			bool allowEmpty = true, bool withTimePart = true, object htmlAttributes = null)
		{
			if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
				throw new ArgumentException("开始时间'startDate'不能晚于结束时间'endDate'");

			if (minDate.HasValue && maxDate.HasValue && minDate.Value > maxDate.Value)
				throw new ArgumentException("最小时间'minDate'不能大于最大时间'maxDate'");

			string fromId = string.Format("{0}_from", name);
			string toId = string.Format("{0}_to", name);

			if (!allowEmpty)
			{
				if (!startDate.HasValue)
					startDate = DateTime.Now;
				if (!endDate.HasValue)
					endDate = DateTime.Now.AddDays(1);
			}

			Dictionary<string, dynamic> option = new Dictionary<string, dynamic>();

			string vformat = "yyyy-MM-dd";
			string format = "YYYY-MM-DD";
			if (withTimePart)
			{
				vformat += " HH:mm:ss";
				format += " HH:mm:ss";
				option.Add("timePicker", true);
				option.Add("timePickerIncrement", 1);
				option.Add("timePicker24Hour", true);
				option.Add("timePickerSeconds", true);
			}
			option.Add("format", format);

			string val = "";
			if (startDate.HasValue || endDate.HasValue)
			{
				DateTime start1 = startDate.HasValue ? startDate.Value : DateTime.MinValue;
				if (minDate.HasValue && minDate.Value > start1)
					start1 = minDate.Value;

				DateTime end1 = endDate.HasValue ? endDate.Value : DateTime.MaxValue;
				if (maxDate.HasValue && maxDate.Value < end1)
					end1 = maxDate.Value;

				if (start1 > end1)
					end1 = start1;

				option.Add("startDate", start1.ToString(vformat));
				option.Add("endDate", end1.ToString(vformat));
				val = string.Format("{0} 至 {1}", start1.ToString(vformat), end1.ToString(vformat));
			}

			if (minDate.HasValue)
				option.Add("minDate", minDate.Value.ToString(vformat));
			if (maxDate.HasValue)
				option.Add("maxDate", maxDate.Value.ToString(vformat));

			List<List<string>> ranges = new List<List<string>>();
			DateTime now = DateTime.Now;
			Tuple<string, DateTime, DateTime>[] array = new Tuple<string, DateTime, DateTime>[]
            {
                new Tuple<string, DateTime, DateTime>(("今天"), now.Date, now.AddDays(1).AddMilliseconds(-1).Date),
                new Tuple<string, DateTime, DateTime>(("昨天"), now.AddDays(-1).Date, now.AddMilliseconds(-1).Date),
                new Tuple<string, DateTime, DateTime>(("最近7天"), now.AddDays(-6).Date, now.AddDays(1).AddMilliseconds(-1).Date),
                new Tuple<string, DateTime, DateTime>(("最近30天"), now.AddDays(-29).Date, now.AddDays(1).AddMilliseconds(-1).Date),
                new Tuple<string, DateTime, DateTime>(("本月"), new DateTime(now.Year, now.Month, 1).Date, new DateTime(now.Year, now.Month, 1).AddMonths(1).AddMilliseconds(-1).Date),
                new Tuple<string, DateTime, DateTime>(("上月"), new DateTime(now.Year, now.Month, 1).AddMonths(-1).Date, new DateTime(now.Year, now.Month, 1).AddMilliseconds(-1).Date),
            };
			foreach (var item in array)
			{
				List<string> list = new List<string> { item.Item1, item.Item2.ToString(vformat), item.Item3.ToString(vformat) };
				ranges.Add(list);
			}
			string rangesStr = new JavaScriptSerializer().Serialize(ranges);

			Dictionary<string, dynamic> locale = new Dictionary<string, dynamic>();
			locale.Add("applyLabel", "确定");
			locale.Add("cancelLabel", allowEmpty ? "清除" : "取消");
			locale.Add("fromLabel", "从");
			locale.Add("toLabel", "至");
			locale.Add("customRangeLabel", "自定义");
			locale.Add("daysOfWeek", new List<string> { "日", "一", "二", "三", "四", "五", "六" });
			locale.Add("monthNames", new List<string> { "一月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月", "十一月", "十二月" });

			option.Add("locale", locale);
			option.Add("showDropdowns", true);
			option.Add("showWeekNumbers", false);
			option.Add("opens", "left");

			string optionStr = new JavaScriptSerializer().Serialize(option);

			var dic = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
			TagBuilder tagBuilder = new TagBuilder("div");
			tagBuilder.MergeAttributes<string, object>(dic);
			var cls = "input-group date-range-picker";
			if (dic.ContainsKey("class"))
				cls += " " + dic["class"];
			tagBuilder.MergeAttribute("class", cls, true);
			if (allowEmpty)
				tagBuilder.MergeAttribute("data-allowempty", "1", true);

			tagBuilder.MergeAttribute("data-format", format, true);
			tagBuilder.MergeAttribute("data-option", optionStr, true);
			tagBuilder.MergeAttribute("data-ranges", rangesStr, true);
			tagBuilder.MergeAttribute("data-fromid", fromId, true);
			tagBuilder.MergeAttribute("data-toid", toId, true);
			tagBuilder.MergeAttribute("data-errormsg", "结束时间必须晚于开始时间", true);

			string inner = string.Format("<span class=\"value-label form-control\">{0}</span>", val);
			inner += string.Format("<input type='hidden' name='{0}' id='{0}' value='{1}' />", fromId, (startDate.HasValue ? startDate.Value.ToString(vformat) : ""));
			inner += string.Format("<input type='hidden' name='{0}' id='{0}' value='{1}' />", toId, (endDate.HasValue ? endDate.Value.ToString(vformat) : ""));
			inner += string.Format("<span class=\"input-group-addon\"><i class=\"fa fa-calendar\"></i></span>");
			tagBuilder.InnerHtml = inner;

			return MvcHtmlString.Create(tagBuilder.ToString(TagRenderMode.Normal));
		}

		public static MvcHtmlString FormLabel(this HtmlHelper htmlHelper, string text, int width = 2, string forId = null, string id = null, object htmlAttributes = null)
		{
			var dic = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
			TagBuilder tagBuilder = new TagBuilder("label");
			tagBuilder.MergeAttributes<string, object>(dic);
			if (id != null)
			{
				tagBuilder.MergeAttribute("id", id, true);
			}
			if (forId != null)
			{
				tagBuilder.MergeAttribute("for", forId, true);
			}
			string h = "";
			if (dic.ContainsKey("title") || dic.ContainsKey("data-title"))
			{
				tagBuilder.MergeAttribute("data-toggle", "tooltip", true);
				h += " <i class=\"fa fa-question-circle\" style=\"color:#3276b1;\"></i>";
			}
			var c = "col-sm-" + width + " control-label" + (h.Length > 0 ? " label-tips" : "");
			if (dic.ContainsKey("class"))
			{
				c += " " + dic["class"];
			}
			tagBuilder.MergeAttribute("class", c, true);
			tagBuilder.InnerHtml = text + h;
			return MvcHtmlString.Create(tagBuilder.ToString(TagRenderMode.Normal));
		}

		public static MvcHtmlString FormRowWithTextInput(this HtmlHelper htmlHelper, string name,
			string labelText, string defaultValue, int labelWidth = 2,
			string tips = null, string placeholder = null, string iconName = null, bool onLeft = true)
		{
			object inputHtmlAttributes = new { placeholder = placeholder, value = defaultValue };
			object labelHtmlAttributes = null;
			if (string.IsNullOrWhiteSpace(tips) == false)
			{
				labelHtmlAttributes = new { title = tips, data_placement = "right" };
			}
			return FormRowWithTextInput(htmlHelper, name, labelText, labelWidth,
				inputHtmlAttributes, labelHtmlAttributes, iconName, onLeft);
		}

		public static MvcHtmlString FormRowWithTextInput(this HtmlHelper htmlHelper, string name,
			string labelText, int labelWidth = 2, object inputHtmlAttributes = null, object labelHtmlAttributes = null,
			string iconName = null, bool onLeft = true)
		{
			StringBuilder html = new StringBuilder();
			html.AppendLine("<div class=\"form-group\">");
			html.AppendLine(htmlHelper.FormLabel(labelText, labelWidth, name, name + "_label", labelHtmlAttributes).ToString());
			html.AppendLine("<div class=\"col-sm-" + (12 - labelWidth) + "\">");
			html.AppendLine(htmlHelper.TextInput(name, inputHtmlAttributes, null, iconName, onLeft).ToString());
			html.AppendLine("</div>");
			html.AppendLine("</div>");
			return MvcHtmlString.Create(html.ToString());
		}

		public static MvcHtmlString FormRow(this HtmlHelper htmlHelper, Func<MvcHtmlString> controlFunc,
			string labelText, int labelWidth = 2, string tips = null)
		{
			object labelHtmlAttributes = null;
			if (string.IsNullOrWhiteSpace(tips) == false)
			{
				labelHtmlAttributes = new { title = tips, data_placement = "right" };
			}
			StringBuilder html = new StringBuilder();
			html.AppendLine("<div class=\"form-group\">");
			html.AppendLine(htmlHelper.FormLabel(labelText, labelWidth, null, null, labelHtmlAttributes).ToString());
			html.AppendLine("<div class=\"col-sm-" + (12 - labelWidth) + "\">");
			if (controlFunc != null)
			{
				html.AppendLine(controlFunc().ToString());
			}
			html.AppendLine("</div>");
			html.AppendLine("</div>");
			return MvcHtmlString.Create(html.ToString());
		}

		/// <summary>
		/// helper function to output pagination html markup
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="totalRecord">number of records</param>
		/// <param name="pageSize">page size (optional)</param>
		/// <returns></returns>
		public static MvcHtmlString Pagination(this HtmlHelper htmlHelper, int totalRecord, int pageSize = 20)
		{
			// querystring key for page index
			string pageKey = "page";

			// get request url
			var request = HttpContext.Current.Request;
			string url = request.Url.LocalPath + "?";
			foreach (string param in request.QueryString.Keys)
			{
				if (string.Equals(param, pageKey, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				if (!string.IsNullOrEmpty(request.QueryString[param]))
				{
					url += string.Format("{0}={1}&", param, HttpUtility.UrlEncode(request.QueryString[param]));
				}
			}

			// get current page index from request url
			int currentPage = 1;
			string pageString = request.QueryString[pageKey];
			if (!string.IsNullOrEmpty(pageString))
			{
				int.TryParse(pageString, out currentPage);
			}

			// get total page
			int totalPage = (totalRecord % pageSize) == 0 ? (totalRecord / pageSize) : (totalRecord / pageSize) + 1;

			if (currentPage < 1) { currentPage = 1; }
			if (currentPage > totalPage) { currentPage = totalPage; }

            //if (totalPage <= 1)
            //{
            //    return MvcHtmlString.Empty;
            //}
            //else // 哪怕只有一页，也应该显示出分页页码信息，以及记录总数
			StringBuilder paging = new StringBuilder();
			paging.AppendLine("<div class=\"pagination-wrapper\">");
            paging.AppendLine("<span class=\"total-number\">当前页" + (totalRecord < pageSize ? totalRecord : pageSize) + "条 / 总共" + totalRecord + "条 / 总共" + totalPage + "页</span><ul class=\"pagination\">");

			if (currentPage > 1)
			{
				string prev = string.Format("{0}page={1}", url, currentPage - 1);
				paging.AppendLine(string.Format("<li><a href='{0}'>上一页</a></li>", prev));
			}
			else
			{
				paging.AppendLine("<li class='disabled'><span>上一页</span></li>");
			}

			int range = 2;
			int rangeStart = (currentPage - range > 0) ? currentPage - range : 1;
			int rangeEnd = (currentPage + range <= totalPage) ? currentPage + range : totalPage;

			if (rangeStart > 1)
			{
				string first = string.Format("{0}page={1}", url, 1);
				paging.AppendLine(string.Format("<li><a href='{0}'>{1}</a></li>", first, 1));
			}

			if (rangeStart > 2)
			{
				paging.AppendLine("<li class='disabled'><span>...</span></li>");
			}

			for (int index = rangeStart; index <= rangeEnd; index++)
			{
				if (currentPage == index)
				{
					paging.AppendLine(string.Format("<li class='active'><span>{0}</span></li>", index));
				}
				else
				{
					string curr = string.Format("{0}page={1}", url, index);
					paging.AppendLine(string.Format("<li><a href='{0}'>{1}</a></li>", curr, index));
				}
			}

			if (rangeEnd < (totalPage - 1))
			{
				paging.AppendLine("<li class='disabled'><span>...</span></li>");
			}

			if (rangeEnd < totalPage)
			{
				string last = string.Format("{0}page={1}", url, totalPage);
				paging.AppendLine(string.Format("<li><a href='{0}'>{1}</a></li>", last, totalPage));
			}

			if (currentPage < totalPage)
			{
				string next = string.Format("{0}page={1}", url, currentPage + 1);
				paging.AppendLine(string.Format("<li><a href='{0}'>下一页</a></li>", next));
			}
			else
			{
				paging.AppendLine("<li class='disabled'><span>下一页</span></li>");
			}
			paging.AppendLine("</ul><div class=\"clearfix\"></div></div><div class=\"clearfix\"></div>");

			return MvcHtmlString.Create(paging.ToString());
		}

        public static MvcHtmlString PaginationByPost(this HtmlHelper htmlHelper, int totalRecord, int pageSize = 20)
        {
            var request = HttpContext.Current.Request;
            string id = Guid.NewGuid().ToString("N");
            StringBuilder html = new StringBuilder();
            html.AppendFormat("<form style=\"display:none;\" action=\"{0}\" method=\"post\" id=\"{1}\">", request.Url.LocalPath, id);
            // querystring key for page index
            string pageKey = "page";
            // get request url
            foreach (string param in request.Form.Keys)
            {
                if (string.Equals(param, pageKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(request.Form[param]))
                {
                    html.AppendFormat("<input type=\"hidden\" name=\"{0}\" value=\"{1}\" />", param, request.Form[param]);
                }
            }
            html.AppendFormat("<input type=\"hidden\" name=\"page\" value=\"\" id=\"page{0}\" />", id);
            html.Append("</form>");
            html.AppendFormat(@"
<script>
function gotoPage{0}(p){{
    $('#page{0}').val(p);
    $('#{0}').submit();
}}
</script>", id);

            // get current page index from request url
            int currentPage = 1;
            string pageString = request.Form[pageKey];
            if (!string.IsNullOrEmpty(pageString))
            {
                int.TryParse(pageString, out currentPage);
            }

            // get total page
            int totalPage = (totalRecord % pageSize) == 0 ? (totalRecord / pageSize) : (totalRecord / pageSize) + 1;

            if (currentPage < 1) { currentPage = 1; }
            if (currentPage > totalPage) { currentPage = totalPage; }

            //if (totalPage <= 1)
            //{
            //    return MvcHtmlString.Empty;
            //}
            //else // 哪怕只有一页，也应该显示出分页页码信息，以及记录总数
            StringBuilder paging = new StringBuilder();
            paging.AppendLine("<div class=\"pagination-wrapper\">");
            paging.AppendLine("<span class=\"total-number\">当前页" + (totalRecord < pageSize ? totalRecord : pageSize) + "条 / 总共" + totalRecord + "条 / 总共" + totalPage + "页</span><ul class=\"pagination\">");

            if (currentPage > 1)
            {
                paging.AppendLine(string.Format("<li><a href=\"javascript:gotoPage{0}('{1}')\">上一页</a></li>", id, currentPage - 1));
            }
            else
            {
                paging.AppendLine("<li class='disabled'><span>上一页</span></li>");
            }

            int range = 2;
            int rangeStart = (currentPage - range > 0) ? currentPage - range : 1;
            int rangeEnd = (currentPage + range <= totalPage) ? currentPage + range : totalPage;

            if (rangeStart > 1)
            {
                paging.AppendLine(string.Format("<li><a href=\"javascript:gotoPage{0}('{1}')\">{1}</a></li>", id, 1));
            }

            if (rangeStart > 2)
            {
                paging.AppendLine("<li class='disabled'><span>...</span></li>");
            }

            for (int index = rangeStart; index <= rangeEnd; index++)
            {
                if (currentPage == index)
                {
                    paging.AppendLine(string.Format("<li class='active'><span>{0}</span></li>", index));
                }
                else
                {
                    paging.AppendLine(string.Format("<li><a href=\"javascript:gotoPage{0}('{1}')\">{1}</a></li>", id, index));
                }
            }

            if (rangeEnd < (totalPage - 1))
            {
                paging.AppendLine("<li class='disabled'><span>...</span></li>");
            }

            if (rangeEnd < totalPage)
            {
                paging.AppendLine(string.Format("<li><a href=\"javascript:gotoPage{0}('{1}')\">{1}</a></li>", id, totalPage));
            }

            if (currentPage < totalPage)
            {
                paging.AppendLine(string.Format("<li><a href=\"javascript:gotoPage{0}('{1}')\">下一页</a></li>", id, currentPage + 1));
            }
            else
            {
                paging.AppendLine("<li class='disabled'><span>下一页</span></li>");
            }
            paging.AppendLine("</ul><div class=\"clearfix\"></div></div><div class=\"clearfix\"></div>");
            html.Append(paging);

            return MvcHtmlString.Create(html.ToString());
        }

		public static MvcHtmlString Th(this HtmlHelper htmlHelper, string title, string cls = null, int colspan = 1, int rowspan = 1)
		{
			if (colspan < 1) colspan = 1;
			if (rowspan < 1) rowspan = 1;

			string th = string.Format("<th colspan='{0}' rowspan='{1}'  class='{2}'>{3}</th>", colspan, rowspan, cls, title);
			return MvcHtmlString.Create(th);
		}

		public static MvcHtmlString GridHeadCheck(this HtmlHelper htmlHelper, string cls = null, int rowspan = 1)
		{
			if (rowspan < 1) rowspan = 1;
			if (string.IsNullOrWhiteSpace(cls)) cls = "c1";

			string th = string.Format("<th rowspan='{0}' class='{1}'><input type='checkbox' class='grid-head-check' /></th>", rowspan, cls);
			return MvcHtmlString.Create(th);
		}

		public static MvcHtmlString GridRowCheck(this HtmlHelper htmlHelper, object data, string cls = null, bool disabled = false,
			Dictionary<string, string> htmlAttr = null)
		{
			if (string.IsNullOrWhiteSpace(cls)) cls = "c1";
			string id = data == null ? "" : data.ToString();
			string disabledStr = disabled ? "disabled" : "";

			string htmlAttrStr = "";
			if (htmlAttr != null)
			{
				foreach (var kvp in htmlAttr)
				{
					htmlAttrStr += string.Format(" {0}='{1}'", kvp.Key, kvp.Value);
				}
			}

			string th = string.Format("<td class='{0}'><input type='checkbox' class='grid-row-check' data-id='{1}' {2} {3} /></td>", cls, id, disabledStr, htmlAttrStr);
			return MvcHtmlString.Create(th);
		}

		/// <summary>
		/// output a sortable th tag
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="title">display text in th tag</param>
		/// <param name="sortField">value in querystring. eg. orderby=sortField</param>
		/// <param name="thCls">additional class name for this th</param>
		/// <returns></returns>
		public static MvcHtmlString SortableTh(this HtmlHelper htmlHelper, string title, string sortField = null, string thCls = null)
		{
			if (string.IsNullOrWhiteSpace(sortField))
			{
				sortField = title;
			}
			string orderbyKey = "orderby";
			string orderKey = "order";

			var request = HttpContext.Current.Request;
			string url = request.Url.LocalPath + "?";
			foreach (string param in request.QueryString.Keys)
			{
				if (string.Equals(param, orderbyKey, StringComparison.OrdinalIgnoreCase)
					|| string.Equals(param, orderKey, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				if (!string.IsNullOrEmpty(request.QueryString[param]))
				{
					url += string.Format("{0}={1}&", param, request.QueryString[param]);
				}
			}

			// get current sort order and orderby form request url
			string currSortField = request.QueryString[orderbyKey];
			string currSortOrder = request.QueryString[orderKey];

			if (!string.Equals(currSortOrder, "DESC", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(currSortOrder, "ASC", StringComparison.OrdinalIgnoreCase))
			{
				currSortOrder = "DESC";
			}
			currSortOrder = currSortOrder.ToUpperInvariant();

			StringBuilder th = new StringBuilder();
			if (string.Equals(sortField, currSortField, StringComparison.OrdinalIgnoreCase))
			{
				string cls = currSortOrder == "DESC" ? "sort-desc" : "sort-asc";
				cls += string.Format(" {0}", thCls);

				string fa = currSortOrder == "DESC" ? "fa-sort-desc" : "fa-sort-asc";
				string order = currSortOrder == "DESC" ? "ASC" : "DESC";
				string sortUrl = string.Format("{0}{1}={2}&{3}={4}", url, orderbyKey, sortField, orderKey, order.ToLowerInvariant());
				th.AppendLine(string.Format("<th class='sort {0}'>", cls));
				th.AppendLine(string.Format("<a href='{0}'>", sortUrl));
				th.AppendLine(string.Format("<span>{0}</span>", title));
				th.AppendLine(string.Format("<i class='fa {0}'></i>", fa));
				th.AppendLine("</a></th>");
			}
			else
			{
				string sortUrl = string.Format("{0}{1}={2}&{3}={4}", url, orderbyKey, sortField, orderKey, "desc");
				th.AppendLine(string.Format("<th class='sort {0}'>", thCls));
				th.AppendLine(string.Format("<a href='{0}'>", sortUrl));
				th.AppendLine(string.Format("<span>{0}</span>", title));
				th.AppendLine("<i class='fa fa-sort'></i>");
				th.AppendLine("</a></th>");
			}

			return MvcHtmlString.Create(th.ToString());
		}

		public static MvcHtmlString Required(this HtmlHelper htmlHelper)
		{
			string required = "<i class=\"required\">*</i>";
			return MvcHtmlString.Create(required.ToString());
		}

		public static MvcHtmlString ToolTip(this HtmlHelper htmlHelper, string msg, string direction = "right")
		{
			string tooltip = string.Format("<span class=\"i-tips\" data-toggle=\"tooltip\" data-placement=\"{0}\" title=\"{1}\"></span>", direction, msg);
			return MvcHtmlString.Create(tooltip.ToString());
		}

		public static MvcHtmlString Alert(this HtmlHelper htmlHelper, Dictionary<string, string> message)
		{
			string msg = string.Format("{{'type': '{0}', 'title': '{1}', 'text': '{2}'}}", message["type"], message["title"], message["text"]);
			string js = string.Format("<script>ECAlert({0});</script>", msg);
			return MvcHtmlString.Create(js);
		}

		public static MvcHtmlString RequiredFlag(this HtmlHelper htmlHelper)
		{
			StringBuilder html = new StringBuilder();
			html.AppendLine("<i class='fa fa-asterisk' style='color:#D04437;font-size:4px;position: absolute;right:0;top:10px'></i>");
			return MvcHtmlString.Create(html.ToString());
		}

		public static MvcHtmlString FormLabeValue(this HtmlHelper htmlHelper, string text)
		{
			StringBuilder html = new StringBuilder();
			html.AppendLine(string.Format("<span class='value-label'>{0}</span>", text));
			return MvcHtmlString.Create(html.ToString());
		}

		public static MvcHtmlString CommonFileUploader(this HtmlHelper htmlHelper)
		{
			List<string> segments = new List<string>
			{
				"<div class=\"common-uploader\">",
					"<div class=\"file-list\"></div>",
					"<div class=\"file-picker\">选择文件</div>",
					"<div class=\"clearfix\"></div>",
					"<div class=\"msgs\"></div>",
					"<div class=\"clearfix\"></div>",
				"</div>"
			};
			return MvcHtmlString.Create(string.Join("", segments));
		}

		public static int GetCurrentPageNumber(this HtmlHelper htmlHelper)
		{
			string pageKey = "page";
			int currentPage = 1;
			string pageString = htmlHelper.ViewContext.HttpContext.Request.QueryString[pageKey];
            if (string.IsNullOrWhiteSpace(pageString))
            {
                pageString = htmlHelper.ViewContext.HttpContext.Request.Form[pageKey];
            }
			if (!string.IsNullOrEmpty(pageString))
			{
				int.TryParse(pageString, out currentPage);
			}
			if (currentPage < 1)
			{
				currentPage = 1;
			}
			return currentPage;
		}

		public static string GetSortField(this HtmlHelper htmlHelper, out bool isAsc)
		{
			string orderbyKey = "orderby";
			string orderKey = "order";
			isAsc = string.Equals(htmlHelper.ViewContext.HttpContext.Request.QueryString[orderKey],
				"ASC", StringComparison.OrdinalIgnoreCase);
			return htmlHelper.ViewContext.HttpContext.Request.QueryString[orderbyKey];
		}
	}
}