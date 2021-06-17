﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace PhotinoNET
{
    public partial class PhotinoWindow
    {
        //FLUENT EVENT HANDLER REGISTRATION
        public event EventHandler<Point> LocationChangedHandler;
        public PhotinoWindow RegisterLocationChangedHandler(EventHandler<Point> handler)
        {
            LocationChangedHandler += handler;
            return this;
        }
        public void OnLocationChanged(int left, int top)
        {
            var location = new Point(left, top);
            LocationChangedHandler?.Invoke(this, location);
        }



        public event EventHandler<Size> SizeChangedHandler;
        public PhotinoWindow RegisterSizeChangedHandler(EventHandler<Size> handler)
        {
            SizeChangedHandler += handler;
            return this;
        }
        public void OnSizeChanged(int width, int height)
        {
            var size = new Size(width, height);
            SizeChangedHandler?.Invoke(this, size);
        }



        public event EventHandler<string> WebMessageReceived;
        public PhotinoWindow RegisterWebMessageReceivedHandler(EventHandler<string> handler)
        {
            WebMessageReceived += handler;
            return this;
        }
        public void OnWebMessageReceived(string message)
        {
            WebMessageReceived?.Invoke(this, message);
        }


        public delegate bool NetClosingDelegate(object sender, EventArgs e);
        public event NetClosingDelegate WindowClosing;
        public PhotinoWindow RegisterWindowClosingHandler(NetClosingDelegate handler)
        {
            WindowClosing += handler;
            return this;
        }
        public byte OnWindowClosing()
        {
            //C++ handles bool values as a single byte, C# uses 4 bytes
            byte noClose = 0;
            var doNotClose = WindowClosing?.Invoke(this, null);
            if (doNotClose ?? false)
                noClose = 1;

            return noClose;
        }



        public event EventHandler WindowCreating;
        public PhotinoWindow RegisterWindowCreatingHandler(EventHandler handler)
        {
            WindowCreating += handler;
            return this;
        }
        public void OnWindowCreating()
        {
            WindowCreating?.Invoke(this, null);
        }



        public event EventHandler WindowCreated;
        public PhotinoWindow RegisterWindowCreatedHandler(EventHandler handler)
        {
            WindowCreated += handler;
            return this;
        }
        public void OnWindowCreated()
        {
            WindowCreated?.Invoke(this, null);
        }





        ///<summary>Custom schemes (other than 'http', 'https' and 'file') must be handled by creating the handlers manually.</summary>
        public delegate Stream NetCustomSchemeDelegate(string url, out string contentType);
        public event NetCustomSchemeDelegate CustomScheme;
        internal List<string> CustomSchemeNames = new List<string>();
        public PhotinoWindow RegisterCustomSchemeHandler(string scheme, NetCustomSchemeDelegate handler)
        {
            if (string.IsNullOrWhiteSpace(scheme))
                throw new ArgumentException("A scheme must be provided. (for example 'app' or 'custom'");

            if (handler == null)
                throw new ArgumentException("A handler (method) with a signature matching NetCustomSchemeDelegate must be supplied.");

            scheme = scheme.ToLower();
            if (CustomSchemeNames.Contains(scheme))
                throw new ArgumentException($"Scheme '{scheme}' has already been defined and cannot be re-defined.");

            if (_nativeInstance == IntPtr.Zero)
            {
                if (CustomSchemeNames.Count > 15)
                    throw new ApplicationException($"No more than 16 custom scheme handlers can be set prior to initialization. Additional handlers can be added after initialization.");
            }
            else
            {
                Photino_AddCustomSchemeName(_nativeInstance, scheme);
            }

            CustomSchemeNames.Add(scheme);
            CustomScheme += handler;

            return this;
        }

        public IntPtr OnCustomScheme(string url, out int numBytes, out string contentType)
        {
            //TODO: Parse the scheme from the URL
            var scheme = "app";

            if (!CustomSchemeNames.Contains(scheme))
                throw new ApplicationException($"A handler for the scheme '{scheme}' has not been registered.");

            var responseStream = CustomScheme.Invoke(url, out contentType);

            if (responseStream == null)
            {
                // Webview should pass through request to normal handlers (e.g., network)
                // or handle as 404 otherwise
                numBytes = 0;
                return default;
            }

            // Read the stream into memory and serve the bytes
            // In the future, it would be possible to pass the stream through into C++
            using (responseStream)
            using (var ms = new MemoryStream())
            {
                responseStream.CopyTo(ms);

                numBytes = (int)ms.Position;
                var buffer = Marshal.AllocHGlobal(numBytes);
                Marshal.Copy(ms.GetBuffer(), 0, buffer, numBytes);
                //_hGlobalToFree.Add(buffer);
                return buffer;
            }
        }
    }
}
