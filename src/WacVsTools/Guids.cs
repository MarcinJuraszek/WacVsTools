// Guids.cs
// MUST match guids.h
using System;

namespace Microsoft.WacVsTools
{
    static class GuidList
    {
        public const string guidWacVsToolsPkgString = "0809a33e-05bc-4e05-ac32-fe6423441c5e";
        public const string guidWacVsToolsCmdSetString = "86bdeca8-eca7-4b32-9a36-9e896c38ea7a";

        public static readonly Guid guidWacVsToolsCmdSet = new Guid(guidWacVsToolsCmdSetString);
    };
}