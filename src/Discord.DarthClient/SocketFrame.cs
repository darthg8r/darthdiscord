using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

#pragma warning disable CS1591
namespace Discord.DarthClient
{
    internal class SocketFrame
    {
        [JsonProperty("op")]
        public int Operation { get; set; }
        [JsonProperty("t", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }
        [JsonProperty("s", NullValueHandling = NullValueHandling.Ignore)]
        public int? Sequence { get; set; }
        [JsonProperty("d")]
        public object Payload { get; set; }
    }

    public enum LoginState : byte
    {
	    /// <summary> The client is currently logged out. </summary>
	    LoggedOut,
	    /// <summary> The client is currently logging in. </summary>
	    LoggingIn,
	    /// <summary> The client is currently logged in. </summary>
	    LoggedIn,
	    /// <summary> The client is currently logging out. </summary>
	    LoggingOut
    }



}
