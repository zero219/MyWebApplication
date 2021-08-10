using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.ActionAttributes
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class RequestHeaderMatchesMediaTypeAttribute : Attribute, IActionConstraint
    {
        private readonly MediaTypeCollection _mediaTypes = new MediaTypeCollection();
        private readonly string _requestHeaderToMatch;

        public RequestHeaderMatchesMediaTypeAttribute(string requestHeaderToMatch, string mediaType, params string[] otherMediaTypes)
        {
            this._requestHeaderToMatch = requestHeaderToMatch ?? throw new ArgumentNullException(nameof(requestHeaderToMatch));
            if (MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue parsedMediaType))
            {
                _mediaTypes.Add(mediaType);
            }
            else
            {
                throw new ArgumentException(nameof(mediaType));
            }

            foreach (var otherMediaType in otherMediaTypes)
            {
                if (MediaTypeHeaderValue.TryParse(otherMediaType, out MediaTypeHeaderValue parsedOtherMediaType))
                {
                    _mediaTypes.Add(mediaType);
                }
                else
                {
                    throw new ArgumentException(nameof(otherMediaTypes));
                }
            }
        }
        public int Order => 0;

        public bool Accept(ActionConstraintContext context)
        {
            var requestHeaders = context.RouteContext.HttpContext.Request.Headers;
            if (!requestHeaders.ContainsKey(_requestHeaderToMatch))
            {
                return false;
            }

            var parsedRequestMediaType = new MediaType(requestHeaders[_requestHeaderToMatch]);

            foreach (var mediaType in _mediaTypes)
            {
                var parsedMediaType = new MediaType(mediaType);
                if (parsedRequestMediaType.Equals(parsedMediaType))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
