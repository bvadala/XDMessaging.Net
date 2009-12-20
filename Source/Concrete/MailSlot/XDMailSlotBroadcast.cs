﻿/*=============================================================================
*
*	(C) Copyright 2007, Michael Carlisle (mike.carlisle@thecodeking.co.uk)
*
*   http://www.TheCodeKing.co.uk
*  
*	All rights reserved.
*	The code and information is provided "as-is" without waranty of any kind,
*	either expressed or implied.
*
*=============================================================================
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using System.DirectoryServices.ActiveDirectory;
using System.Runtime.InteropServices;

namespace TheCodeKing.Net.Messaging.Concrete.MailSlot
{
    /// <summary>
    /// Implementation of IXDBroadcast which sends messages over the local network and 
    /// interprocess. Messages sent to a named channel will be received by the first available
    /// instance of IXDListener in the same mode. Each message may only be read by ONE listener
    /// on a single machine. Other listeners will queue until they can take ownership of the listener
    /// singleton.
    /// </summary>
    public sealed class XDMailSlotBroadcast : IXDBroadcast
    {
        /// <summary>
        /// Indicates the base path of the MailSlot.
        /// </summary>
        internal const string SlotLocation = @"\mailslot\xdmessaging\";
        /// <summary>
        /// The unique identifier for the MailSlot.
        /// </summary>
        private readonly string mailSlotIdentifier;
        /// <summary>
        /// Static constructor for initializing the current network domain or workgroup
        /// to which messages will be broadcast.
        /// </summary>
        static XDMailSlotBroadcast()
        {
        }
        /// <summary>
        /// Internal constructor.
        /// </summary>
        internal XDMailSlotBroadcast(bool propagateNetwork)
        {
            if (propagateNetwork)
            {
                // address of network MailSlot
                this.mailSlotIdentifier = string.Concat(@"\\*", SlotLocation); 
            }
            else
            {
                // address of local MailSlot
                this.mailSlotIdentifier = string.Concat(@"\\", Environment.MachineName, SlotLocation); 
            }
        }
        /// <summary>
        /// Implementation of IXDBroadcast for sending messages to a named channel on the local network.
        /// </summary>
        /// <param name="channel">The channel on which to send the message.</param>
        /// <param name="message">The message.</param>
        public void SendToChannel(string channelName, string message)
        {
            if (string.IsNullOrEmpty(channelName))
            {
                throw new ArgumentNullException(channelName, "The channel name must be defined");
            }
            if (message == null)
            {
                throw new ArgumentNullException(message, "The messsage packet cannot be null");
            }
            if (string.IsNullOrEmpty(channelName))
            {
                throw new ArgumentException("The channel name may not contain the ':' character.", "channelName");
            }

            //synchronize writes to mailslot
            bool created = false;
            string mailSlotId = string.Concat(mailSlotIdentifier, channelName);
            using (Mutex mutex = new Mutex(true, mailSlotId.Replace(@"\", "."), out created))
            {
                if (!created)
                {
                    try
                    {
                        mutex.WaitOne();
                    }
                    catch (ThreadInterruptedException) { }
                    catch (AbandonedMutexException) { }
                }

                IntPtr writeHandle = IntPtr.Zero;
                try
                {
                    writeHandle = Native.CreateFile(mailSlotId, FileAccess.Write, FileShare.Read, 0, FileMode.Open, 0, IntPtr.Zero);
                    if ((int)writeHandle > 0)
                    {
                        // format the message
                        string raw = string.Format("{0}:{1}", channelName, message);

                        // serialize the data
                        byte[] bytes;
                        NativeOverlapped overlap = new System.Threading.NativeOverlapped();
                        BinaryFormatter b = new BinaryFormatter();
                        using (MemoryStream stream = new MemoryStream())
                        {
                            b.Serialize(stream, raw);
                            stream.Flush();
                            int dataSize = (int)stream.Length;

                            // create byte array
                            bytes = new byte[256];
                            int bytesRead = 0;
                            uint bytesWritten = 0;
                            stream.Seek(0, SeekOrigin.Begin);
                            while ((bytesRead = stream.Read(bytes, 0, bytes.Length)) > 0)
                            {
                                while (!Native.WriteFile(writeHandle, bytes, (uint)bytesRead, ref bytesWritten, ref overlap))
                                {
                                    // IO fail, keep retrying
                                }
                            }
                        }
                    }
                    else
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        throw new IOException(string.Format("{0} Unable to open mailslot. Try again later.", errorCode));
                    }
                    try
                    {
                        // this tick is because MailSlot cannot handle
                        // hign throughput and would otherwise drop messages
                        Thread.Sleep(1);
                    }
                    catch (ThreadInterruptedException) { }
                    catch (AbandonedMutexException) { }
                }
                finally
                {
                    // close the handle
                    if ((int)writeHandle > 0)
                    {
                        Native.CloseHandle(writeHandle);
                    }
                }
            }
        }
    }
}
