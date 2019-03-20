using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Rook.Framework.Core.Application.ResponseHandlers;

namespace Rook.Framework.Core.Application.Message
{
    /// <summary>
	/// A POCO representation of the JSON message
	/// </summary>
	public class Message<TNeed, TSolution>
	{
		/// <summary>
		/// Gets or sets the UUID for the message.
		/// </summary>
		/// <value>
		/// The UUID.
		/// </value>
		[Required, JsonRequired, JsonProperty]
		public Guid Uuid { get; set; }

		/// <summary>
		/// Gets or sets the the service that initiated the request/message
		/// </summary>
		/// <value>
		/// The source.
		/// </value>
		[Required, JsonRequired, JsonProperty]
		public string Source { get; set; }

		/// <summary>
		/// Gets or sets the  date/time that the message was created ( UTC ).
		/// </summary>
		/// <value>
		/// The published time.
		/// </value>
		[Required, JsonRequired, JsonProperty]
		public DateTime PublishedTime { get; set; }

		/// <summary>
		/// Gets or sets the the name of the last service to decorate this message
		/// </summary>
		/// <value>
		/// The last modified by.
		/// </value>
		[Required, JsonRequired, JsonProperty]
		public string LastModifiedBy { get; set; }

		/// <summary>
		/// Gets or sets  the date/time that the message was last updated ( UTC )
		/// </summary>
		/// <value>
		/// The last modified time.
		/// </value>
		[Required, JsonRequired, JsonProperty]
		public DateTime LastModifiedTime { get; set; }

		/// <summary>
		/// Gets or sets a use-case oriented requirement e.g. GetByUserId
		/// </summary>
		/// <value>
		/// The method.
		/// </value>
		[Required, JsonRequired, JsonProperty,/* Obsolete("Migrate to Message2<TNeed,TSolution> where TNeed : NeedBase")*/]
		public string Method { get; set; }

		/// <summary>
		/// Gets or sets the need. 
		/// The data required to satisfy the request (essentially the method params)
		/// </summary>
		/// <value>
		/// The need.
		/// </value>
		[Required, JsonRequired, JsonProperty]
		public TNeed Need { get; set; }

		/// <summary>
		/// Gets or sets the solution.
		/// A complex domain object that satisfies the need. Many services can contribute solution items to the solution
		/// </summary>
		/// <value>
		/// The solution.
		/// </value>
		[Required, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
		public TSolution[] Solution { get; set; }

		/// <summary>
		/// Gets or sets the errors.
		/// //An array of error message objects with source and message properties
		/// </summary>
		/// <value>
		/// The errors.
		/// </value>
		[Required, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
		public List<ResponseError> Errors { get; set; } = new List<ResponseError>();

		/// <summary>
		/// Used to indicate to Newtonsoft whether to attempt to serialise the Errors property
		/// </summary>
		/// <returns></returns>
		public bool ShouldSerializeErrors()
		{
			return Errors.Any();
		}
	}
}
