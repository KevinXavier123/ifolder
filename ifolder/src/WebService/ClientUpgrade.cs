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
 *  Author: Mike Lasky <mlasky@novell.com>
 *
 ***********************************************************************/
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;

using Simias.Client;
using Simias;

namespace Novell.iFolder.Install
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class ClientUpgrade
	{
		#region Class Members
		/// <summary>
		/// Temporary directory used to copy the ifolder updates to.
		/// </summary>
		private static string iFolderUpdateDirectory = "ead51d60-cd98-4d35-8c7c-b43a2ca949c8";

		/// <summary>
		/// iFolder Client update files.
		/// </summary>
		private static string iFolderWindowsApplication = "iFolderApp.exe";

		/// <summary>
		/// Strings used in the handler query.
		/// </summary>
		private static string PlatformQuery = "Platform";
		private static string FileQuery = "File";

		/// <summary>
		/// Web service object to use for checking for client updates.
		/// </summary>
		private ClientUpdate service = null;

		/// <summary>
		/// Address to the host where the web service is running.
		/// </summary>
		private string hostAddress;
		#endregion

		#region Constructor
		
		/// <summary>
		/// Initializes a new instance of the object.
		/// </summary>
		/// <param name="domainID">Identifier of the domain that the user belongs to.</param>
		private ClientUpgrade( string domainID )
		{
			// Connect to the local web service.
			SimiasWebService simiasSvc = new SimiasWebService();
			simiasSvc.Url = Manager.LocalServiceUrl.ToString() + "/Simias.asmx";
			WebState ws = new WebState(domainID, domainID);
			ws.InitializeWebClient(simiasSvc);

			// Get the local domain information.
			DomainInformation domainInfo = simiasSvc.GetDomainInformation( domainID );
			if ( domainInfo != null )
			{
				// Get the address of the host service.
				hostAddress = domainInfo.Host;
				// Setup the url to the server.
				service = new ClientUpdate();
				service.Url = hostAddress + "/ClientUpdate.asmx";
				ws.InitializeWebClient(service);
			}
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Downloads the specified files in the list from the server.
		/// </summary>
		/// <param name="fileList">List of files to download.</param>
		/// <returns>The path to the downloaded files.</returns>
		private string DownloadFiles( string[] fileList )
		{
			// Create the temporary directory.
			string downloadDir = Path.Combine( Path.GetTempPath(), iFolderUpdateDirectory );
			if ( Directory.Exists( downloadDir ) )
			{
				// Clean up the old directory.
				Directory.Delete( downloadDir, true );
			}

			// Create it new everytime.
			Directory.CreateDirectory( downloadDir );
		
			try
			{
				// Construct a WebClient to do the downloads.
				WebClient webClient = new WebClient();
				webClient.Credentials = service.Credentials; 
				foreach ( string file in fileList )
				{
					// Add the filename as the query string.
					NameValueCollection nvc = new NameValueCollection();
					nvc.Add( PlatformQuery, MyEnvironment.Platform.ToString() );
					nvc.Add( FileQuery, file );
					webClient.QueryString = nvc;
					webClient.DownloadFile( hostAddress + "/ClientUpdateHandler.ashx", Path.Combine( downloadDir, file ) );
				}
			}
			catch
			{
				// Delete the directory.
				Directory.Delete( downloadDir, true );
				downloadDir = null;
			}

			return downloadDir;
		}

		/// <summary>
		/// Gets the version of the currently running Windows client.
		/// </summary>
		/// <returns>A Version object containing the version of the client if successful.
		/// Otherwise null is returned.</returns>
		private string GetWindowsClientVersion()
		{
			string version = null;

			string fullPath = Path.Combine( SimiasSetup.bindir, iFolderWindowsApplication );
			if ( File.Exists( fullPath ) )
			{
				FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo( fullPath );
				version = versionInfo.ProductVersion;
			}

			return version;
		}
		
		/// <summary>
		/// Checks to see if there is a newer client application on the domain server and
		/// prompts the user to upgrade.
		/// </summary>
		/// <param name="domainID">The ID of the domain to check for updates against.</param>
		/// <returns>The version of the update if available. Otherwise null is returned.</returns>
		private string CheckForUpdate()
		{
			string updateVersion = null;

			// Make sure that the service object is authenticated.
			if ( service != null )
			{
				// Get the current version of this client.
				string currentVersion = null;
				if ( MyEnvironment.Platform == MyPlatformID.Windows )
				{
					currentVersion = GetWindowsClientVersion();
				}
				else
				{
					// TODO: Get the current client version for Linux.
				}

				if ( currentVersion != null )
				{
					// Call to the web service to see if there is a version newer than the one
					// that is currently running.
					updateVersion = service.IsUpdateAvailable( MyEnvironment.Platform.ToString(), currentVersion );
				}
			}

			return updateVersion;
		}


		/// <summary>
		/// Gets the updated client application and runs the installation program.
		/// Note: This call will return before the application is updated.
		/// </summary>
		/// <returns>True if the installation program is successfully started. Otherwise false is returned.</returns>
		private bool RunUpdate()
		{
			bool running = false;

			// Make sure that the service is authenticated.
			if ( service != null )
			{
				// Get the list of files needed for the update.
				string[] fileList = service.GetUpdateFiles();
				if ( fileList != null )
				{
					// Download the files in the list to a temporary directory.
					string downloadDir = DownloadFiles( fileList );
					if ( downloadDir != null )
					{
						if ( MyEnvironment.Platform == MyPlatformID.Windows )
						{
							// There should only be one file needed for the windows update.
							Process installProcess = new Process();
							installProcess.StartInfo.FileName = Path.Combine( downloadDir, Path.GetFileName( fileList[ 0 ] ) );
							installProcess.StartInfo.UseShellExecute = true;
							installProcess.StartInfo.CreateNoWindow = false;
							running = installProcess.Start();
						}
						else
						{
							// TODO: Run the Linux install.
						}
					}
				}
			}

			return running;
		}
		
		#endregion

		#region Public Methods
		/// <summary>
		/// Checks to see if there is a newer client application on the domain server and
		/// prompts the user to upgrade.
		/// </summary>
		/// <returns>The version of the update if available. Otherwise null is returned.</returns>
		public static string CheckForUpdate(string domainID)
		{
			ClientUpgrade cu = new ClientUpgrade(domainID);
			return cu.CheckForUpdate();
		}
			
		/// <summary>
		/// Gets the updated client application and runs the installation program.
		/// Note: This call will return before the application is updated.
		/// </summary>
		/// <param name="domainID">The ID of the domain to check for updates against.</param>
		/// <returns>True if the installation program is successfully started. Otherwise false is returned.</returns>
		public static bool RunUpdate(string domainID)
		{
			ClientUpgrade cu = new ClientUpgrade(domainID);
			return cu.RunUpdate();
		}

		#endregion
	}
}