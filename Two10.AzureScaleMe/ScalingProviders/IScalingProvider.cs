using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Two10.AzureScaleMe.ScalingProviders
{
    public interface IScalingProvider
    {
        bool Scale(int delta);

    }
}
