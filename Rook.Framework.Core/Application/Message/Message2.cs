using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Rook.Framework.Core.Application.Message {
    public sealed class Message2<TNeed, TSolution> : Message<TNeed, TSolution> where TNeed : NeedBase
    {
        /// <summary>
        /// Gets or sets a use-case oriented requirement e.g. GetByUserId
        /// </summary>
        /// <value>
        /// The method.
        /// </value>
        [Required, JsonRequired, JsonProperty/*, Obsolete("Use Need.NeedType")*/]
        public new string Method
        {
            get => Need.NeedType;
            set => Need.NeedType = value;
        }
    }
}