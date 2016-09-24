﻿using System;
using System.Runtime.Serialization;

namespace VkNet.Enums
{
	/// <summary>
	/// Информация о том прочитано ли сообщение.
	/// </summary>
	[DataContract]
	public enum MessageReadState
    {
        /// <summary>
        /// Сообщение не прочитано.
        /// </summary>
        Unreaded = 0,

        /// <summary>
        /// Сообщение прочитано.
        /// </summary>
        Readed = 1
    }
}