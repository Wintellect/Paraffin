//------------------------------------------------------------------------------
// <copyright file= "SmartConsoleTraceListener.cs" company="Wintellect">
//    Copyright (c) 2002-2017 John Robbins/Wintellect -- All rights reserved.
// </copyright>
// <Project>
//    Wintellect Debugging .NET Code
// </Project>
//------------------------------------------------------------------------------
namespace Wintellect.Paraffin
{
    using System;
    using System.Diagnostics;
    using System.Globalization;

    /// <summary>
    /// Cleaner console output for TraceSource output.
    /// </summary>
    /// <remarks>
    /// TraceSource output is prefixed and suffixed with a lot of extra 
    /// information such as the TraceSource name and the event ID. As I don't
    /// want to see that information in Paraffin's output so I'll create the 
    /// custom listener to do only the main output and nothing else.
    /// </remarks>
    internal sealed class SmartConsoleTraceListener : ConsoleTraceListener
    {
        /// <summary>
        /// Writes trace information and the formatted array of objects to the
        /// console.
        /// </summary>
        /// <param name="eventCache">
        /// The parameter is not used.
        /// </param>
        /// <param name="source">
        /// The parameter is not used.
        /// </param>
        /// <param name="eventType">
        /// The parameter is not used.
        /// </param>
        /// <param name="id">
        /// The parameter is not used.
        /// </param>
        /// <param name="format">
        /// A format string that contains zero or more format items, which 
        /// correspond to objects in the args array.
        /// </param>
        /// <param name="args">
        /// An object array containing zero or more objects to format.
        /// </param>
        public override void TraceEvent(TraceEventCache eventCache,
                                        String source,
                                        TraceEventType eventType,
                                        Int32 id,
                                        String format,
                                        params Object[] args)
        {
            String info = String.Format(CultureInfo.InvariantCulture,
                                        format,
                                        args);
            this.WriteLine(info);
        }
    }
}
