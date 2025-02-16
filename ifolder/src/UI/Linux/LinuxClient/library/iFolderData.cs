/*****************************************************************************
*
* Copyright (c) [2009] Novell, Inc.
* All Rights Reserved.
*
* This program is free software; you can redistribute it and/or
* modify it under the terms of version 2 of the GNU General Public License as
* published by the Free Software Foundation.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.   See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program; if not, contact Novell, Inc.
*
* To contact Novell about this file by physical or electronic mail,
* you may find current contact information at www.novell.com
*
*-----------------------------------------------------------------------------
  *
  *                 $Author: Calvin Gaisford <cgaisford@novell.com>
  *                          Boyd Timothy <btimothy@novell.com>
  *                 $Modified by: <Modifier>
  *                 $Mod Date: <Date Modified>
  *                 $Revision: 0.0
  *-----------------------------------------------------------------------------
  * This module is used to:
  *        <Description of the functionality of the file >
  *
  *
  *******************************************************************************/
	 
using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Xml;
using System.Threading;

using Simias.Client;
using Simias.Client.Event;
using Novell.iFolder.Events;
using Novell.iFolder.Controller;

using Gtk;

namespace Novell.iFolder
{
	/// <summary>
	/// iFolder Data used in Linux Client
	/// </summary>
	public sealed class iFolderData
	{
		/// <summary>
		/// Member that ensures single instance
		/// </summary>
		private static iFolderData instance = null;
		
		/// <summary>
		/// This object is used to control access to the data
		/// </summary>
		private static object instanceLock = new object();


		/// <summary>
		/// Web Service to access the ifolder data
		/// </summary>
		private iFolderWebService	ifws;


		/// <summary>
		/// Web Service to access the simias data
		/// </summary>
		private SimiasWebService	simws;
		
		private Manager				simiasManager;
		
		
		/// <summary>
		/// Handle to the SimiasEventBroker
		/// </summary>
		private SimiasEventBroker eventBroker;


		private DomainController domainController;

		private Hashtable		ifolderIters = null;
		
		/// Keep a hashtable around to keep track of when a user deletes an
		/// iFolder.  By doing this, the client can immediately give
		/// feedback to the user by removing the deleted iFolder from the UI
		/// and add it onto the list of iFolders we eventually expect to get
		/// a delete event for.
		private Hashtable		deletediFolders = null;
		
		/// <summary>
		/// TreeModel to hold all of the iFolders/Subscriptions.  All classes
		/// wanting to know about new/changed/deleted iFolders should add
		/// event handlers onto this TreeModel.
		/// </summary>
		private ListStore iFolderListStore;
		
//		public event EventHandler iFolderAdded;
//		public event EventHandler iFolderDeleted;

		// These variables are used to keep track of how many
		// outstanding objects there are during a sync so that we don't
		// have to call CalculateSyncSize() over and over needlessly.
		private uint objectsToSync = 0;
		private bool startingSync  = false;
		private bool bFileSyncFailed = false;

        /// <summary>
        /// Gets list of iFolders
        /// </summary>
		public ListStore iFolders
		{
			get
			{
				return iFolderListStore;
			}
		}


		/// <summary>
		/// Constructor.
		/// </summary>
		private iFolderData()
		{
			simiasManager = Util.GetSimiasManager();

			try
			{
				Debug.PrintLine(String.Format("Url = {0}", simiasManager.WebServiceUri));
				ifws = new iFolderWebService();
				ifws.Url = 
					simiasManager.WebServiceUri.ToString() +
					"/iFolder.asmx";
				LocalService.Start(ifws, simiasManager.WebServiceUri, simiasManager.DataPath);
			}
			catch(Exception)
			{
				ifws = null;
				throw new Exception("Unable to create ifolder web service");
			}
			try
			{
				simws = new SimiasWebService();
				simws.Url = 
					simiasManager.WebServiceUri.ToString() +
					"/Simias.asmx";
				LocalService.Start(simws, simiasManager.WebServiceUri, simiasManager.DataPath);
			}
			catch(Exception)
			{
				simws = null;
				throw new Exception("Unable to create simias web service");
			}
			
			domainController = DomainController.GetDomainController();

			iFolderListStore = new ListStore(typeof(iFolderHolder));
			iFolderListStore.SetSortFunc(
				0,
				new TreeIterCompareFunc(TreeModelSortFunction));
			iFolderListStore.SetSortColumnId(0, SortType.Ascending);
			
			ifolderIters = new Hashtable();
			deletediFolders = new Hashtable();

			// Register for domain events
			if (domainController != null)
			{
				domainController.DomainAdded +=
					new DomainAddedEventHandler(OnDomainAddedEvent);
				domainController.DomainDeleted +=
					new DomainDeletedEventHandler(OnDomainDeletedEvent);

				domainController.DomainLoggedOut +=
					new DomainLoggedOutEventHandler(OnDomainLoggedOutEvent);
			}

			// Register with the SimiasEventBroker to get Simias Events
			eventBroker = SimiasEventBroker.GetSimiasEventBroker();
			if (eventBroker != null)
			{
//				eventBroker.iFolderAdded				+= OniFolderAddedEvent;
//				eventBroker.iFolderChanged				+= OniFolderChangedEvent;
//				eventBroker.iFolderDeleted				+= OniFolderDeletedEvent;
				eventBroker.CollectionSyncEventFired 	+= OniFolderSyncEvent;
				eventBroker.FileSyncEventFired			+= OniFolderFileSyncEvent;
			}

//			Refresh();
		}
		
		private int TreeModelSortFunction(TreeModel model, TreeIter a, TreeIter b)
		{
			iFolderHolder holderA = (iFolderHolder)model.GetValue(a, 0);
			iFolderHolder holderB = (iFolderHolder)model.GetValue(b, 0);
			
			if (holderA == null || holderB == null)
				return 0;
			
			iFolderWeb ifolderA = holderA.iFolder;
			iFolderWeb ifolderB = holderB.iFolder;
			
			if (ifolderA == null || ifolderB == null)
				return 0;
			
			string nameA = ifolderA.Name;
			string nameB = ifolderB.Name;
			
			if (nameA == null || nameB == null)
				return 0;

			return string.Compare(nameA, nameB, true);
		}




		//===================================================================
		// GetData
		// Gets the current instance of iFolderData
		//===================================================================
		static public iFolderData GetData()
		{
			lock (instanceLock)
			{
				if (instance == null)
				{
					instance = new iFolderData();
				}

				return instance;
			}
		}




		//===================================================================
		// Refresh
		// Reads the current ifolder and domains from Simias
		//===================================================================
		public void Refresh()
		{
//			Debug.PrintLine(String.Format("In Refresh: Number of iFolders: {0}", ifws.GetAlliFolders().Length));
			lock (instanceLock)
			{
				// Clear orphaned iFolders out of the iFolderListStore
				ClearOrphanediFolders();
				
				// Refresh the current iFolders
				iFolderWeb[] ifolders = null;
				try
				{
					ifolders = ifws.GetAlliFolders();
				}
				catch(Exception)
				{
				 //TODO: Exception was found when domain is disconnected, were as foudn that
                                 // GetAlliFolders is returning exception, but not able locate as debugging shows 
                                 // function is going throught	
				
					//ifolders = null;
				}

				if(ifolders != null && ifolders.Length >0)
				{
					Hashtable		ifolderItersClone = null;
					try
					{
						ifolderItersClone = (Hashtable) ifolderIters.Clone();
					}
					catch(Exception ex)
					{
						Debug.PrintLine(string.Format("Exception while cloning hashtable. {0}", ex.Message));
					}
					// Populate/update iFolderListStore
					foreach (iFolderWeb ifolder in ifolders)
					{
						// Check to see if an iFolder already exists and if so
						// update the current one.  Otherwise, add a new one to
						// the iFolderListStore.  We can't just replace or
						// recreate an iFolderHolder object because we lose
						// count of the number of objects that are left to
						// synchronize (Bug #82690).
					//	Debug.PrintLine("encr_status: {0}", ifolder.encryption_status);
						string ifolderID =
							ifolder.IsSubscription ?
								ifolder.CollectionID :
								ifolder.ID;

						if (ifolderIters.ContainsKey(ifolderID))
						{
							// We already have this iFolder in the ListStore so
							// just update the iFolderWeb object in the
							// iFolderHolder.
							TreeIter iter = (TreeIter)ifolderIters[ifolderID];
							iFolderHolder existingHolder = null;

							try
							{
								existingHolder = (iFolderHolder)
									iFolderListStore.GetValue(iter, 0);
							}
							catch(Exception e)
							{
								Debug.PrintLine(e.Message);
							}

							if ((existingHolder != null) && (existingHolder.iFolder.OwnerID != ifolder.OwnerID))
							{
								existingHolder.iFolder = ifolder;
	
								TreePath path = iFolderListStore.GetPath(iter);
								if (path != null)
								{
									iFolderChangedHandler changedHandler =
								    new iFolderChangedHandler( path, iter, iFolderListStore);
				                                    GLib.Idle.Add(changedHandler.IdleHandler);
                                				}
						                       //  iFolderListStore.EmitRowChanged(path, iter);

							}
							try
							{
								if( ifolderItersClone.ContainsKey(ifolderID))
									ifolderItersClone.Remove( ifolderID);
							}
							catch(Exception ex)
							{
								Debug.PrintLine(string.Format("Exception while removing from ht. {0}", ex.Message));
							}
						}
						else
						{
							// This is a new one we haven't seen before
							AddiFolder(ifolder);
						}
					}
					try
					{
						foreach(string key in ifolderItersClone.Keys)
						{
							try
							{
								Debug.PrintLine(string.Format("Removing entry: {0}", key));
								try
								{
									if( ifws.CheckiFolder( key ) == false)
										throw new Exception( "Invalid iFolderID");
								}
								catch(Exception)
								{
									DeliFolder(key);
								}
							}
							catch(Exception ex)
							{
								Debug.PrintLine(string.Format("exception in real delete. {0}", ex.Message));
							}
						}
					}
					catch(Exception ex)
					{
						Debug.PrintLine(string.Format("Exception in removing. Outer for only. {0}", ex.Message));
					}
				}
			}
		}
		
		/// <summary>
		/// Clear out iFolders that no longer have a domain
		/// </summary>
		private void ClearOrphanediFolders()
		{
			lock(instanceLock)
			{
				// Iterate through the existing iFolders and remove the ones that
				// were part of the deleted domain.
				TreeIter iter;
				if (iFolderListStore.GetIterFirst(out iter))
				{
					// Build a list of the iters we need to remove and remove them
					// after iterating the list so that we don't modify the list
					// while iterating through it.
					ArrayList itersToRemove = new ArrayList();

					iFolderHolder holder;
					
					do
					{
						holder =
							(iFolderHolder)iFolderListStore.GetValue(iter, 0);

						DomainInformation domain =
							domainController.GetDomain(holder.iFolder.DomainID);
					
					 if (domain == null || (!domain.Authenticated && holder.iFolder.IsSubscription))	
						{
							// Queue removal of the holder
							itersToRemove.Add(holder.iFolder.ID);
						}
					} while (iFolderListStore.IterNext(ref iter));
					
					foreach(string ifolderID in itersToRemove)
					{
						DeliFolder(ifolderID);
					}
				}
			}
		}



		//===================================================================
		// AddiFolder
		// adds the ifolder to the iFolderData internal tables
		//===================================================================
		private iFolderHolder AddiFolder(iFolderWeb ifolder)
		{
			lock (instanceLock)
			{
				iFolderHolder ifHolder = null;

				string ifolderID =
					ifolder.IsSubscription ?
						ifolder.CollectionID :
						ifolder.ID;

				if (ifolderIters.ContainsKey(ifolderID))
				{
					// This condition got hit most likely because CreateiFolder
					// was called and now the SimiasEventBroker is calling
					// AddiFolder on the Added Event.

					// We already have this iFolder in the ListStore so
					// just update the iFolderWeb object in the
					// iFolderHolder.
					TreeIter iter = (TreeIter)ifolderIters[ifolderID];

					ifHolder = (iFolderHolder)
						iFolderListStore.GetValue(iter, 0);

					if (ifHolder != null)
					{
						ifHolder.iFolder = ifolder;

						TreePath path = iFolderListStore.GetPath(iter);
						if (path != null)
						{
							iFolderChangedHandler changedHandler =
								new iFolderChangedHandler(
									path, iter, iFolderListStore);

							GLib.Idle.Add(changedHandler.IdleHandler);
						}
					}
				}
				else
				{
					ifHolder = new iFolderHolder(ifolder);
					ifHolder.State = iFolderState.Initial;
					iFolderAddHandler addHandler =
						new iFolderAddHandler(ifHolder, this);
					GLib.Idle.Add(addHandler.IdleHandler);
				}
				
				return ifHolder;
			}
		}
		
        /// <summary>
        /// Add iFolder
        /// </summary>
        /// <param name="holder">iFolder Holder</param>
		private void ProtectedAddiFolder(iFolderHolder holder)
		{
			if (holder == null) return;

			lock(instanceLock)
			{
				iFolderWeb ifolder = holder.iFolder;

				string ifolderID =
					ifolder.IsSubscription ?
						ifolder.CollectionID :
						ifolder.ID;

				if (!ifolderIters.ContainsKey(ifolderID))
				{
					TreeIter iter = iFolderListStore.AppendValues(holder);
					if (ifolder.IsSubscription)
					{
						ifolderIters[ifolder.CollectionID] = iter;
//This needs a fix, since ifolder.CollectionID and ifolder.ID are same
					}
					else
					{
						ifolderIters[ifolder.ID] = iter;
					}
				}
			}
		}

		


		//===================================================================
		// DeliFolder
		// removes the ifolder from the iFolderData internal tables
		//===================================================================
		public void DeliFolder(string ifolderID)
		{
			lock (instanceLock)
			{
				iFolderDeleteHandler deleteHandler =
					new iFolderDeleteHandler(ifolderID, this);
				GLib.Idle.Add(deleteHandler.IdleHandler);
			}
		}
		
        /// <summary>
        /// RealDelete
        /// </summary>
        /// <param name="ifolderID">iFolder ID</param>
        /// <param name="deleteImmediately">true if delete immediately</param>
		private void RealDelete(string ifolderID, bool deleteImmediately)
		{
			lock (instanceLock)
			{
				if (!deleteImmediately && deletediFolders.Contains(ifolderID))
				{
					// This has already been deleted by the user
					deletediFolders.Remove(ifolderID);
					return;
				}
			
				string realID = ifolderID;

				if(!IsiFolder(realID))
				{
					if( (realID == null)/* || (!IsiFolder(realID)) */)
						return;

				}

				if (ifolderIters.ContainsKey(realID))
				{
					TreeIter iter = (TreeIter)ifolderIters[realID];
					iFolderListStore.Remove(ref iter);
					ifolderIters.Remove(realID);
				}
				
				// Keep track of this so that when an external
				// delete comes in we will ignore it.
				if (deleteImmediately)
					deletediFolders[realID] = realID;
			}
		}
		
        /// <summary>
        /// Quick Delete
        /// </summary>
        /// <param name="ifolderID">iFolder ID</param>
		private void QuickDelete(string ifolderID)
		{
			RealDelete(ifolderID, true);
		}
		
		private void ProtectedDeliFolder(string ifolderID)
		{
			RealDelete(ifolderID, false);
		}

		


		//===================================================================
		// IsiFolder
		// Checks to see if the ID passed is an iFolder
		//===================================================================
		public bool IsiFolder(string ifolderID)
		{
			if (ifolderID == null) return false;

			lock(instanceLock)
			{
				return ifolderIters.ContainsKey(ifolderID);
			}
		}



// FIXME: iFolderData.ISPOBox should be moved to DomainController
		//===================================================================
		// ISPOBox
		// Checks to see if the ID passed is a pobox in a current domain
		//===================================================================
		public bool ISPOBox(string poBoxID)
		{
			lock(instanceLock)
			{
				DomainInformation[] domainsA = domainController.GetDomains();
				foreach(DomainInformation domain in domainsA)
				{
					if (domain.POBoxID != null && domain.POBoxID.Equals(poBoxID))
						return true;
				}

				return false;
			}
		}




		//===================================================================
		// GetiFolderID
		// Gets the iFolder ID for a subscription that's been added
		//===================================================================
		public string GetiFolderID(string subscriptionID)
		{
			lock(instanceLock)
			{
				return subscriptionID;
			}
		}




		//===================================================================
		// GetiFolder
		// Gets the iFolderHolder from the iFolderData structures
		//===================================================================
		public iFolderHolder GetiFolder(string ifolderID)
		{
			lock(instanceLock)
			{
				iFolderHolder ifHolder = null;
				Debug.PrintLine("In GetiFolder");

				if (ifolderIters.ContainsKey(ifolderID))
				{
					TreeIter iter = (TreeIter)ifolderIters[ifolderID];
					ifHolder = (iFolderHolder)
						iFolderListStore.GetValue(iter, 0);
				}

				return ifHolder;
			}
		}

		public iFolderWeb GetDefaultiFolder( string iFolderID)
		{
			return ifws.GetMinimaliFolder(iFolderID, 1);
		}


		//===================================================================
		// ReadiFolder
		// Reads and returns the iFolderHolder for the specificed ifolderID
		//===================================================================
		public iFolderHolder ReadiFolder(string ifolderID)
		{
			lock(instanceLock)
			{
				iFolderHolder ifHolder = null;

				if (ifolderIters.ContainsKey(ifolderID))
				{
					TreeIter iter = (TreeIter)ifolderIters[ifolderID];
					ifHolder = (iFolderHolder)
						iFolderListStore.GetValue(iter, 0);
				}
				
				// at this point, if we had the iFolder, we'll have the
				// original iFolderHolder to update
				try
				{
					iFolderWeb ifolder =
						ifws.GetiFolder(ifolderID);


					if (ifolder != null)
					{
						if (ifHolder != null)
						{
							ifHolder.iFolder = ifolder;

							if (ifolderIters.ContainsKey(ifolder.ID))
							{
								// Emit a TreeModel RowChanged Event
								TreeIter iter = (TreeIter)
									ifolderIters[ifolder.ID];
								TreePath path = iFolderListStore.GetPath(iter);
								if (path != null)
								{
									iFolderChangedHandler changedHandler =
										new iFolderChangedHandler(
											path, iter, iFolderListStore);
									GLib.Idle.Add(changedHandler.IdleHandler);
								}
							}
						}
						else
						{
							ifHolder = AddiFolder(ifolder);
						}
					}
				}
				catch(Exception)
				{
					ifHolder = null;
				}
				
				return ifHolder;
			}
		}

		//===================================================================
		// GetiFolders
		// Returns an array of the current iFolderHolders
		//===================================================================
		public iFolderHolder[] GetiFolders()
		{
			lock(instanceLock)
			{
				ArrayList arrayList = new ArrayList();
				TreeIter iter;
				
				if (iFolderListStore.GetIterFirst(out iter))
				{
					do
					{
						iFolderHolder ifHolder =
							(iFolderHolder)iFolderListStore.GetValue(iter, 0);
						if (ifHolder != null && ifHolder.iFolder != null)
							arrayList.Add(ifHolder);
					} while (iFolderListStore.IterNext(ref iter));
				}

				iFolderHolder[] ifolderA = (iFolderHolder[])arrayList.ToArray(typeof(iFolderHolder));
//				foreach (iFolderHolder ifHolder in ifolderA)
//				{
//Debug.PrintLine("\t{0}", ifHolder.iFolder.Name);
//				}

				return ifolderA;
			}
		}




		//===================================================================
		// GetAvailableiFolder
		// Returns the subscription for an iFolder or null if it isn't there
		//===================================================================
		public iFolderHolder GetAvailableiFolder(string ifolderID)
		{
			lock(instanceLock)
			{
				string realID;
				iFolderHolder ifHolder = null;

				realID = GetiFolderID(ifolderID);
				if(realID != null)
				{
					if (ifolderIters.ContainsKey(realID))
					{
						TreeIter iter = (TreeIter)ifolderIters[realID];
						try
						{
							ifHolder =
								(iFolderHolder)iFolderListStore.GetValue(iter, 0);
						}
						catch(Exception e)
						{
							Debug.PrintLine(e.Message);
						}
					}
				}

				return ifHolder;
			}
		}




		//===================================================================
		// ReadAvailableiFolder
		// Returns the subscription for an iFolder and reads the update version
		//===================================================================
		public iFolderHolder ReadAvailableiFolder(	string ifolderID,
													string collectionID)
		{
			lock(instanceLock)
			{
				iFolderHolder ifHolder = GetAvailableiFolder(ifolderID);
				try
				{
					iFolderWeb ifolder = ifws.GetiFolderInvitation(
								collectionID, ifolderID);
					if (ifolder == null || !ifolder.State.Equals("Available"))
						return null;

					if(ifHolder != null)
					{
						if(!IsiFolder(ifolder.CollectionID))
						{
							ifHolder.iFolder = ifolder;
							
							if (ifolderIters.ContainsKey(ifolder.CollectionID))
							{
								// Emit a TreeModel RowChanged Event
								TreeIter iter = (TreeIter)
									ifolderIters[ifolder.CollectionID];
								TreePath path = iFolderListStore.GetPath(iter);
								if (path != null)
								{
									iFolderChangedHandler changedHandler =
										new iFolderChangedHandler(
											path, iter, iFolderListStore);
									GLib.Idle.Add(changedHandler.IdleHandler);
								}
							}
						}
					}
					else
					{
						// Prevent this subscription from being added if it's
						// waiting to really been deleted.
						if (!deletediFolders.Contains(ifolder.CollectionID)
							&& !IsiFolder(ifolder.CollectionID))
						{
								ifHolder = AddiFolder(ifolder);
						}
					}
				}
				catch(Exception)
				{
					ifHolder = null;
				}

				return ifHolder;
			}
		}




		//===================================================================
		// CreateiFolder
		// creates an iFolder in the domain at the path specified
		//===================================================================
		public iFolderHolder CreateiFolder(string path, string domainID, bool SSL, string EncryptionAlgorithm)
		{
			// Manipulate the passphrase to the nearest multiple of eight
			lock(instanceLock)
			{
				iFolderWeb newiFolder;
				if( EncryptionAlgorithm == null)		// Sharable iFolder
				{
					Debug.PrintLine("Creating unencrypted folder");
					newiFolder = ifws.CreateiFolderInDomainEncr(path, domainID,
                                                                                        SSL, null, null);
					//calling encry method passing null for encryp algo and pp
				}
				else
				{
					Debug.PrintLine("Creating encrypted folder");
					string Passphrase  = simws.GetPassPhrase(domainID);
   					newiFolder = ifws.CreateiFolderInDomainEncr(path, domainID, 
											SSL, EncryptionAlgorithm, Passphrase);
				}

				if (newiFolder == null)
				{
					return null;
				}

				iFolderHolder ifHolder = AddiFolder(newiFolder);
				return ifHolder;
			}	
		}

		public int GetSecurityPolicy(string domainID)
		{
			return ifws.GetSecurityPolicy(domainID);
		}
		/*
		// Not Used
		public int SetSecurityPolicy(string domainID, int status)
		{
			return ifws.SetSecurityPolicy(domainID, status);
		}
		*/

		//===================================================================
		// AcceptiFolderInvitation
		// accepts an iFolder Invitation and updates the iFoler Information
		//===================================================================
		public iFolderHolder AcceptiFolderInvitation(	string ifolderID,
														string domainID,
														string localPath)
		{
			return AcceptiFolderInvitation( ifolderID, domainID, localPath, false);
		}


		//===================================================================
		// AcceptiFolderInvitation
		// accepts an iFolder Invitation and updates the iFoler Information
		//===================================================================
		public iFolderHolder AcceptiFolderInvitation(	string ifolderID,
														string domainID,
														string localPath,
														bool merge)
		{
			lock(instanceLock)
			{
				iFolderHolder ifHolder = null;
//				iFolderWeb[] ifarray = ifws.GetAlliFolders();
//				Debug.PrintLine(String.Format("The number of iFolders is: {0}", ifarray.Length));
				iFolderWeb newifolder = null;
				
				if(merge == true)
				{
					newifolder = ifws.MergeiFolder(
											domainID,
											ifolderID,
											localPath);	   		 		
				}
				else
				{
					newifolder = ifws.AcceptiFolderInvitation(
											domainID,
											ifolderID,
											localPath);
				}
//				ifarray = ifws.GetAlliFolders();
//				Debug.PrintLine(String.Format("The number of iFolders is: {0}", ifarray.Length));
				if (newifolder == null)
				{
					return null;
				}



				ifHolder = GetiFolder(ifolderID);
				if( ifHolder == null)
				{
					Debug.PrintLine("Calling addiFolder");
					ifHolder = AddiFolder(newifolder);
				}
				ifHolder.iFolder = newifolder;

				return ifHolder;
			}
		}




		//===================================================================
		// RevertiFolder
		// reverts an iFolder to an invitation
		//===================================================================
		public iFolderHolder RevertiFolder(string ifolderID)
		{
			lock(instanceLock)
			{
				iFolderHolder ifHolder = null;

				ifHolder = GetiFolder(ifolderID);
				if(ifHolder == null)
				{
					throw new Exception("iFolder did not exist");
				}

				try
				{
	    			iFolderWeb reviFolder = 
						ifws.RevertiFolder(ifHolder.iFolder.ID);
					if (reviFolder == null)
					{
						ifHolder = null;
						return ifHolder;
					}
					reviFolder.IsSubscription = true;
					ifHolder.iFolder = reviFolder;

				    if( ifolderIters == null)
					{
						ifolderIters = new Hashtable();
					}
					if (ifolderIters.ContainsKey(ifolderID))
					{
						TreeIter iter = (TreeIter)
						ifolderIters[ifolderID];
						//Update the iter, so that the Deleted event will update UI in GLib.Idle
						iFolderListStore.SetValue(iter, 0, ifHolder);
					}
					
				}
				catch(Exception e)
				{
					if ( String.Compare( e.Message, "Invalid CollectionID" ) == 0 )	
					{
						ifHolder = null;
						return ifHolder;
					}
					throw new Exception(Util.GS("An exception occurred while attempting to revert the iFolder to a normal folder.  Exception message: ") + e.Message);
				}

				return ifHolder;
			}
		}




		//===================================================================
		// DeleteiFolder
		// reverts and/or declines an iFolder invitation
		//===================================================================
		public void DeleteiFolder(string ifolderID)
		{
			lock(instanceLock)
			{
				iFolderHolder ifHolder = null;
				iFolderWeb ifolder = null;

				ifHolder = GetiFolder(ifolderID);
				bool IsOwner = (ifHolder.iFolder.CurrentUserID.Equals (ifHolder.iFolder.OwnerID));

				if(IsiFolder(ifolderID) && IsOwner)
				{
					ifolder = ifHolder.iFolder;
					if (ifolder.Role == null || ifolder.Role.Equals("Master"))
					{
						string realID = ifolder.ID;
						ifws.DeleteiFolder(ifolder.DomainID, realID);
						QuickDelete(realID);
						return;
					}

					ifHolder = RevertiFolder(ifolderID);
				}
				else
				{
					string realID = GetiFolderID(ifolderID);
					if(realID != null)
					{
						ifHolder = GetiFolder(realID);
					}
				}

				if (ifHolder != null)
				{
					ifolder = ifHolder.iFolder;
					if (ifolder != null && ifolder.IsSubscription)
					{
						string realID = ifolder.ID;
						ifws.DeclineiFolderInvitation(
							ifolder.DomainID, realID);
						QuickDelete(realID);
					}
				}
			}	
		}



        /// <summary>
        /// Get iFolder UserFromNode ID
        /// </summary>
        /// <param name="collectionID">Collection ID</param>
        /// <param name="nodeID">Node ID</param>
        /// <returns>iFolder User</returns>
		public iFolderUser GetiFolderUserFromNodeID(string collectionID, 
														string nodeID)
		{
			lock(instanceLock)
			{
				iFolderUser user = null;

				try
				{
					user = ifws.GetiFolderUserFromNodeID(
						collectionID, nodeID);
				}
				catch(Exception)
				{
					user = null;	
				}
				return user;
			}
		}


        /// <summary>
        /// Is Current User
        /// </summary>
        /// <param name="UserID">User ID</param>
        /// <returns>Boolean Value</returns>
		public bool IsCurrentUser(string UserID)
		{
			lock(instanceLock)
			{
				DomainInformation[] domainsA = domainController.GetDomains();
				foreach(DomainInformation domain in domainsA)
				{
					if(domain.MemberUserID.Equals(UserID))
						return true;
				}
				return false;
			}
		}

        /// <summary>
        /// Get User Disk Space
        /// </summary>
        /// <param name="UserID">User ID</param>
        /// <returns>Disk Space</returns>
		public DiskSpace GetUserDiskSpace(string UserID)
		{
			lock(instanceLock)
			{
				DiskSpace ds = null;
				try
				{
					ds = ifws.GetUserDiskSpace(UserID);
				}
				catch(Exception)
				{
					// Let this one go
				}
				return ds;
			}
		}

		///
		/// Event Handlers
		///
		private void OnDomainAddedEvent(object o, DomainEventArgs args)
		{
			Refresh();	// Refresh will add in any new iFolders
			// Since POBox creation is completely removed from 3.7 and above clients so removing references too.
			//domainController.CheckForNewiFolders();
		}
		
		private void OnDomainDeletedEvent(object o, DomainEventArgs args)
		{
			//Refresh();
		}
		
		private void OnDomainLoggedOutEvent(object o, DomainEventArgs args)
		{
			//Refresh();
		}

		private void OniFolderSyncEvent(object o, CollectionSyncEventArgs args)
		{
			if (args == null || args.ID == null || args.Name == null)
				return;	// Prevent an exception

			TreeIter iter = TreeIter.Zero;
			iFolderHolder ifHolder = null;

			if (ifolderIters.ContainsKey(args.ID))
			{
				iter = (TreeIter)ifolderIters[args.ID];
				ifHolder = (iFolderHolder)iFolderListStore.GetValue(iter, 0);
			}
			
			if (ifHolder == null) return;

			switch(args.Action)
			{
				case Simias.Client.Event.Action.StartLocalSync:
					ifHolder.State = iFolderState.SynchronizingLocal;
					break;
				case Simias.Client.Event.Action.StartSync:
					// Keep track of when a sync starts
					startingSync = true;
					
					// Keep track of if any files failed to sync
					bFileSyncFailed = false;

					ifHolder.State = iFolderState.Synchronizing;
					break;
				case Simias.Client.Event.Action.StopSync:
					try
					{

						ReadiFolder(args.ID);	// Update our copy of the iFolder
					
						SyncSize syncSize = ifws.CalculateSyncSize(args.ID);
						objectsToSync = syncSize.SyncNodeCount;
						ifHolder.ObjectsToSync = objectsToSync;
					}
					catch
					{}

					if (ifHolder.ObjectsToSync > 0)
					{
						// Check to see if there were any errors synchronizing
						// any files.  If there were, change the state to
						// FailedSync.
						if (bFileSyncFailed)
							ifHolder.State = iFolderState.FailedSync;
						else
							ifHolder.State = iFolderState.Normal;
					}
					else
					{
						if (args.Connected || ifHolder.iFolder.Role == "Master")
							ifHolder.State = iFolderState.Normal;
						else
							ifHolder.State = iFolderState.Disconnected;
					}

					objectsToSync = 0;
					break;
				default:
					break;
			}
			
			// Emit a TreeModel RowChanged Event
			TreePath path = iFolderListStore.GetPath(iter);
			if (path != null)
			{
				 iFolderChangedHandler changedHandler =
						 new iFolderChangedHandler( path, iter, iFolderListStore);
				 GLib.Idle.Add(changedHandler.IdleHandler);				 
			}		
			//	iFolderListStore.EmitRowChanged(path, iter);
		}

		private void OniFolderFileSyncEvent(object o, FileSyncEventArgs args)
		{
			if (args == null || args.CollectionID == null || args.Name == null)
				return;	// Prevent an exception

			if (args.SizeRemaining == args.SizeToSync)
			{
				if (startingSync || (objectsToSync <= 0))
				{
					startingSync = false;
					try
					{
						SyncSize syncSize = ifws.CalculateSyncSize(args.CollectionID);
						objectsToSync = syncSize.SyncNodeCount;
					}
					catch(Exception)
					{
						objectsToSync = 1;
					}
				}

				if (!args.Direction.Equals(Simias.Client.Event.Direction.Local))
				{
					// Decrement the count whether we're showing the iFolder
					// in the current list or not.  We'll need this if the
					// user switches back to the list that contains the iFolder
					// that is actually synchronizing.
					if (objectsToSync <= 0)
						objectsToSync = 0;
					else
						objectsToSync--;
	

					// Get the iFolderHolder and set the objectsToSync
					TreeIter iter = TreeIter.Zero;
					iFolderHolder ifHolder = null;
					if (ifolderIters.ContainsKey(args.CollectionID))
					{
						iter = (TreeIter)ifolderIters[args.CollectionID];
						ifHolder = (iFolderHolder)iFolderListStore.GetValue(iter, 0);
					}
					
					if (ifHolder != null)
					{
						if (ifHolder.State != iFolderState.Synchronizing)
							ifHolder.State = iFolderState.Synchronizing;

						ifHolder.ObjectsToSync = objectsToSync;

						// Emit a TreeModel RowChanged Event
						TreePath path = iFolderListStore.GetPath(iter);
						if (path != null)
						{
							iFolderChangedHandler changedHandler =
									 new iFolderChangedHandler(path, iter, iFolderListStore);
							GLib.Idle.Add(changedHandler.IdleHandler);
						}
				//			iFolderListStore.EmitRowChanged(path, iter);
					}
				}
			}
			
			if (args.Status != SyncStatus.Success)
				bFileSyncFailed = true;
		}
		
        /// <summary>
        /// Print Debug State
        /// </summary>
		public void PrintDebugState()
		{
			lock(instanceLock)
			{
				Debug.PrintLine("************************** iFolderData Data Inspection **************************");
				Debug.PrintLine(String.Format("ifolderIters Hashtable (All items in iFolderListStore, {0}):", ifolderIters.Count));
				foreach(TreeIter treeIter in ifolderIters.Values)
				{
					iFolderHolder ifHolder = null;
					try
					{
						ifHolder = (iFolderHolder)iFolderListStore.GetValue(treeIter, 0);
					}
					catch{}
					
					if (ifHolder == null)
						Debug.PrintLine("\tIter does not exist in iFolderListStore");
					else
						Debug.PrintLine(String.Format("\t{0}, {1}", ifHolder.iFolder.ID, ifHolder.iFolder.Name));
				}
				
				
				Debug.PrintLine(String.Format("iFolderListStore Contents ({0}):", iFolderListStore.IterNChildren()));
				TreeIter iter;
				if (iFolderListStore.GetIterFirst(out iter))
				{

					iFolderHolder holder;
					
					do
					{
						holder =
							(iFolderHolder)iFolderListStore.GetValue(iter, 0);
						
						Debug.PrintLine(String.Format("\t{0}, {1}", holder.iFolder.ID, holder.iFolder.Name));
					} while (iFolderListStore.IterNext(ref iter));
				}
				
				Debug.PrintLine(String.Format("deletediFolders ({0})", deletediFolders.Count));
				foreach (string id in deletediFolders.Values)
				{
					Debug.PrintLine(String.Format("\t{0}", id));
				}
			}
		}
		
        /// <summary>
        /// iFolder Add Handler
        /// </summary>
		private class iFolderAddHandler
		{
			iFolderHolder	holder;
			iFolderData		ifdata;
			
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="holder">iFOlder Holder</param>
            /// <param name="ifdata">iFolder Data</param>
			public iFolderAddHandler(iFolderHolder holder, iFolderData ifdata)
			{
				this.holder = holder;
				this.ifdata = ifdata;
			}
			
			public bool IdleHandler()
			{
				if (holder != null && ifdata != null)
					ifdata.ProtectedAddiFolder(holder);
				
				return false;
			}
		}
		
        /// <summary>
        /// class iFolderDeleteHandler
        /// </summary>
		private class iFolderDeleteHandler
		{
			string			ifolderID;
			iFolderData		ifdata;
			
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="ifolderID">iFolder ID</param>
            /// <param name="ifdata">iFOlder Data</param>
			public iFolderDeleteHandler(string ifolderID, iFolderData ifdata)
			{
				this.ifolderID = ifolderID;
				this.ifdata = ifdata;
			}
			
			public bool IdleHandler()
			{
				if (ifolderID != null && ifdata != null)
					ifdata.ProtectedDeliFolder(ifolderID);
				
				return false;
			}
		}
	}
	
    /// <summary>
    /// iFolder Change Handler
    /// </summary>
	public class iFolderChangedHandler
	{
		TreePath	path;
		TreeIter	iter;
		ListStore	list;
		
		public iFolderChangedHandler(TreePath path, TreeIter iter, ListStore list)
		{
			this.path = path;
			this.iter = iter;
			this.list = list;
		}
		
		public bool IdleHandler()
		{
			if (list != null && path != null && list.IterIsValid(iter))
				list.EmitRowChanged(path, iter);

			return false;	// Don't keep calling this
		}
	}

	public class Debug
	{
		public static void PrintLine(string str)
		{
			#if DEBUG
				Debug.PrintLine(str);
			#endif
		}
	}
}
