using System;
using System.Collections.Generic;
using System.Reflection;

namespace TECHCOOL.UI
{
    public class Form<T>
    {
        T record;
        Dictionary<string, Field<T>> fields = new();
        int field_edit_index = 0;
        string current_field
        {
            get
            {
                int i = 0;
                string title = "";
                foreach (KeyValuePair<string, Field<T>> kv in fields)
                {
                    if (i == field_edit_index)
                    {
                        title = kv.Key;
                        break;
                    }
                    i++;
                }
                return title;
            }
        }

        public void AddField(string title, Field<T> field)
        {
            fields.Add(title, field);
        }

        // New TextBox method using property selector
        public Form<T> TextBox(string title, Func<T, object> propertySelector, Action<T, object> propertySetter)
        {
            fields.Add(title, new TextBox<T> { Title = title, PropertySelector = propertySelector, PropertySetter = propertySetter });
            return this;
        }

        public Form<T> IntBox(string title, Func<T, object> propertySelector, Action<T, object> propertySetter)
        {
            fields.Add(title, new IntBox<T> { Title = title, PropertySelector = propertySelector, PropertySetter = propertySetter });
            return this;
        }

        public Form<T> DoubleBox(string title, Func<T, object> propertySelector, Action<T, object> propertySetter)
        {
            fields.Add(title, new DoubleBox<T> { Title = title, PropertySelector = propertySelector, PropertySetter = propertySetter });
            return this;
        }

        public Form<T> SelectBox(string title, Func<T, object> propertySelector, Action<T, object> propertySetter, Dictionary<string, object> options = null)
        {
            if (options == null) options = new();
            fields.Add(title, new SelectBox<T> { Title = title, PropertySelector = propertySelector, PropertySetter = propertySetter, Options = options });
            return this;
        }

        public void AddOption(string field, string option, object value)
        {
            if (!fields.ContainsKey(field))
            {
                throw new Exception("There is no such field in this form called " + field);
            }
            if (!(fields[field] is SelectBox<T>))
            {
                throw new Exception("Field " + field + " is not a SelectBox");
            }
            var f = (SelectBox<T>)fields[field];
            f.Options.Add(option, value);
        }

        public bool Edit(T record)
        {
            this.record = record;
            bool recordChanged = false;

            foreach (KeyValuePair<string, Field<T>> kv in fields)
            {
                var field = kv.Value;
                var value = field.PropertySelector(record);
                if (value != null)
                {
                    field.Value = value;
                }
            }

            int x, y;
            (x, y) = Console.GetCursorPosition();
            ConsoleKeyInfo key;
            do
            {
                Console.SetCursorPosition(x, y);
                field_edit_index = Math.Clamp(field_edit_index, 0, fields.Count - 1);
                Draw();
                key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        fields[current_field].Enter();
                        object property_value = fields[current_field].Value;

                        fields[current_field].PropertySetter(record, property_value);
                        recordChanged = true;
                        break;

                    case ConsoleKey.DownArrow:
                        field_edit_index++;
                        break;

                    case ConsoleKey.UpArrow:
                        field_edit_index--;
                        break;

                    case ConsoleKey.Escape:
                        return recordChanged;
                }

            } while (true);
        }

        protected void Draw()
        {
            int x, y;
            (x, y) = Console.GetCursorPosition();
            int i = 0;
            foreach (KeyValuePair<string, Field<T>> kv in fields)
            {
                Field<T> field = kv.Value;
                field.Focus = (i++ == field_edit_index);
                field.Draw(x, y++);
            }
            Console.WriteLine();
        }

        int getLongestTitleLength()
        {
            int longest = 0;
            foreach (KeyValuePair<string, Field<T>> kv in fields)
            {
                Field<T> field = kv.Value;
                longest = Math.Max(field.Title.Length, longest);
            }
            return longest;
        }
    }


    public abstract class Field<T>
    {
        public string Title { get; set; }
        public Func<T, object> PropertySelector { get; set; }
        public Action<T, object> PropertySetter { get; set; }
        public abstract object Value { get; set; }
        public bool Focus { get; set; }
        public int FieldWidth { get; set; } = 20;
        public int LabelWidth { get; set; } = 20;
        public abstract void Draw(int left, int top);
        public abstract void Enter();
    }

    public class TextBox<T> : Field<T>
    {
        string value;
        public override object Value { get { return value; } set { this.value = value.ToString(); } }
        protected int left = 0;
        protected int top = 0;

        public override void Enter()
        {
            Console.SetCursorPosition(left + LabelWidth, top);
            string input = Console.ReadLine();
            Value = input;
        }

        public override void Draw(int left, int top)
        {
            this.left = left;
            this.top = top;
            Console.SetCursorPosition(left, top);
            Console.Write("{0,-" + LabelWidth + "}", Title);
            if (Focus)
                Screen.ColorFocus();
            else
                Screen.ColorField();
            Console.Write($"{{0,-{FieldWidth}}}", Value?.ToString());
            Screen.ColorDefault();
        }
    }

    public class IntBox<T> : TextBox<T>
    {
        int value;
        public override object Value
        {
            get => value;
            set
            {
                int.TryParse(value.ToString(), out this.value);
            }
        }

        public bool Validate(string input)
        {
            return int.TryParse(input, out _);
        }

        public override void Enter()
        {
            bool ok;
            string input;
            Console.CursorVisible = true;
            do
            {
                Console.SetCursorPosition(left + LabelWidth, top);
                input = Console.ReadLine();
                ok = Validate(input);
            } while (!ok);
            Value = input;
            Console.CursorVisible = false;
        }
    }


    public class DoubleBox<T> : TextBox<T>
    {
        double value;
        public override object Value
        {
            get => value;
            set
            {
                double.TryParse(value.ToString(), out this.value);
            }
        }

        public bool Validate(string input)
        {
            return double.TryParse(input, out _);
        }

        public override void Enter()
        {
            bool ok;
            string input;
            Console.CursorVisible = true;
            do
            {
                Console.SetCursorPosition(left + LabelWidth, top);
                input = Console.ReadLine();
                ok = Validate(input);
            } while (!ok);
            Value = input;
            Console.CursorVisible = false;
        }
    }

    public class SelectBox<T> : Field<T>
    {
        object value;
        public override object Value { get { return value; } set { this.value = value; } }
        public Dictionary<string, object> Options = new();
        int left = 0;
        int top = 0;
        int index = 0;

        public override void Enter()
        {
            bool stop = false;
            Func<int, object> valueByIndex = (idx) =>
            {
                int ii = 0;
                foreach (KeyValuePair<string, object> kv in Options)
                {
                    if (ii++ == idx) return kv.Value;
                }
                return null;
            };

            do
            {
                int i = 0;
                foreach (KeyValuePair<string, object> kv in Options)
                {
                    Console.SetCursorPosition(left + LabelWidth, top + 1 + i);
                    if (i == index)
                        Screen.ColorFocus();
                    else
                        Screen.ColorDefault();
                    Console.Write("{0," + FieldWidth + "}", kv.Key);
                    i++;
                }

                ConsoleKey key = Console.ReadKey().Key;
                index = Math.Clamp(index, 0, Options.Count - 1);
                switch (key)
                {
                    case ConsoleKey.Enter:
                        value = valueByIndex(index);
                        Screen.Clear();
                        stop = true;
                        break;
                    case ConsoleKey.UpArrow:
                        index--;
                        break;
                    case ConsoleKey.DownArrow:
                        index++;
                        break;
                    case ConsoleKey.Escape:
                        stop = true;
                        break;
                }
            } while (!stop);

            Console.CursorVisible = false;
            Screen.ColorDefault();
        }

        public override void Draw(int left, int top)
        {
            this.left = left;
            this.top = top;
            Console.SetCursorPosition(left, top);
            Console.Write("{0,-" + LabelWidth + "}", Title);
            if (Focus)
                Screen.ColorFocus();
            else
                Screen.ColorField();
            Console.Write($"{{0,-{FieldWidth}}}", Value?.ToString());
            Screen.ColorDefault();
        }
    }
}
