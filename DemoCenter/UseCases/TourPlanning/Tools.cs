//--------------------------------------------------------------
// Copyright (c) PTV Group
// 
// For license details, please refer to the file COPYING, which 
// should have been provided with this distribution.
//--------------------------------------------------------------

using System;
using System.ComponentModel;

namespace Ptv.XServer.Demo.UseCases.TourPlanning
{
    /// <summary>
    /// Helper class containing methods supporting the tour planning.
    /// </summary>
    public static class Tools
    {
        /// <summary>
        /// A helper method to invoke any method in a non-blocking, asynchronous way.
        /// </summary>
        /// <typeparam name="T">Type of the method and action parameters.</typeparam>
        /// <param name="method">Method which is to be called asynchronously.</param>
        /// <param name="success">Callback which is called after a successful execution of the asynchronous method, i.e. no exceptions occurred.</param>
        /// <param name="error">Callback which is called after a raised exception in the asynchronous method.</param>
        public static void AsyncUIHelper<T>(Func<T> method, Action<T> success, Action<Exception> error)
        {
            var worker = new BackgroundWorker();
            worker.DoWork += (o, e) =>
            {
                try { e.Result = method(); }
                catch (Exception ex) { e.Result = ex; }
            };
            worker.RunWorkerCompleted += (o, e) =>
            {
                try
                {
                    if (e.Result is Exception)
                        error(e.Result as Exception);
                    else
                        success((T)e.Result);
                }
                finally
                {
                    worker.Dispose();
                }
            };
            worker.RunWorkerAsync();
        }
    }
}
