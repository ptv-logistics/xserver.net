// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Diagnostics;

namespace Ptv.XServer.Controls.Map.Tools
{
    /// <summary>
    /// Helper class which extends the System.Diagnostics.TraceSource class by an own customized layout
    /// of the messages. For example, a time stamp is added and unnecessary information (event id) is
    /// removed.
    /// </summary>
    public class Logger
    {
        private readonly TraceSource traceSource;

        /// <summary> Constructor initializes the incorporated TraceSource instance. </summary>
        /// <param name="name"> Name for the TraceSource instance. </param>
        public Logger(string name)
        {
            traceSource = new TraceSource(name);
        }

        /// <summary> Writes a message to each listener (including an additional new line), if event type is not filtered. </summary>
        /// <param name="eventType"> Level of logging, which may result in a filtering of this message. </param>
        /// <param name="message"> Text which should be logged. </param>
        public void Writeline(TraceEventType eventType, string message)
        {
            ForEachListener(eventType, message, (listener, newMessage) => { listener.WriteLine(newMessage); listener.Flush(); });
        }

        /// <summary> Writes a message to each listener (without an additional new line), if event type is not filtered. </summary>
        /// <param name="eventType"> Level of logging, which may result in a filtering of this message. </param>
        /// <param name="message"> Text which should be logged. </param>
        public void Write(TraceEventType eventType, string message)
        {
            ForEachListener(eventType, message, (listener, newMessage) => { listener.Write(newMessage); listener.Flush(); });
        }

        private void ForEachListener(TraceEventType eventType, string message, Action<TraceListener, string> action)
        {
            if (!traceSource.Switch.ShouldTrace(eventType)) return;

            var formattedMessage = $"{DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss:fff", System.Globalization.CultureInfo.InvariantCulture)} [{eventType}]: {message}";
            foreach (TraceListener listener in traceSource.Listeners)
                action(listener, formattedMessage);
        }
    }
}
