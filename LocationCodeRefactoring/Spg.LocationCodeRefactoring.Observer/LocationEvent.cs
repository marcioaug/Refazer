using Spg.LocationRefactor.TextRegion;
using System.Collections.Generic;
using System;
using Spg.LocationRefactor.Location;

namespace Spg.LocationCodeRefactoring.Observer
{
    /// <summary>
    /// Locations selected event
    /// </summary>
    public class LocationEvent
    {
        /// <summary>
        /// List of locations selected
        /// </summary>
        public List<CodeLocation> locations { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="locations">Locations in each source code</param>
        public LocationEvent(List<CodeLocation> locations)
        {
            this.locations = locations;
        }
    }
}