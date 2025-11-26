// MiniJSON for Unity (Safe Version)
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MiniJSON
{
    public static class Json
    {
        public static object Deserialize(string json)
        {
            if (json == null) return null;
            return Parser.Parse(json);
        }

        public static string Serialize(object obj)
        {
            return Serializer.Serialize(obj);
        }

        sealed class Parser : IDisposable
        {
            const string WORD_BREAK = "{}[],:\"";

            public static object Parse(string json)
            {
                using (var instance = new Parser(json))
                {
                    return instance.ParseValue();
                }
            }

            private StringReader json;

            private Parser(string jsonString)
            {
                json = new StringReader(jsonString);
            }

            public void Dispose()
            {
                json.Dispose();
                json = null;
            }

            private Dictionary<string, object> ParseObject()
            {
                Dictionary<string, object> table = new Dictionary<string, object>();

                json.Read(); // {

                while (true)
                {
                    switch (NextToken)
                    {
                        case TOKEN.NONE:
                            return null;

                        case TOKEN.CURLY_CLOSE:
                            json.Read();
                            return table;

                        default:
                            string name = ParseString();
                            if (name == null)
                                return null;

                            if (NextToken != TOKEN.COLON)
                                return null;

                            json.Read(); // :

                            table[name] = ParseValue();
                            break;
                    }
                }
            }

            private List<object> ParseArray()
            {
                List<object> array = new List<object>();
                json.Read(); // [

                var parsing = true;
                while (parsing)
                {
                    TOKEN nextToken = NextToken;

                    switch (nextToken)
                    {
                        case TOKEN.NONE:
                            return null;

                        case TOKEN.SQUARE_CLOSE:
                            json.Read();
                            return array;

                        default:
                            array.Add(ParseByToken(nextToken));
                            break;
                    }
                }

                return array;
            }

            private object ParseValue()
            {
                TOKEN nextToken = NextToken;
                return ParseByToken(nextToken);
            }

            private object ParseByToken(TOKEN token)
            {
                switch (token)
                {
                    case TOKEN.STRING:
                        return ParseString();

                    case TOKEN.NUMBER:
                        return ParseNumber();

                    case TOKEN.CURLY_OPEN:
                        return ParseObject();

                    case TOKEN.SQUARE_OPEN:
                        return ParseArray();

                    case TOKEN.TRUE:
                        json.Read();
                        return true;

                    case TOKEN.FALSE:
                        json.Read();
                        return false;

                    case TOKEN.NULL:
                        json.Read();
                        return null;
                }

                return null;
            }

            private string ParseString()
            {
                StringBuilder sb = new StringBuilder();
                json.Read(); // "

                bool parsing = true;
                while (parsing)
                {
                    if (json.Peek() == -1)
                        break;

                    char c = NextChar;
                    if (c == '"')
                    {
                        parsing = false;
                        break;
                    }

                    if (c == '\\')
                    {
                        if (json.Peek() == -1)
                            break;

                        c = NextChar;
                        switch (c)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                char[] hex = new char[4];
                                for (int i = 0; i < 4; i++)
                                    hex[i] = NextChar;
                                sb.Append((char)Convert.ToInt32(new string(hex), 16));
                                break;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }

                return sb.ToString();
            }

            private object ParseNumber()
            {
                string number = NextWord;

                if (number.Contains("."))
                {
                    double parsedDouble;
                    double.TryParse(number, out parsedDouble);
                    return parsedDouble;
                }

                long parsedInt;
                long.TryParse(number, out parsedInt);
                return parsedInt;
            }

            private char NextChar => (char)json.Read();
            private string NextWord
            {
                get
                {
                    StringBuilder sb = new StringBuilder();

                    while (!IsWordBreak(PeekChar))
                    {
                        sb.Append(NextChar);

                        if (json.Peek() == -1)
                            break;
                    }

                    return sb.ToString();
                }
            }

            private char PeekChar => (char)json.Peek();

            private enum TOKEN
            {
                NONE, CURLY_OPEN, CURLY_CLOSE, SQUARE_OPEN, SQUARE_CLOSE,
                COLON, COMMA, STRING, NUMBER, TRUE, FALSE, NULL
            }

            private TOKEN NextToken
            {
                get
                {
                    while (Char.IsWhiteSpace(PeekChar))
                    {
                        json.Read();

                        if (json.Peek() == -1)
                            return TOKEN.NONE;
                    }

                    char c = PeekChar;
                    switch (c)
                    {
                        case '{': return TOKEN.CURLY_OPEN;
                        case '}': return TOKEN.CURLY_CLOSE;
                        case '[': return TOKEN.SQUARE_OPEN;
                        case ']': return TOKEN.SQUARE_CLOSE;
                        case ',': json.Read(); return TOKEN.COMMA;
                        case '"': return TOKEN.STRING;
                        case ':': return TOKEN.COLON;
                        case '-':
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            return TOKEN.NUMBER;
                    }

                    string word = NextWord;

                    switch (word)
                    {
                        case "true": return TOKEN.TRUE;
                        case "false": return TOKEN.FALSE;
                        case "null": return TOKEN.NULL;
                    }

                    return TOKEN.NONE;
                }
            }

            private bool IsWordBreak(char c)
            {
                return Char.IsWhiteSpace(c) || WORD_BREAK.IndexOf(c) != -1;
            }
        }

        sealed class Serializer
        {
            private StringBuilder builder;

            private Serializer()
            {
                builder = new StringBuilder();
            }

            public static string Serialize(object obj)
            {
                var instance = new Serializer();
                instance.SerializeValue(obj);
                return instance.builder.ToString();
            }

            private void SerializeValue(object value)
            {
                if (value == null)
                {
                    builder.Append("null");
                }
                else if (value is string)
                {
                    SerializeString((string)value);
                }
                else if (value is bool)
                {
                    builder.Append((bool)value ? "true" : "false");
                }
                else if (value is IDictionary)
                {
                    SerializeObject((IDictionary)value);
                }
                else if (value is IList)
                {
                    SerializeArray((IList)value);
                }
                else if (value is char)
                {
                    SerializeString(value.ToString());
                }
                else
                {
                    SerializeNumber(value);
                }
            }

            private void SerializeObject(IDictionary obj)
            {
                builder.Append('{');
                bool first = true;

                foreach (object key in obj.Keys)
                {
                    if (!first)
                        builder.Append(',');

                    SerializeString(key.ToString());
                    builder.Append(':');
                    SerializeValue(obj[key]);
                    first = false;
                }

                builder.Append('}');
            }

            private void SerializeArray(IList array)
            {
                builder.Append('[');

                bool first = true;

                foreach (object obj in array)
                {
                    if (!first)
                        builder.Append(',');

                    SerializeValue(obj);
                    first = false;
                }

                builder.Append(']');
            }

            private void SerializeString(string str)
            {
                builder.Append('\"');

                foreach (char c in str)
                {
                    switch (c)
                    {
                        case '\\': builder.Append("\\\\"); break;
                        case '\"': builder.Append("\\\""); break;
                        case '\n': builder.Append("\\n"); break;
                        case '\r': builder.Append("\\r"); break;
                        case '\t': builder.Append("\\t"); break;
                        default: builder.Append(c); break;
                    }
                }

                builder.Append('\"');
            }

            private void SerializeNumber(object number)
            {
                builder.Append(number.ToString());
            }
        }
    }
}
