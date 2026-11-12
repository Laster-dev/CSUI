// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace MS.Internal
{
    internal delegate void AvTraceEventHandler(AvTraceBuilder traceBuilder, ReadOnlySpan<object> parameters);

    internal class AvTraceFormat : AvTraceDetails
    {
        public AvTraceFormat(AvTraceDetails details, object[] args)
            : base(details.Id, details.Labels)
        {
            string fmt = details.Labels != null && details.Labels.Length > 0 ? details.Labels[0] : string.Empty;
            _message = args != null && args.Length > 0
                ? string.Format(CultureInfo.InvariantCulture, fmt, args)
                : fmt;
        }

        public override string Message => _message;

        private readonly string _message;
    }

    internal sealed class AvTraceBuilder
    {
        public AvTraceBuilder(string message)
        {
            _sb = new StringBuilder(message);
        }

        public void Append(string message) => _sb.Append(message);

        public void AppendFormat(string message, params object[] args)
        {
            object[] formattedArgs = new object[args.Length];
            for (int i = 0; i < args.Length; ++i)
            {
                string s = args[i] as string;
                formattedArgs[i] = s ?? AvTrace.ToStringHelper(args[i]);
            }

            _sb.AppendFormat(CultureInfo.InvariantCulture, message, formattedArgs);
        }

        public override string ToString() => _sb.ToString();

        private readonly StringBuilder _sb;
    }

    internal sealed class AvTrace
    {
        public AvTrace(Func<object> getTraceSourceDelegate, Action clearTraceSourceDelegate)
        {
        }

        public event AvTraceEventHandler TraceExtraMessages;

        public bool IsEnabled => false;
        public bool IsEnabledOverride => false;
        public bool EnabledByDebugger { get; set; }
        public bool SuppressGeneratedParameters { get; set; }

        public void Refresh() { }

        public string Trace(TraceEventType type, int eventId, string message, string[] labels, object[] parameters)
        {
            _ = TraceExtraMessages;
            return null;
        }

        public string Trace(TraceEventType type, int eventId, string message, string[] labels, ReadOnlySpan<object> parameters)
        {
            _ = TraceExtraMessages;
            return null;
        }

        public void TraceStartStop(int eventID, string message, string[] labels, object[] parameters) { }

        public void TraceStartStop(int eventID, string message, string[] labels, ReadOnlySpan<object> parameters) { }

        public static string ToStringHelper(object value)
        {
            if (value == null)
            {
                return "<null>";
            }

            try
            {
                return AntiFormat(value.ToString());
            }
            catch
            {
                return "<unprintable>";
            }
        }

        public static string AntiFormat(string s)
        {
            if (string.IsNullOrEmpty(s) || (s.IndexOf('{') < 0 && s.IndexOf('}') < 0))
            {
                return s;
            }

            return s.Replace("{", "{{").Replace("}", "}}");
        }

        public static int GetHashCodeHelper(object value)
        {
            try
            {
                return value?.GetHashCode() ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        public static string TypeNameHelper(object value)
        {
            return value == null ? "<null>" : value.GetType().Name;
        }

        public static string TypeName(object value)
        {
            return TypeNameHelper(value);
        }
    }
}
