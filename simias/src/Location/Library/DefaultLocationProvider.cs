/***********************************************************************
 *  $RCSfile$
 *
 *  Copyright (C) 2004 Novell, Inc.
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU General Public
 *  License as published by the Free Software Foundation; either
 *  version 2 of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *  General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public
 *  License along with this program; if not, write to the Free
 *  Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 *
 *  Author: Rob
 *
 ***********************************************************************/

using System;

using Simias.Sync;

namespace Simias.Location
{
	/// <summary>
	/// Default Location Provider
	/// </summary>
	public class DefaultLocationProvider : ILocationProvider
	{
		private string storePath;

		/// <summary>
		/// Default Constructor
		/// </summary>
		public DefaultLocationProvider()
		{
		}

		#region ILocationProvider Members
		
		/// <summary>
		/// Configure the location provider.
		/// </summary>
		/// <param name="configuration">The Simias configuration object.</param>
		public void Configure(Configuration configuration)
		{
			storePath = configuration.StorePath;
		}

		/// <summary>
		/// Locate the collection master.
		/// </summary>
		/// <param name="collection">The collection id.</param>
		/// <returns>A URI object containing the location of the collection master, or null.</returns>
		public Uri Locate(string collection)
		{
			Uri result = null;

			SyncStore syncStore = new SyncStore(storePath);

			SyncCollection syncCollection = syncStore.OpenCollection(collection);

			if (syncCollection != null)
			{
				result = syncCollection.MasterUri;
			}

			return result;
		}

		#endregion
	}
}
