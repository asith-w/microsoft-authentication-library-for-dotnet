﻿//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Instance;

namespace Microsoft.Identity.Client
{
    internal abstract class EventBase : Dictionary<string, string>
    {
        protected const string EventNamePrefix = "msal.";
        public const string ConstEventName = EventNamePrefix + "event_name";
        protected const string StartTime = EventNamePrefix + "start_time";
        protected const string ElapsedTime = EventNamePrefix + "elapsed_time";
        private readonly long _startTimestamp;

        public const string TenantPlaceHolder = "<tenant>"; // It is used to replace the real tenant in telemetry info

        public EventBase(string eventName) : this(eventName, new Dictionary<string, string>()) {}

        protected static long CurrentUnixTimeMilliseconds()
        {
            return MsalHelpers.DateTimeToUnixTimestampMilliseconds(DateTimeOffset.Now);
        }

        public EventBase(string eventName, IDictionary<string, string> predefined) : base(predefined)
        {
            this[ConstEventName] = eventName;
            _startTimestamp = CurrentUnixTimeMilliseconds();
            this[StartTime] = _startTimestamp.ToString();
            this[ElapsedTime] = "-1";
        }

        public void Stop()
        {
            this[ElapsedTime] = (CurrentUnixTimeMilliseconds() - _startTimestamp).ToString();  // It is a duration
        }

        public static string ScrubTenant(Uri uri) // Note: There is also a Unit Test case for this helper
        {
            if (!uri.IsAbsoluteUri)
            {
                throw new ArgumentException("Requires an absolute uri");
            }
            // only collect telemetry for well-known hosts, // omit B2C ???
            if (!AadAuthority.IsInTrustedHostList(uri.Host)) // || uri.AbsolutePath.StartsWith("/" + B2CAuthority.Prefix))
            {
                return null;
            }

            var pieces = uri.AbsolutePath.Split('/'); // It looks like {"", "common", "oauth2", "v2.0", "token"}
            if (pieces.Length >= 2)
            {
                int tenantPosition = pieces[1] == B2CAuthority.Prefix ? 2 : 1;
                if (tenantPosition < pieces.Length)
                {
                    // Replace it rather than remove it. Otherwise the end result would misleadingly look like a complete URL while it is actually not.
                    pieces[tenantPosition] = TenantPlaceHolder;
                }
            }
            string scrubbedPath = String.Join("/", pieces);
            return uri.Scheme + "://" + uri.Authority + scrubbedPath;
        }
    }
}
