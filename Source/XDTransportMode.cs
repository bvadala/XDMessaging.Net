﻿/*=============================================================================
*
*	(C) Copyright 2011, Michael Carlisle (mike.carlisle@thecodeking.co.uk)
*
*   http://www.TheCodeKing.co.uk
*  
*	All rights reserved.
*	The code and information is provided "as-is" without waranty of any kind,
*	either expressed or implied.
*
*=============================================================================
*/
namespace TheCodeKing.Net.Messaging
{
    /// <summary>
    ///   This defines the tranport modes that can be used for interprocess communication.
    /// </summary>
    public enum XDTransportMode
    {
        /// <summary>
        ///   Uses Windows Messaging to pass data beteen processes using WM_COPYDATA. This mode
        ///   is ideal for performant communication between windows form based applications.
        /// </summary>
        WindowsMessaging,
        /// <summary>
        ///   Uses file watchers to trigger events and pass data between processes. This mode can 
        ///   be used within non-form based applications such as Windows Services.
        /// </summary>
        IOStream,
        /// <summary>
        ///   Uses MailSlots to broadcast messages across same domain/workgroup networks.
        /// </summary>
        MailSlot
    }
}