﻿namespace Utility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Data;
    using System.Xml;
    using Models.Core;
    using APSIM.Shared.Utilities;
    using System.Reflection;
    using APSIM.Core;

    /// <summary>
    /// Graph utility functions.
    /// </summary>
    public class Graph
    {
        public static Models.Graph CreateGraphFromResource(string resourceName)
        {
            string graphXmL = ReflectionUtilities.GetResourceAsString(resourceName);

            if (graphXmL != null)
            {
                List<Exception> errors = null;
                Models.Graph graph = FileFormat.ReadFromFile<Graph>(graphXmL).Model as Models.Graph;
                if (errors != null && errors.Any())
                    throw errors.First();
                graph.ParentAllDescendants();
                return graph;
            }
            return null;
        }
    }
}
