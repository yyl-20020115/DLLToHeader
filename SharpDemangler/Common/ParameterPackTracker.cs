using System;
using System.Linq;

namespace SharpDemangler.Common;

public class ParameterPackTracker
{
    public int CurrentPackIndex { get; set; }
    public int CurrentPackMax { get; set; }

    //public ParameterPackTracker(NodeArray array)
    //{
    //    CurrentPackIndex = 0;
    //    CurrentPackMax = array.Count();
    //}
}
