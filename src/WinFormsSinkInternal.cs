﻿using System;
using System.IO;

using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Formatting.Json;

namespace Serilog.Sinks.WinForms
{
    public sealed class WinFormsSinkInternal : ILogEventSink
    {
        public delegate void LogHandler(string sourceContext, string str);

        public event LogHandler OnLogReceived;

        public delegate void GridLogHandler(GridLogEvent logEvent);

        public event GridLogHandler OnGridLogReceived;

        private readonly ITextFormatter _textFormatter;

        private readonly bool _isGridLogger;

        public WinFormsSinkInternal(ITextFormatter textFormatter, bool isGridLogger = false)
        {
            _textFormatter = textFormatter;
            _isGridLogger = isGridLogger;
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null)
            {
                throw new ArgumentNullException(nameof(logEvent));
            }

            if (_textFormatter == null)
            {
                throw new ArgumentNullException($"Missing Log Formatter");
            }

            var renderSpace = new StringWriter();
            _textFormatter.Format(logEvent, renderSpace);

            if (_isGridLogger)
            {
                OnGridLogReceived?.Invoke(new GridLogEvent { Level = logEvent.Level, TimeStamp = logEvent.Timestamp, Message = renderSpace.ToString() });

                return;
            }

            logEvent.Properties.TryGetValue("SourceContext", out var contextProperty);

            FireEvent(contextProperty?.ToString().Trim('"'), renderSpace.ToString());
        }

        private void FireEvent(string context, string str)
        {
            OnLogReceived?.Invoke(context, str);
        }
    }

    public static class WindFormsSink
    {
        private static readonly ITextFormatter _defaultTextFormatter = new MessageTemplateTextFormatter("{Timestamp:HH:mm:ss} {Level} {Message:lj}{NewLine}{Exception}");

        public static WinFormsSinkInternal SimpleTextBoxSink { get; private set; } = new WinFormsSinkInternal(_defaultTextFormatter);

        public static WinFormsSinkInternal JsonTextBoxSink { get; private set; } = new WinFormsSinkInternal(new JsonFormatter());

        public static readonly WinFormsSinkInternal GridLogSink = new WinFormsSinkInternal(new MessageTemplateTextFormatter("{Message}{NewLine}{Exception}"), true);

        public static WinFormsSinkInternal MakeSimpleTextBoxSink(ITextFormatter formatter = null)
        {
            if (formatter == null) { formatter = _defaultTextFormatter; }

            SimpleTextBoxSink = new WinFormsSinkInternal(formatter);

            return SimpleTextBoxSink;
        }

        public static WinFormsSinkInternal MakeJsonTextBoxSink(ITextFormatter formatter = null)
        {
            if (formatter == null) { formatter = new JsonFormatter(); }

            JsonTextBoxSink = new WinFormsSinkInternal(formatter);

            return JsonTextBoxSink;
        }
    }
}
