using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;

namespace Two10.AzureScaleMe.Worker
{
    static class Program
    {
        static void Main()
        {
            AzureScaleMe.ScaleMe.InstallCertificates();
            AzureScaleMe.ScaleMe.Run(10000);
        }
    }
}
